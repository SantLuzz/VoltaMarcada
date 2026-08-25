using VoltaMarcada.Application.Abstractions.Persistence;

namespace VoltaMarcada.Application.Professionals;

public sealed record ProfessionalDto(
    Guid Id,
    Guid CompanyUserId,
    string CompanyMemberName,
    string CompanyMemberEmail,
    bool IsActive,
    string TimeZoneId,
    IReadOnlyList<Guid> ServiceIds,
    IReadOnlyList<string> ServiceNames)
{
    internal static ProfessionalDto FromItem(ProfessionalListItem item) =>
        new(
            item.Professional.Id,
            item.Professional.CompanyUserId,
            item.CompanyMemberName,
            item.CompanyMemberEmail,
            item.Professional.IsActive,
            item.Professional.TimeZoneId,
            item.ServiceIds,
            item.ServiceNames);
}

public sealed record ProfessionalRegistrationOptionsDto(
    IReadOnlyList<CompanyMemberOption> CompanyMembers,
    IReadOnlyList<ProfessionalServiceOption> Services,
    IReadOnlyList<string> TimeZoneIds,
    string DefaultTimeZoneId);
