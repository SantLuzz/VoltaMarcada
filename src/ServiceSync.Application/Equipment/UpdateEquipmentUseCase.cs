using FluentValidation;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.Equipment;

public sealed class UpdateEquipmentUseCase(
    ICurrentCompany currentCompany,
    ICustomerRepository customerRepository,
    IEquipmentRepository equipmentRepository,
    IValidator<UpdateEquipmentCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<EquipmentOperationResult> ExecuteAsync(
        Guid equipmentId,
        UpdateEquipmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return EquipmentOperationResult.InvalidInput();
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var equipment = await equipmentRepository.GetForCompanyAsync(equipmentId, companyId, cancellationToken);
        if (equipment is null)
        {
            return EquipmentOperationResult.NotFound();
        }

        var customer = await customerRepository.GetForCompanyAsync(command.CustomerId, companyId, cancellationToken);
        if (customer is null)
        {
            return EquipmentOperationResult.CustomerNotFound();
        }

        equipment.Update(
            customer.Id,
            command.Type,
            command.Manufacturer,
            command.Model,
            command.SerialNumber,
            command.Capacity,
            command.LocationDescription,
            command.InstallationDate,
            command.Notes,
            timeProvider.GetUtcNow());

        await equipmentRepository.SaveChangesAsync(cancellationToken);
        return EquipmentOperationResult.Success(equipment.Id);
    }
}
