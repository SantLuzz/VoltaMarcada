using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Equipment;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class EquipmentRepository(AppDbContext dbContext) : IEquipmentRepository
{
    public async Task AddAsync(global::VoltaMarcada.Domain.Entities.Equipment equipment, CancellationToken cancellationToken = default) =>
        await dbContext.Equipment.AddAsync(equipment, cancellationToken);

    public async Task<IReadOnlyList<EquipmentListItem>> GetAllForCompanyAsync(
        Guid companyId,
        GetEquipmentQuery query,
        CancellationToken cancellationToken = default)
    {
        var equipment = dbContext.Equipment
            .AsNoTracking()
            .Where(item => item.CompanyId == companyId)
            .Join(
                dbContext.Customers.AsNoTracking().Where(customer => customer.CompanyId == companyId),
                item => item.CustomerId,
                customer => customer.Id,
                (item, customer) => new { Equipment = item, CustomerName = customer.Name });

        if (query.SearchFilter is { } searchFilter)
        {
            var pattern = ToContainsPattern(searchFilter);
            equipment = equipment.Where(item =>
                EF.Functions.ILike(item.Equipment.Type, pattern, "\\") ||
                EF.Functions.ILike(item.CustomerName, pattern, "\\") ||
                (item.Equipment.Manufacturer != null && EF.Functions.ILike(item.Equipment.Manufacturer, pattern, "\\")) ||
                (item.Equipment.Model != null && EF.Functions.ILike(item.Equipment.Model, pattern, "\\")) ||
                (item.Equipment.SerialNumber != null && EF.Functions.ILike(item.Equipment.SerialNumber, pattern, "\\")));
        }

        var results = await equipment
            .OrderBy(item => item.Equipment.Type)
            .ThenBy(item => item.CustomerName)
            .ToListAsync(cancellationToken);

        return results
            .Select(item => new EquipmentListItem(item.Equipment, item.CustomerName))
            .ToArray();
    }

    public Task<global::VoltaMarcada.Domain.Entities.Equipment?> GetForCompanyAsync(
        Guid equipmentId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        dbContext.Equipment.SingleOrDefaultAsync(
            equipment => equipment.Id == equipmentId && equipment.CompanyId == companyId,
            cancellationToken);

    public Task<global::VoltaMarcada.Domain.Entities.Equipment?> GetActiveForCustomerByTypeAndManufacturerAsync(
        Guid companyId,
        Guid customerId,
        string type,
        string? manufacturer,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = type.Trim().ToUpperInvariant();
        var normalizedManufacturer = string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer.Trim().ToUpperInvariant();
        var equipment = dbContext.Equipment.Where(
            equipment => equipment.CompanyId == companyId &&
                         equipment.CustomerId == customerId &&
                         equipment.IsActive &&
                         equipment.Type.ToUpper() == normalizedType);
        return normalizedManufacturer is null
            ? equipment.SingleOrDefaultAsync(item => item.Manufacturer == null, cancellationToken)
            : equipment.SingleOrDefaultAsync(item => item.Manufacturer != null && item.Manufacturer.ToUpper() == normalizedManufacturer, cancellationToken);
    }

    public async Task<IReadOnlyList<EquipmentListItem>> GetAllForCustomerAsync(
        Guid customerId,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var results = await dbContext.Equipment
            .AsNoTracking()
            .Where(equipment => equipment.CustomerId == customerId && equipment.CompanyId == companyId)
            .Join(
                dbContext.Customers.AsNoTracking().Where(customer => customer.CompanyId == companyId),
                equipment => equipment.CustomerId,
                customer => customer.Id,
                (equipment, customer) => new { Equipment = equipment, CustomerName = customer.Name })
            .OrderBy(item => item.Equipment.Type)
            .ToListAsync(cancellationToken);

        return results
            .Select(item => new EquipmentListItem(item.Equipment, item.CustomerName))
            .ToArray();
    }

    public Task<bool> ExistsForCustomerAsync(
        Guid customerId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        dbContext.Equipment.AnyAsync(
            equipment => equipment.CustomerId == customerId && equipment.CompanyId == companyId,
            cancellationToken);

    public void Remove(global::VoltaMarcada.Domain.Entities.Equipment equipment) =>
        dbContext.Equipment.Remove(equipment);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private static string ToContainsPattern(string value) =>
        $"%{value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
}
