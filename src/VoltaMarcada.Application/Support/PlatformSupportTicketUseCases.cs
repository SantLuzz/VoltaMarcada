using System.Net;
using VoltaMarcada.Application.Abstractions.Communication;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Routing;
using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Support;

public sealed class GetPlatformSupportTicketsUseCase(ISupportTicketRepository repository)
{
    public Task<IReadOnlyList<PlatformSupportTicketListItem>> ExecuteAsync(CancellationToken cancellationToken = default) => repository.GetAllAsync(cancellationToken);
}

public sealed class GetPlatformSupportTicketDetailsUseCase(ISupportTicketRepository repository)
{
    public async Task<OperationResult<PlatformSupportTicketDetails>> ExecuteAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await repository.GetForUpdateAsync(ticketId, cancellationToken);
        if (ticket is null) return OperationResult<PlatformSupportTicketDetails>.Failure(SupportErrors.TicketNotFound);
        return OperationResult<PlatformSupportTicketDetails>.Success(new PlatformSupportTicketDetails(ticket.Id, ticket.Number, ticket.CompanyTradeName, ticket.RequesterName, ticket.RequesterEmail, ticket.Subject, ticket.Status, ticket.OpenedAtUtc, ticket.ClosedAtUtc, ticket.Messages.OrderBy(message => message.CreatedAtUtc).Select(message => new PlatformSupportTicketMessage(message.Id, message.Direction, message.Body, message.CreatedAtUtc, message.Attachments.Select(attachment => new PlatformSupportTicketAttachment(attachment.Id, attachment.OriginalFileName, attachment.ContentType, attachment.Length)).ToArray())).ToArray()));
    }
}

public sealed class GetPlatformSupportTicketAttachmentUseCase(ISupportTicketRepository repository, IFileStorage fileStorage)
{
    public async Task<OperationResult<FileStorageDownloadResult>> ExecuteAsync(Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var attachment = await repository.GetAttachmentAsync(attachmentId, cancellationToken);
        if (attachment is null) return OperationResult<FileStorageDownloadResult>.Failure(SupportErrors.TicketNotFound);
        return OperationResult<FileStorageDownloadResult>.Success(await fileStorage.GetAsync(attachment.FileKey, cancellationToken));
    }
}

public sealed class ReplyToSupportTicketUseCase(ISupportTicketRepository repository, ICurrentUser currentUser, IEmailSender emailSender, IApplicationUrlBuilder urlBuilder, TimeProvider timeProvider)
{
    public async Task<OperationResult> ExecuteAsync(Guid ticketId, string response, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty) return OperationResult.Failure(SupportErrors.Unauthenticated);
        if (string.IsNullOrWhiteSpace(response) || response.Trim().Length > 5000) return OperationResult.Failure(SupportErrors.InvalidInput);
        var ticket = await repository.GetForUpdateAsync(ticketId, cancellationToken);
        if (ticket is null) return OperationResult.Failure(SupportErrors.TicketNotFound);
        if (ticket.Status == SupportTicketStatus.Closed) return OperationResult.Failure(SupportErrors.TicketClosed);

        var delivery = await emailSender.SendAsync(new EmailMessage(ticket.RequesterEmail, $"Res.: chamado nº {ticket.Number}", CreateEmailBody(ticket.RequesterName, ticket.Number, response, urlBuilder.BuildApplicationUrl("/logos/logo.png"))), cancellationToken);
        if (!delivery.IsSuccess) return OperationResult.Failure(SupportErrors.EmailDeliveryFailed);

        ticket.AddPlatformMessage(response, currentUser.UserId, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }

    private static string CreateEmailBody(string name, long number, string response, string logoUrl)
    {
        var safeName = WebUtility.HtmlEncode(name);
        var safeResponse = WebUtility.HtmlEncode(response.Trim()).Replace("\n", "<br />", StringComparison.Ordinal);
        var safeLogoUrl = WebUtility.HtmlEncode(logoUrl);
        return $"""
            <div style="background:#f4f7fb;padding:32px;font-family:Arial,sans-serif;color:#16233a"><div style="max-width:600px;margin:auto;background:#fff;border:1px solid #dfe6f1;border-radius:14px;overflow:hidden"><div style="padding:24px 32px;background:#0d6efd"><img src="{safeLogoUrl}" alt="VoltaMarcada" style="height:38px;max-width:190px;object-fit:contain" /></div><div style="padding:32px"><p>Olá, {safeName}</p><p>Seu chamado nº <strong>{number}</strong> foi respondido pela equipe de suporte.</p><div style="padding:18px;border-left:4px solid #0d6efd;background:#f4f7fb;white-space:normal">{safeResponse}</div><p style="margin-top:28px">Att.<br />VoltaMarcada</p><p style="font-size:12px;color:#637085">Não responda este e-mail.</p></div></div></div>
            """;
    }
}

public sealed class CloseSupportTicketUseCase(ISupportTicketRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<OperationResult> ExecuteAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty) return OperationResult.Failure(SupportErrors.Unauthenticated);
        var ticket = await repository.GetForUpdateAsync(ticketId, cancellationToken);
        if (ticket is null) return OperationResult.Failure(SupportErrors.TicketNotFound);
        if (ticket.Status == SupportTicketStatus.Closed) return OperationResult.Failure(SupportErrors.TicketClosed);
        ticket.Close(currentUser.UserId, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}