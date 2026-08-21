using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.Equipment;

public sealed class DeleteEquipmentUseCase(
    ICurrentCompany currentCompany,
    IEquipmentRepository repository,
    TimeProvider timeProvider)
{
    public async Task<EquipmentOperationResult> ExecuteAsync(Guid equipmentId, CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var equipment = await repository.GetForCompanyAsync(equipmentId, companyId, cancellationToken);
        if (equipment is null)
        {
            return EquipmentOperationResult.NotFound();
        }

        equipment.Deactivate(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return EquipmentOperationResult.Success(equipment.Id);
    }
}
