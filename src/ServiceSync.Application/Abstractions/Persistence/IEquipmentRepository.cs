using ServiceSync.Application.Equipment;

namespace ServiceSync.Application.Abstractions.Persistence;

public interface IEquipmentRepository
{
    Task AddAsync(global::ServiceSync.Domain.Entities.Equipment equipment, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EquipmentListItem>> GetAllForCompanyAsync(
        Guid companyId,
        GetEquipmentQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EquipmentListItem>> GetAllForCustomerAsync(
        Guid customerId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<global::ServiceSync.Domain.Entities.Equipment?> GetForCompanyAsync(
        Guid equipmentId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsForCustomerAsync(
        Guid customerId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    void Remove(global::ServiceSync.Domain.Entities.Equipment equipment);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed record EquipmentListItem(global::ServiceSync.Domain.Entities.Equipment Equipment, string CustomerName);
