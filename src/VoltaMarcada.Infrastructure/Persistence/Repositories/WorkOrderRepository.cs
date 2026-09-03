using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class WorkOrderRepository(AppDbContext dbContext) : IWorkOrderRepository
{
    public async Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default) => await dbContext.WorkOrders.AddAsync(workOrder, cancellationToken);

    public Task<WorkOrder?> GetForAccessAsync(Guid workOrderId, Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default) =>
        dbContext.WorkOrders.Include(x => x.Items).Include(x => x.ChecklistItems).Include(x => x.Photos).SingleOrDefaultAsync(x => x.Id == workOrderId && x.CompanyId == companyId && (professionalId == null || x.ProfessionalId == professionalId), cancellationToken);

    public async Task<IReadOnlyList<WorkOrderListItem>> GetAllForAccessAsync(Guid companyId, Guid? professionalId, GetWorkOrdersQuery query, CancellationToken cancellationToken = default)
    {
        var orders = dbContext.WorkOrders.AsNoTracking().Where(x => x.CompanyId == companyId && (professionalId == null || x.ProfessionalId == professionalId));
        if (query.Status is { } status) orders = orders.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(query.SearchFilter))
        {
            var term = query.SearchFilter.Trim().ToLower();
            orders = orders.Where(x => dbContext.Customers.Any(c => c.Id == x.CustomerId && c.CompanyId == companyId && c.Name.ToLower().Contains(term)) || dbContext.Services.Any(s => s.Id == x.ServiceId && s.CompanyId == companyId && s.Name.ToLower().Contains(term)));
        }
        return await orders
            .GroupJoin(
                dbContext.Appointments.AsNoTracking().Where(appointment => appointment.CompanyId == companyId && appointment.WorkOrderId != null),
                order => order.Id,
                appointment => appointment.WorkOrderId!.Value,
                (order, appointments) => new { Order = order, ScheduledAtUtc = appointments.Select(appointment => (DateTimeOffset?)appointment.StartAtUtc).Min() })
            .OrderBy(item => item.Order.Status == VoltaMarcada.Domain.Enums.WorkOrderStatus.Completed)
            .ThenBy(item => item.ScheduledAtUtc ?? item.Order.CreatedAtUtc)
            .ThenBy(item => item.Order.CreatedAtUtc)
            .Join(dbContext.Customers.Where(customer => customer.CompanyId == companyId), item => item.Order.CustomerId, customer => customer.Id, (item, customer) => new { item.Order, item.ScheduledAtUtc, Customer = customer })
            .Join(dbContext.Equipment.Where(equipment => equipment.CompanyId == companyId), item => item.Order.EquipmentId, equipment => equipment.Id, (item, equipment) => new { item.Order, item.ScheduledAtUtc, item.Customer, Equipment = equipment })
            .Join(dbContext.Services.Where(service => service.CompanyId == companyId), item => item.Order.ServiceId, service => service.Id, (item, service) => new WorkOrderListItem(item.Order.Id, item.Customer.Name, string.IsNullOrWhiteSpace(item.Equipment.Model) ? item.Equipment.Type : $"{item.Equipment.Type} - {item.Equipment.Model}", service.Name, item.Order.Status, item.Order.Priority, item.Order.CreatedAtUtc, item.Order.ProfessionalId, item.ScheduledAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}
