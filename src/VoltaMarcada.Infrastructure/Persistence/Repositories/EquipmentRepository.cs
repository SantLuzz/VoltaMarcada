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

    public async Task<EquipmentHistoryPageProjection?> GetHistoryPageForCompanyAsync(
        Guid equipmentId,
        Guid companyId,
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var header = await (
            from equipment in dbContext.Equipment.AsNoTracking()
            join customer in dbContext.Customers.AsNoTracking() on equipment.CustomerId equals customer.Id
            where equipment.Id == equipmentId && equipment.CompanyId == companyId && customer.CompanyId == companyId
            select new EquipmentHistoryHeaderDto(
                equipment.Id,
                string.IsNullOrWhiteSpace(equipment.Manufacturer) || string.IsNullOrWhiteSpace(equipment.Model)
                    ? equipment.Type
                    : $"{equipment.Type} - {equipment.Manufacturer} {equipment.Model}",
                customer.Name,
                equipment.LocationDescription))
            .SingleOrDefaultAsync(cancellationToken);

        if (header is null)
        {
            return null;
        }

        var completedOrders = dbContext.WorkOrders.AsNoTracking().Where(order =>
            order.CompanyId == companyId &&
            order.EquipmentId == equipmentId &&
            order.Status == global::VoltaMarcada.Domain.Enums.WorkOrderStatus.Completed &&
            order.CompletedAtUtc != null);

        var totalRecordCount = await completedOrders.CountAsync(cancellationToken);
        var records = await completedOrders
            .Join(
                dbContext.Services.AsNoTracking().Where(service => service.CompanyId == companyId),
                order => order.ServiceId,
                service => service.Id,
                (order, service) => new { Order = order, ServiceName = service.Name })
            .OrderByDescending(item => item.Order.CompletedAtUtc)
            .ThenByDescending(item => item.Order.Id)
            .Skip(offset)
            .Take(pageSize)
            .Select(item => new
            {
                item.Order.Id,
                item.Order.CompletedAtUtc,
                item.ServiceName,
                ProfessionalName = (
                    from professional in dbContext.Professionals.AsNoTracking()
                    join membership in dbContext.CompanyUsers.AsNoTracking() on professional.CompanyUserId equals membership.Id
                    join user in dbContext.Users.AsNoTracking() on membership.UserId equals user.Id
                    where professional.CompanyId == companyId && membership.CompanyId == companyId && professional.Id == item.Order.ProfessionalId
                    select string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName)
                    .FirstOrDefault(),
                RequiredChecklistCount = dbContext.WorkOrderChecklistItems.Count(checklist => checklist.CompanyId == companyId && checklist.WorkOrderId == item.Order.Id && checklist.IsRequired),
                CompletedChecklistCount = dbContext.WorkOrderChecklistItems.Count(checklist => checklist.CompanyId == companyId && checklist.WorkOrderId == item.Order.Id && checklist.IsCompleted),
                BeforePhotoCount = dbContext.WorkOrderPhotos.Count(photo => photo.CompanyId == companyId && photo.WorkOrderId == item.Order.Id && photo.Type == global::VoltaMarcada.Domain.Enums.WorkOrderPhotoType.Before),
                AfterPhotoCount = dbContext.WorkOrderPhotos.Count(photo => photo.CompanyId == companyId && photo.WorkOrderId == item.Order.Id && photo.Type == global::VoltaMarcada.Domain.Enums.WorkOrderPhotoType.After),
                AttachmentCount = dbContext.WorkOrderAttachments.Count(attachment => attachment.CompanyId == companyId && attachment.WorkOrderId == item.Order.Id),
                TotalAmount = dbContext.WorkOrderItems.Where(workOrderItem => workOrderItem.CompanyId == companyId && workOrderItem.WorkOrderId == item.Order.Id).Select(workOrderItem => workOrderItem.Quantity * workOrderItem.UnitPrice).DefaultIfEmpty().Sum() - item.Order.DiscountAmount,
                item.Order.ExecutionNotes,
                NextMaintenanceDueDate = dbContext.MaintenanceOpportunities.Where(opportunity => opportunity.CompanyId == companyId && opportunity.PreviousWorkOrderId == item.Order.Id).Select(opportunity => (DateOnly?)opportunity.DueDate).FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return new EquipmentHistoryPageProjection(
            header,
            records.Select(item => new EquipmentHistoryRecordDto(
                $"OS #{item.Id.ToString("N")[..8].ToUpperInvariant()}",
                item.ServiceName,
                item.ProfessionalName,
                item.CompletedAtUtc!.Value,
                item.RequiredChecklistCount,
                item.CompletedChecklistCount,
                item.BeforePhotoCount,
                item.AfterPhotoCount,
                item.AttachmentCount,
                item.TotalAmount,
                item.ExecutionNotes,
                item.NextMaintenanceDueDate)).ToArray(),
            totalRecordCount);
    }


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
