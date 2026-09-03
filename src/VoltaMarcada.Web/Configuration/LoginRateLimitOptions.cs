namespace VoltaMarcada.Web.Configuration;

public sealed class LoginRateLimitOptions
{
    public const string SectionName = "LoginRateLimit";

    public int PermitLimit { get; init; } = 10;

    public int WindowSeconds { get; init; } = 60;

    public int QueueLimit { get; init; }

    public int LockoutMinutes { get; init; } = 15;

    public int MaxFailedAccessAttempts { get; init; } = 5;
}
