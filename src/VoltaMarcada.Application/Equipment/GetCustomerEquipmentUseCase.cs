using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Equipment;

public sealed class GetCustomerEquipmentUseCase(
    ICurrentCompany currentCompany,
    IEquipmentRepository repository)
{
    public async Task<IReadOnlyList<EquipmentDto>> ExecuteAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var equipment = await repository.GetAllForCustomerAsync(customerId, companyId, cancellationToken);

        return equipment
            .Select(item => EquipmentDto.FromEntity(item.Equipment, item.CustomerName))
            .ToArray();
    }
}
