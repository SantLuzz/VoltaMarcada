using System.Net;
using VoltaMarcada.Application.Abstractions.Communication;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Support;

public sealed class SendSupportRequestUseCase(
    ICurrentUser currentUser,
    ICurrentCompany currentCompany,
    ISupportRequestContextReader contextReader,
    ISupportRequestDeliveryConfiguration deliveryConfiguration,
    IEmailSender emailSender)
{
    private const int MaximumAttachmentCount = 5;
    private const int MaximumAttachmentBytes = 5 * 1024 * 1024;
    private const int MaximumTotalAttachmentBytes = 20 * 1024 * 1024;

    public async Task<OperationResult> ExecuteAsync(SendSupportRequestCommand command, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty) return OperationResult.Failure(SupportErrors.Unauthenticated);
        if (string.IsNullOrWhiteSpace(command.Subject) || command.Subject.Trim().Length > 200 || string.IsNullOrWhiteSpace(command.Message) || command.Message.Trim().Length > 5000) return OperationResult.Failure(SupportErrors.InvalidInput);
        if (command.Attachments is null || command.Attachments.Count > MaximumAttachmentCount) return OperationResult.Failure(SupportErrors.InvalidAttachment);

        var companyId = await currentCompany.FindCompanyIdAsync(cancellationToken);
        if (companyId is null || companyId == Guid.Empty) return OperationResult.Failure(SupportErrors.CompanyUnavailable);
        var context = await contextReader.GetAsync(currentUser.UserId, companyId.Value, cancellationToken);
        if (context is null) return OperationResult.Failure(SupportErrors.CompanyUnavailable);
        if (string.IsNullOrWhiteSpace(context.UserEmail)) return OperationResult.Failure(SupportErrors.UserEmailUnavailable);
        if (string.IsNullOrWhiteSpace(deliveryConfiguration.RecipientEmail)) return OperationResult.Failure(SupportErrors.RecipientUnavailable);

        var attachments = new List<EmailAttachment>(command.Attachments.Count);
        long totalBytes = 0;
        foreach (var input in command.Attachments)
        {
            if (input.Content is null || input.Content.Length > MaximumAttachmentBytes) return OperationResult.Failure(SupportErrors.InvalidAttachment);
            totalBytes += input.Content.LongLength;
            if (totalBytes > MaximumTotalAttachmentBytes) return OperationResult.Failure(SupportErrors.InvalidAttachment);
            var contentType = SupportAttachmentDetector.DetectContentType(input.Content);
            if (contentType is null) return OperationResult.Failure(SupportErrors.InvalidAttachment);
            attachments.Add(new EmailAttachment(input.FileName, contentType, input.Content));
        }

        var subject = command.Subject.Trim();
        var body = $"<p><strong>Usu?rio:</strong> {WebUtility.HtmlEncode(context.UserName)}</p><p><strong>E-mail:</strong> {WebUtility.HtmlEncode(context.UserEmail)}</p><p><strong>Empresa:</strong> {WebUtility.HtmlEncode(context.CompanyTradeName)} ({WebUtility.HtmlEncode(context.CompanyLegalName)})</p><p>{WebUtility.HtmlEncode(command.Message.Trim()).Replace("\n", "<br />", StringComparison.Ordinal)}</p>";
        await emailSender.SendAsync(new EmailMessage(deliveryConfiguration.RecipientEmail, $"[Support] {subject}", body, context.UserEmail, attachments), cancellationToken);
        return OperationResult.Success();
    }
}

