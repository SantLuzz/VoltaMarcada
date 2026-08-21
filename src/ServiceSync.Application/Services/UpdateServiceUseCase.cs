using FluentValidation;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.Services;

public sealed class UpdateServiceUseCase(
    ICurrentCompany currentCompany,
    IServiceRepository serviceRepository,
    IValidator<UpdateServiceCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<ServiceOperationResult> ExecuteAsync(
        Guid serviceId,
        UpdateServiceCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ServiceOperationResult.InvalidInput();
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var service = await serviceRepository.GetForCompanyAsync(serviceId, companyId, cancellationToken);
        if (service is null)
        {
            return ServiceOperationResult.NotFound();
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
        return ServiceOperationResult.Success(service.Id);
    }
}
