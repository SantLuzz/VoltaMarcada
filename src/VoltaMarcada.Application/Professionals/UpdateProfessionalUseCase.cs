using VoltaMarcada.Application.Abstractions.Operations;
using FluentValidation;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.Scheduling;

namespace VoltaMarcada.Application.Professionals;

public sealed class UpdateProfessionalUseCase(
    ICurrentCompany currentCompany,
    IProfessionalRepository repository,
    ProfessionalOwnerAuthorization ownerAuthorization,
    ISchedulingConfiguration schedulingConfiguration,
    IValidator<UpdateProfessionalCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid?>> ExecuteAsync(Guid professionalId, UpdateProfessionalCommand command, CancellationToken cancellationToken = default)
    {
        if (!(await validator.ValidateAsync(command, cancellationToken)).IsValid)
        {
            return OperationResult<Guid?>.Failure(ProfessionalErrors.InvalidInput);
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        if (!await ownerAuthorization.IsOwnerAsync(companyId, cancellationToken))
        {
            return OperationResult<Guid?>.Failure(ProfessionalErrors.Forbidden);
        }

        var professional = await repository.GetForCompanyAsync(professionalId, companyId, cancellationToken);
        if (professional is null)
        {
            return OperationResult<Guid?>.Failure(ProfessionalErrors.NotFound);
        }

        if (!await repository.IsCompanyMemberAvailableAsync(command.CompanyUserId, companyId, professionalId, cancellationToken))
        {
            return OperationResult<Guid?>.Failure(ProfessionalErrors.InvalidCompanyMember);
        }

        if (!await repository.AreActiveServicesValidAsync(companyId, command.ServiceIds, cancellationToken))
        {
            return OperationResult<Guid?>.Failure(ProfessionalErrors.InvalidService);
        }

        var timeZoneId = NormalizeTimeZoneId(command.TimeZoneId, schedulingConfiguration);
        if (timeZoneId is null)
        {
            return OperationResult<Guid?>.Failure(ProfessionalErrors.InvalidInput);
        }

        professional.Update(command.CompanyUserId, command.ServiceIds, timeZoneId, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid?>.Success(professional.Id);
    }

    private static string? NormalizeTimeZoneId(string? value, ISchedulingConfiguration configuration)
    {
        var timeZoneId = string.IsNullOrWhiteSpace(value) ? configuration.DefaultTimeZoneId : value.Trim();
        return configuration.AllowedTimeZoneIds.Contains(timeZoneId) ? timeZoneId : null;
    }
}
