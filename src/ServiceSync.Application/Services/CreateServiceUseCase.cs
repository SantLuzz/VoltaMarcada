using FluentValidation;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.Services;

public sealed class CreateServiceUseCase(
    ICurrentCompany currentCompany,
    IServiceRepository serviceRepository,
    IValidator<CreateServiceCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<ServiceOperationResult> ExecuteAsync(
        CreateServiceCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ServiceOperationResult.InvalidInput();
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var checklistItems = ToDomainChecklistItems(command.ChecklistItems);
        var service = global::ServiceSync.Domain.Entities.Service.Create(
            companyId,
            command.Name,
            command.Description,
            command.DurationMinutes,
            command.PriceType,
            command.BasePrice,
            command.AllowOnlineBooking,
            command.RequireBeforePhotos,
            command.RequireAfterPhotos,
            checklistItems,
            timeProvider.GetUtcNow());

        await serviceRepository.AddAsync(service, cancellationToken);
        await serviceRepository.SaveChangesAsync(cancellationToken);
        return ServiceOperationResult.Success(service.Id);
    }

    internal static IReadOnlyList<global::ServiceSync.Domain.Entities.ServiceChecklistItem> ToDomainChecklistItems(
        IReadOnlyList<ServiceChecklistItemInput> checklistItems) =>
        checklistItems
            .Select((item, index) => global::ServiceSync.Domain.Entities.ServiceChecklistItem.Create(
                item.Description,
                item.IsRequired,
                index))
            .ToArray();
}
