namespace VoltaMarcada.Application.Support;

public sealed record SendSupportRequestCommand(string Subject, string Message, IReadOnlyList<SupportAttachmentInput> Attachments);
