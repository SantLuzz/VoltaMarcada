using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.Support;

public sealed class CreateSupportTicketUseCase(
    ICurrentUser currentUser,
    ICurrentCompany currentCompany,
    ISupportRequestContextReader contextReader,
    ISupportTicketRepository repository,
    IFileStorage fileStorage,
    TimeProvider timeProvider)
{
    private const int MaximumAttachmentCount = 5;
    private const int MaximumAttachmentBytes = 5 * 1024 * 1024;
    private const int MaximumTotalAttachmentBytes = 20 * 1024 * 1024;

    public async Task<OperationResult<SupportTicketCreated>> ExecuteAsync(
        SendSupportRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            return OperationResult<SupportTicketCreated>.Failure(SupportErrors.Unauthenticated);
        }

        if (string.IsNullOrWhiteSpace(command.Subject) || command.Subject.Trim().Length > 200 ||
            string.IsNullOrWhiteSpace(command.Message) || command.Message.Trim().Length > 5000)
        {
            return OperationResult<SupportTicketCreated>.Failure(SupportErrors.InvalidInput);
        }

        if (command.Attachments is null || command.Attachments.Count > MaximumAttachmentCount)
        {
            return OperationResult<SupportTicketCreated>.Failure(SupportErrors.InvalidAttachment);
        }

        var companyId = await currentCompany.FindCompanyIdAsync(cancellationToken);
        if (companyId is null || companyId == Guid.Empty)
        {
            return OperationResult<SupportTicketCreated>.Failure(SupportErrors.CompanyUnavailable);
        }

        var context = await contextReader.GetAsync(currentUser.UserId, companyId.Value, cancellationToken);
        if (context is null || string.IsNullOrWhiteSpace(context.UserEmail))
        {
            return OperationResult<SupportTicketCreated>.Failure(SupportErrors.UserEmailUnavailable);
        }

        var validatedAttachments = ValidateAttachments(command.Attachments);
        if (validatedAttachments is null)
        {
            return OperationResult<SupportTicketCreated>.Failure(SupportErrors.InvalidAttachment);
        }

        var uploadedFiles = new List<StoredSupportAttachment>(validatedAttachments.Count);
        try
        {
            foreach (var attachment in validatedAttachments)
            {
                await using var content = new MemoryStream(attachment.Content, writable: false);
                var result = await fileStorage.UploadAsync(
                    new FileStorageUploadRequest(
                        new FileUpload(content, attachment.FileName, attachment.ContentType, attachment.Content.LongLength),
                        $"support/{companyId.Value:N}",
                        Access: FileStorageAccess.Private),
                    cancellationToken);
                uploadedFiles.Add(new StoredSupportAttachment(result.Key, attachment.FileName, attachment.ContentType, attachment.Content.LongLength));
            }

            await using var transaction = await repository.BeginTransactionAsync(cancellationToken);
            var now = timeProvider.GetUtcNow();
            var ticket = SupportTicket.Create(
                await repository.GetNextNumberAsync(cancellationToken),
                companyId.Value,
                currentUser.UserId,
                context.UserName,
                context.UserEmail,
                context.CompanyTradeName,
                command.Subject,
                command.Message,
                now);

            foreach (var attachment in uploadedFiles)
            {
                ticket.AddInitialAttachment(attachment.FileKey, attachment.FileName, attachment.ContentType, attachment.Length);
            }

            await repository.AddAsync(ticket, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperationResult<SupportTicketCreated>.Success(new SupportTicketCreated(ticket.Number));
        }
        catch
        {
            foreach (var attachment in uploadedFiles)
            {
                try { await fileStorage.DeleteAsync(attachment.FileKey, CancellationToken.None); }
                catch { }
            }

            throw;
        }
    }

    private static List<ValidatedSupportAttachment>? ValidateAttachments(IReadOnlyList<SupportAttachmentInput> attachments)
    {
        long totalBytes = 0;
        var result = new List<ValidatedSupportAttachment>(attachments.Count);
        foreach (var attachment in attachments)
        {
            if (attachment.Content is null || attachment.Content.Length is 0 or > MaximumAttachmentBytes)
            {
                return null;
            }

            totalBytes += attachment.Content.LongLength;
            if (totalBytes > MaximumTotalAttachmentBytes)
            {
                return null;
            }

            var contentType = SupportAttachmentDetector.DetectContentType(attachment.Content);
            if (contentType is null)
            {
                return null;
            }

            result.Add(new ValidatedSupportAttachment(attachment.FileName, contentType, attachment.Content));
        }

        return result;
    }

    private sealed record ValidatedSupportAttachment(string FileName, string ContentType, byte[] Content);
    private sealed record StoredSupportAttachment(string FileKey, string FileName, string ContentType, long Length);
}