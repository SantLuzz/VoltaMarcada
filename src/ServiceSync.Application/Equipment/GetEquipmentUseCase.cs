using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.Equipment;

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
