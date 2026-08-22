using FluentValidation;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;
using ServiceSync.Application.Scheduling;

namespace ServiceSync.Application.Professionals;

public sealed class CreateProfessionalUseCase(
    ICurrentCompany currentCompany,
    IProfessionalRepository repository,
    ProfessionalOwnerAuthorization ownerAuthorization,
    ISchedulingConfiguration schedulingConfiguration,
    IValidator<CreateProfessionalCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<ProfessionalOperationResult> ExecuteAsync(CreateProfessionalCommand command, CancellationToken cancellationToken = default)
    {
        if (!(await validator.ValidateAsync(command, cancellationToken)).IsValid)
        {
            return ProfessionalOperationResult.InvalidInput();
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        if (!await ownerAuthorization.IsOwnerAsync(companyId, cancellationToken))
        {
            return ProfessionalOperationResult.Forbidden();
        }

        if (!await repository.IsCompanyMemberAvailableAsync(command.CompanyUserId, companyId, cancellationToken: cancellationToken))
        {
            return ProfessionalOperationResult.InvalidCompanyMember();
        }

        if (!await repository.AreActiveServicesValidAsync(companyId, command.ServiceIds, cancellationToken))
        {
            return ProfessionalOperationResult.InvalidService();
        }

        var timeZoneId = NormalizeTimeZoneId(command.TimeZoneId, schedulingConfiguration);
        if (timeZoneId is null)
        {
            return ProfessionalOperationResult.InvalidInput();
        }

        var professional = global::ServiceSync.Domain.Entities.Professional.Create(companyId, command.CompanyUserId, command.ServiceIds, timeZoneId, timeProvider.GetUtcNow());
        await repository.AddAsync(professional, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ProfessionalOperationResult.Success(professional.Id);
    }

    private static string? NormalizeTimeZoneId(string? value, ISchedulingConfiguration configuration)
    {
        var timeZoneId = string.IsNullOrWhiteSpace(value) ? configuration.DefaultTimeZoneId : value.Trim();
        return configuration.AllowedTimeZoneIds.Contains(timeZoneId) ? timeZoneId : null;
    }
}
