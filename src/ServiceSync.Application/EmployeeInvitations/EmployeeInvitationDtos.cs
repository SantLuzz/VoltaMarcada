using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.EmployeeInvitations;

public sealed record CompanyOverviewDto(
    string LegalName,
    string TradeName,
    string Document,
    string WhatsApp,
    string PostalCode,
    string Address,
    string StateName,
    string CityName,
    string? LogoKey,
    IReadOnlyList<EmployeeDto> Employees,
    IReadOnlyList<EmployeeInvitationDto> Invitations);

public sealed record EmployeeDto(
    Guid MembershipId,
    string FullName,
    string Email,
    string Document,
    string Phone,
    string Address,
    DateTimeOffset CreatedAtUtc,
    CompanyUserRole Role);

public sealed record EmployeeInvitationDto(
    Guid Id,
    string FullName,
    string Email,
    string Document,
    string Phone,
    string Address,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset CreatedAtUtc,
    bool IsPending,
    bool IsRevoked,
    bool IsExpired,
    bool IsAccepted);

public sealed record PublicEmployeeInvitationDto(string FullName, string MaskedEmail);
