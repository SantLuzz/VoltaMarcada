using VoltaMarcada.Application.Abstractions.Operations;
using FluentValidation;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Equipment;

public sealed class CreateEquipmentUseCase(
    ICurrentCompany currentCompany,
    ICustomerRepository customerRepository,
    IEquipmentRepository equipmentRepository,
    IValidator<CreateEquipmentCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid?>> ExecuteAsync(
        CreateEquipmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return OperationResult<Guid?>.Failure(EquipmentErrors.InvalidInput);
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var customer = await customerRepository.GetForCompanyAsync(command.CustomerId, companyId, cancellationToken);
        if (customer is null)
        {
            return OperationResult<Guid?>.Failure(EquipmentErrors.CustomerNotFound);
        }

        var equipment = global::VoltaMarcada.Domain.Entities.Equipment.Create(
            companyId,
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

        await equipmentRepository.AddAsync(equipment, cancellationToken);
        await equipmentRepository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid?>.Success(equipment.Id);
    }
}
