using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Equipment;

public sealed class GetEquipmentForEditUseCase(
    ICurrentCompany currentCompany,
    IEquipmentRepository equipmentRepository,
    ICustomerRepository customerRepository)
{
    public async Task<EquipmentDto?> ExecuteAsync(Guid equipmentId, CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var equipment = await equipmentRepository.GetForCompanyAsync(equipmentId, companyId, cancellationToken);
        if (equipment is null)
        {
            return null;
        }

        var customer = await customerRepository.GetForCompanyAsync(
            equipment.CustomerId,
            companyId,
            cancellationToken);
        return customer is null ? null : EquipmentDto.FromEntity(equipment, customer.Name);
    }
}
