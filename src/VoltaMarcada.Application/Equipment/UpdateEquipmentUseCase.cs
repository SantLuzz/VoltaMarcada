using VoltaMarcada.Application.Abstractions.Operations;
using FluentValidation;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Equipment;

public sealed class UpdateEquipmentUseCase(
    ICurrentCompany currentCompany,
    ICustomerRepository customerRepository,
    IEquipmentRepository equipmentRepository,
    IValidator<UpdateEquipmentCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid?>> ExecuteAsync(
        Guid equipmentId,
        UpdateEquipmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return OperationResult<Guid?>.Failure(EquipmentErrors.InvalidInput);
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var equipment = await equipmentRepository.GetForCompanyAsync(equipmentId, companyId, cancellationToken);
        if (equipment is null)
        {
            return OperationResult<Guid?>.Failure(EquipmentErrors.NotFound);
        }

        var customer = await customerRepository.GetForCompanyAsync(command.CustomerId, companyId, cancellationToken);
        if (customer is null)
        {
            return OperationResult<Guid?>.Failure(EquipmentErrors.CustomerNotFound);
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
        return OperationResult<Guid?>.Success(equipment.Id);
    }
}
