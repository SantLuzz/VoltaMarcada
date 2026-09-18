namespace VoltaMarcada.Application.Support;

public sealed record SupportAttachmentInput(string FileName, string DeclaredContentType, byte[] Content);
