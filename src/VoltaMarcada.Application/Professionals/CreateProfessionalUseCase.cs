using VoltaMarcada.Application.Abstractions.Operations;
using FluentValidation;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.Scheduling;

namespace VoltaMarcada.Application.Professionals;

public sealed class CreateProfessionalUseCase(
    ICurrentCompany currentCompany,
    IProfessionalRepository repository,
    ProfessionalOwnerAuthorization ownerAuthorization,
    ISchedulingConfiguration schedulingConfiguration,
    IValidator<CreateProfessionalCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid?>> ExecuteAsync(CreateProfessionalCommand command, CancellationToken cancellationToken = default)
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

        if (!await repository.IsCompanyMemberAvailableAsync(command.CompanyUserId, companyId, cancellationToken: cancellationToken))
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

        var professional = global::VoltaMarcada.Domain.Entities.Professional.Create(companyId, command.CompanyUserId, command.ServiceIds, timeZoneId, timeProvider.GetUtcNow());
        await repository.AddAsync(professional, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid?>.Success(professional.Id);
    }

    private static string? NormalizeTimeZoneId(string? value, ISchedulingConfiguration configuration)
    {
        var timeZoneId = string.IsNullOrWhiteSpace(value) ? configuration.DefaultTimeZoneId : value.Trim();
        return configuration.AllowedTimeZoneIds.Contains(timeZoneId) ? timeZoneId : null;
    }
}
