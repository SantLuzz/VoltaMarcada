using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;
using ServiceSync.Application.Scheduling;

namespace ServiceSync.Application.Professionals;

public sealed class GetProfessionalsUseCase(ICurrentCompany currentCompany, IProfessionalRepository repository)
{
    public async Task<IReadOnlyList<ProfessionalDto>> ExecuteAsync(string? searchFilter = null, CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var professionals = await repository.GetAllForCompanyAsync(companyId, searchFilter, cancellationToken);
        return professionals.Select(ProfessionalDto.FromItem).ToArray();
    }
}

public sealed class GetProfessionalForEditUseCase(ICurrentCompany currentCompany, IProfessionalRepository repository)
{
    public async Task<ProfessionalDto?> ExecuteAsync(Guid professionalId, CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var item = (await repository.GetAllForCompanyAsync(companyId, null, cancellationToken)).SingleOrDefault(professional => professional.Professional.Id == professionalId);
        return item is null ? null : ProfessionalDto.FromItem(item);
    }
}

public sealed class GetCurrentEmployeeProfessionalUseCase(ICurrentUser currentUser, ICurrentCompany currentCompany, IProfessionalRepository repository)
{
    public async Task<ProfessionalDto?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            return null;
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var professional = await repository.GetForCurrentUserAsync(currentUser.UserId, companyId, cancellationToken);
        return professional is null ? null : ProfessionalDto.FromItem(professional);
    }
}

public sealed class GetProfessionalRegistrationOptionsUseCase(ICurrentCompany currentCompany, IProfessionalRepository repository, ISchedulingConfiguration schedulingConfiguration)
{
    public async Task<ProfessionalRegistrationOptionsDto> ExecuteAsync(Guid? professionalId = null, CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var members = await repository.GetAvailableCompanyMembersAsync(companyId, professionalId, cancellationToken);
        var services = await repository.GetActiveServicesAsync(companyId, cancellationToken);
        return new ProfessionalRegistrationOptionsDto(members, services, schedulingConfiguration.AllowedTimeZoneIds.OrderBy(timeZoneId => timeZoneId).ToArray(), schedulingConfiguration.DefaultTimeZoneId);
    }
}
