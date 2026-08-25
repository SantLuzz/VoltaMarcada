using VoltaMarcada.Application.Abstractions.Operations;
using FluentValidation;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Services;

public sealed class CreateServiceUseCase(
    ICurrentCompany currentCompany,
    IServiceRepository serviceRepository,
    IValidator<CreateServiceCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid?>> ExecuteAsync(
        CreateServiceCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return OperationResult<Guid?>.Failure(ServiceErrors.InvalidInput);
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var checklistItems = ToDomainChecklistItems(command.ChecklistItems);
        var service = global::VoltaMarcada.Domain.Entities.Service.Create(
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
        return OperationResult<Guid?>.Success(service.Id);
    }

    internal static IReadOnlyList<global::VoltaMarcada.Domain.Entities.ServiceChecklistItem> ToDomainChecklistItems(
        IReadOnlyList<ServiceChecklistItemInput> checklistItems) =>
        checklistItems
            .Select((item, index) => global::VoltaMarcada.Domain.Entities.ServiceChecklistItem.Create(
                item.Description,
                item.IsRequired,
                index))
            .ToArray();
}
