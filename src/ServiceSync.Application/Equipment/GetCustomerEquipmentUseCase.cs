using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.Equipment;

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
