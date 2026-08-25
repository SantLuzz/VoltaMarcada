using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Equipment;

public sealed class DeleteEquipmentUseCase(
    ICurrentCompany currentCompany,
    IEquipmentRepository repository,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid?>> ExecuteAsync(Guid equipmentId, CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var equipment = await repository.GetForCompanyAsync(equipmentId, companyId, cancellationToken);
        if (equipment is null)
        {
            return OperationResult<Guid?>.Failure(EquipmentErrors.NotFound);
        }

        equipment.Deactivate(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid?>.Success(equipment.Id);
    }
}
