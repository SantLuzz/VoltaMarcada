using VoltaMarcada.Application.Abstractions.Operations;
using FluentValidation;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Services;

public sealed class UpdateServiceUseCase(
    ICurrentCompany currentCompany,
    IServiceRepository serviceRepository,
    IValidator<UpdateServiceCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid?>> ExecuteAsync(
        Guid serviceId,
        UpdateServiceCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return OperationResult<Guid?>.Failure(ServiceErrors.InvalidInput);
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var service = await serviceRepository.GetForCompanyAsync(serviceId, companyId, cancellationToken);
        if (service is null)
        {
            return OperationResult<Guid?>.Failure(ServiceErrors.NotFound);
        }

        service.Update(
            command.Name,
            command.Description,
            command.DurationMinutes,
            command.PriceType,
            command.BasePrice,
            command.AllowOnlineBooking,
            command.RequireBeforePhotos,
            command.RequireAfterPhotos,
            CreateServiceUseCase.ToDomainChecklistItems(command.ChecklistItems),
            timeProvider.GetUtcNow());

        await serviceRepository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid?>.Success(service.Id);
    }
}
