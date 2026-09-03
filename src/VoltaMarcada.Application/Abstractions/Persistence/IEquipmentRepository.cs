using VoltaMarcada.Application.Equipment;

namespace VoltaMarcada.Application.Abstractions.Persistence;

public interface IEquipmentRepository
{
    Task AddAsync(global::VoltaMarcada.Domain.Entities.Equipment equipment, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EquipmentListItem>> GetAllForCompanyAsync(
        Guid companyId,
        GetEquipmentQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EquipmentListItem>> GetAllForCustomerAsync(
        Guid customerId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<global::VoltaMarcada.Domain.Entities.Equipment?> GetForCompanyAsync(
        Guid equipmentId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<EquipmentHistoryPageProjection?> GetHistoryPageForCompanyAsync(
        Guid equipmentId,
        Guid companyId,
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<global::VoltaMarcada.Domain.Entities.Equipment?> GetActiveForCustomerByTypeAndManufacturerAsync(
        Guid companyId,
        Guid customerId,
        string type,
        string? manufacturer,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsForCustomerAsync(
        Guid customerId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    void Remove(global::VoltaMarcada.Domain.Entities.Equipment equipment);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed record EquipmentListItem(global::VoltaMarcada.Domain.Entities.Equipment Equipment, string CustomerName);
