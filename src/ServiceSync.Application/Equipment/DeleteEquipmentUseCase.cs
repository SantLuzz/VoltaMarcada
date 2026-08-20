using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.Equipment;

public sealed class DeleteEquipmentUseCase(
    ICurrentCompany currentCompany,
    IEquipmentRepository repository)
{
    public async Task<EquipmentOperationResult> ExecuteAsync(Guid equipmentId, CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var equipment = await repository.GetForCompanyAsync(equipmentId, companyId, cancellationToken);
        if (equipment is null)
        {
            return EquipmentOperationResult.NotFound();
        }

        repository.Remove(equipment);
        await repository.SaveChangesAsync(cancellationToken);
        return EquipmentOperationResult.Success(equipment.Id);
    }
}
