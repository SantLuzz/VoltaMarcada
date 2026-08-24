using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.MaintenanceOpportunities;
using ServiceSync.Domain.Entities;
using ServiceSync.Domain.Enums;

namespace ServiceSync.Infrastructure.Persistence.Repositories;

public sealed class MaintenanceOpportunityRepository(AppDbContext dbContext) : IMaintenanceOpportunityRepository
{
    public Task AddAsync(MaintenanceOpportunity opportunity, CancellationToken cancellationToken = default) =>
        dbContext.MaintenanceOpportunities.AddAsync(opportunity, cancellationToken).AsTask();

    public Task<MaintenanceOpportunity?> GetForCompanyAsync(Guid opportunityId, Guid companyId, CancellationToken cancellationToken = default) =>
        dbContext.MaintenanceOpportunities.SingleOrDefaultAsync(
            opportunity => opportunity.Id == opportunityId && opportunity.CompanyId == companyId,
            cancellationToken);

    public async Task<IReadOnlyList<MaintenanceOpportunityListItem>> GetListAsync(
        Guid companyId,
        GetMaintenanceOpportunitiesQuery query,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var opportunities = dbContext.MaintenanceOpportunities.AsNoTracking()
            .Where(opportunity => opportunity.CompanyId == companyId);

        if (query.Status is { } status)
        {
            opportunities = opportunities.Where(opportunity => opportunity.Status == status);
        }

        if (query.Urgency is { } urgency)
        {
            opportunities = ApplyUrgencyFilter(opportunities, urgency, today);
        }

        var items = await (
            from opportunity in opportunities
            join customer in dbContext.Customers.AsNoTracking().Where(item => item.CompanyId == companyId)
                on opportunity.CustomerId equals customer.Id
            join equipment in dbContext.Equipment.AsNoTracking().Where(item => item.CompanyId == companyId)
                on opportunity.EquipmentId equals equipment.Id
            join service in dbContext.Services.AsNoTracking().Where(item => item.CompanyId == companyId)
                on opportunity.RecommendedServiceId equals service.Id
            join workOrder in dbContext.WorkOrders.AsNoTracking().Where(item => item.CompanyId == companyId)
                on opportunity.PreviousWorkOrderId equals workOrder.Id
            orderby opportunity.DueDate, customer.Name
            select new
            {
                Opportunity = opportunity,
                CustomerName = customer.Name,
                CustomerWhatsApp = customer.WhatsApp,
                equipment.Type,
                equipment.Manufacturer,
                equipment.Model,
                RecommendedServiceName = service.Name,
                workOrder.CompletedAtUtc
            }).ToArrayAsync(cancellationToken);

        return items.Select(item => new MaintenanceOpportunityListItem(
            item.Opportunity.Id,
            item.CustomerName,
            item.CustomerWhatsApp,
            FormatEquipment(item.Type, item.Manufacturer, item.Model),
            item.CompletedAtUtc ?? item.Opportunity.CreatedAtUtc,
            item.RecommendedServiceName,
            item.Opportunity.DueDate,
            item.Opportunity.Notes,
            item.Opportunity.Status,
            GetUrgency(item.Opportunity.DueDate, today))).ToArray();
    }

    public async Task<MaintenanceOpportunitySummary> GetSummaryAsync(Guid companyId, DateOnly today, CancellationToken cancellationToken = default)
    {
        var openOpportunities = dbContext.MaintenanceOpportunities.AsNoTracking().Where(
            opportunity => opportunity.CompanyId == companyId &&
                           opportunity.Status != MaintenanceOpportunityStatus.Completed &&
                           opportunity.Status != MaintenanceOpportunityStatus.Dismissed);
        var nextSevenDays = today.AddDays(7);
        var nextThirtyDays = today.AddDays(30);
        return new MaintenanceOpportunitySummary(
            await openOpportunities.CountAsync(opportunity => opportunity.DueDate < today, cancellationToken),
            await openOpportunities.CountAsync(opportunity => opportunity.DueDate >= today && opportunity.DueDate <= nextSevenDays, cancellationToken),
            await openOpportunities.CountAsync(opportunity => opportunity.DueDate >= today && opportunity.DueDate <= nextThirtyDays, cancellationToken));
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);

    public async Task<IMaintenanceOpportunityTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new MaintenanceOpportunityTransaction(await dbContext.Database.BeginTransactionAsync(cancellationToken));

    private static IQueryable<MaintenanceOpportunity> ApplyUrgencyFilter(
        IQueryable<MaintenanceOpportunity> opportunities,
        MaintenanceOpportunityUrgency urgency,
        DateOnly today)
    {
        opportunities = opportunities.Where(opportunity =>
            opportunity.Status != MaintenanceOpportunityStatus.Completed &&
            opportunity.Status != MaintenanceOpportunityStatus.Dismissed);
        return urgency switch
        {
            MaintenanceOpportunityUrgency.Overdue => opportunities.Where(opportunity => opportunity.DueDate < today),
            MaintenanceOpportunityUrgency.NextSevenDays => opportunities.Where(opportunity => opportunity.DueDate >= today && opportunity.DueDate <= today.AddDays(7)),
            MaintenanceOpportunityUrgency.NextThirtyDays => opportunities.Where(opportunity => opportunity.DueDate >= today && opportunity.DueDate <= today.AddDays(30)),
            _ => opportunities.Where(opportunity => opportunity.DueDate > today.AddDays(30))
        };
    }

    private static MaintenanceOpportunityUrgency GetUrgency(DateOnly dueDate, DateOnly today) => dueDate < today
        ? MaintenanceOpportunityUrgency.Overdue
        : dueDate <= today.AddDays(7)
            ? MaintenanceOpportunityUrgency.NextSevenDays
            : dueDate <= today.AddDays(30)
                ? MaintenanceOpportunityUrgency.NextThirtyDays
                : MaintenanceOpportunityUrgency.Future;

    private static string FormatEquipment(string type, string? manufacturer, string? model) => string.Join(
        " ",
        new[] { type, manufacturer, model }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private sealed class MaintenanceOpportunityTransaction(IDbContextTransaction transaction) : IMaintenanceOpportunityTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
