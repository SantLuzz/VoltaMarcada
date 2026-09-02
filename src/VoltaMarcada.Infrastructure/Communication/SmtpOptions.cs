namespace VoltaMarcada.Infrastructure.Communication;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public bool Enabled { get; init; }

    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 587;

    public bool UseTls { get; init; } = true;

    public string SenderEmail { get; init; } = string.Empty;

    public string SenderName { get; init; } = "VoltaMarcada";

    public string? Username { get; init; }

    public string? Password { get; init; }
}
