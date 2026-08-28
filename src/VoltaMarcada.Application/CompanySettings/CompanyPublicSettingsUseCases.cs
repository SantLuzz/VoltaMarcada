using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Application.CompanySettings;

public sealed class GetCompanyPublicSettingsUseCase(MaintenanceOpportunityOwnerAuthorization authorization, ICompanyRepository repository)
{
    public async Task<CompanyPublicSettingsDto?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null)
        {
            return null;
        }

        var company = await repository.GetByIdAsync(companyId.Value, cancellationToken);
        return company is null ? null : new CompanyPublicSettingsDto(
            company.Slug,
            company.TradeName,
            company.PublicDescription,
            company.PublicPhone,
            company.NotificationEmail,
            company.PublicRequestIntervalMinutes,
            company.BusinessHours.OrderBy(item => item.DayOfWeek).Select(item => new CompanyBusinessHoursDto(item.DayOfWeek, item.IsOpen, item.StartTime, item.EndTime)).ToArray());
    }
}

public sealed class UpdateCompanyPublicSettingsUseCase(MaintenanceOpportunityOwnerAuthorization authorization, ICompanyRepository repository, TimeProvider timeProvider)
{
    public async Task<OperationResult> ExecuteAsync(UpdateCompanyPublicSettingsCommand command, CancellationToken cancellationToken = default)
    {
        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null)
        {
            return OperationResult.Failure(CompanySettingsErrors.Forbidden);
        }

        var company = await repository.GetByIdAsync(companyId.Value, cancellationToken);
        if (company is null)
        {
            return OperationResult.Failure(CompanySettingsErrors.NotFound);
        }

        try
        {
            company.UpdatePublicProfile(command.PublicDescription, command.PublicPhone, command.NotificationEmail, timeProvider.GetUtcNow());
            company.UpdatePublicScheduling(command.PublicRequestIntervalMinutes, command.BusinessHours, timeProvider.GetUtcNow());
        }
        catch (DomainException)
        {
            return OperationResult.Failure(CompanySettingsErrors.InvalidInput);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
