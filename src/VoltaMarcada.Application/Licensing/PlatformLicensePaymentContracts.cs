using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Licensing;

public sealed record PlatformLicensePaymentDto(
    Guid Id,
    string CompanyName,
    string OwnerEmail,
    DateTimeOffset? PaidUntilUtc,
    decimal Amount,
    DateTimeOffset SubmittedAtUtc,
    LicensePaymentStatus? Status,
    DateTimeOffset? DecidedAtUtc,
    string? DecisionReason,
    string? OwnerName = null,
    string? ReviewerName = null,
    CompanyLicenseStatus? LicenseStatus = null,
    DateTimeOffset? TrialEndsAtUtc = null);
