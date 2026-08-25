using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Equipment;

public sealed class GetEquipmentUseCase(
    ICurrentCompany currentCompany,
    IEquipmentRepository repository)
{
    public async Task<IReadOnlyList<EquipmentDto>> ExecuteAsync(
        GetEquipmentQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var equipment = await repository.GetAllForCompanyAsync(
            companyId,
            query ?? GetEquipmentQuery.Empty,
            cancellationToken);

        return equipment
            .Select(item => EquipmentDto.FromEntity(item.Equipment, item.CustomerName))
            .ToArray();
    }
}
