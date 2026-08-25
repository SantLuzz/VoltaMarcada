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
        return await orders.OrderByDescending(x => x.CreatedAtUtc).Join(dbContext.Customers.Where(x => x.CompanyId == companyId), x => x.CustomerId, x => x.Id, (order, customer) => new { order, customer }).Join(dbContext.Equipment.Where(x => x.CompanyId == companyId), x => x.order.EquipmentId, x => x.Id, (entry, equipment) => new { entry.order, entry.customer, equipment }).Join(dbContext.Services.Where(x => x.CompanyId == companyId), x => x.order.ServiceId, x => x.Id, (entry, service) => new WorkOrderListItem(entry.order.Id, entry.customer.Name, string.IsNullOrWhiteSpace(entry.equipment.Model) ? entry.equipment.Type : $"{entry.equipment.Type} - {entry.equipment.Model}", service.Name, entry.order.Status, entry.order.Priority, entry.order.CreatedAtUtc, entry.order.ProfessionalId)).ToArrayAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}
