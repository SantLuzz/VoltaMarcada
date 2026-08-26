using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class MaintenanceOpportunityRepository(AppDbContext dbContext) : IMaintenanceOpportunityRepository
{
    public Task AddAsync(MaintenanceOpportunity opportunity, CancellationToken cancellationToken = default) =>
        dbContext.MaintenanceOpportunities.AddAsync(opportunity, cancellationToken).AsTask();

    public Task<MaintenanceOpportunity?> GetForCompanyAsync(Guid opportunityId, Guid companyId, CancellationToken cancellationToken = default) =>
        dbContext.MaintenanceOpportunities.SingleOrDefaultAsync(
            opportunity => opportunity.Id == opportunityId && opportunity.CompanyId == companyId,
            cancellationToken);

    public Task<MaintenanceBookingContext?> GetBookingContextAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        (
            from opportunity in dbContext.MaintenanceOpportunities
            join company in dbContext.Companies.AsNoTracking() on opportunity.CompanyId equals company.Id
            join customer in dbContext.Customers.AsNoTracking() on new { opportunity.CompanyId, opportunity.CustomerId } equals new { customer.CompanyId, CustomerId = customer.Id }
            join equipment in dbContext.Equipment.AsNoTracking() on new { opportunity.CompanyId, opportunity.EquipmentId } equals new { equipment.CompanyId, EquipmentId = equipment.Id }
            join service in dbContext.Services.AsNoTracking() on new { opportunity.CompanyId, ServiceId = opportunity.RecommendedServiceId } equals new { service.CompanyId, ServiceId = service.Id }
            where opportunity.BookingTokenHash == tokenHash && opportunity.BookingTokenRevokedAtUtc == null
            select new MaintenanceBookingContext(
                opportunity,
                company.Slug,
                company.TradeName,
                company.LogoKey,
                company.WhatsApp,
                customer.Name,
                FormatEquipment(equipment.Type, equipment.Manufacturer, equipment.Model),
                service.Name,
                service.DurationMinutes,
                service.IsActive)
        ).SingleOrDefaultAsync(cancellationToken);

    public Task<MaintenanceOpportunityContactContext?> GetContactContextAsync(
        Guid opportunityId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        (
            from opportunity in dbContext.MaintenanceOpportunities.AsNoTracking()
            join customer in dbContext.Customers.AsNoTracking()
                on opportunity.CustomerId equals customer.Id
            join equipment in dbContext.Equipment.AsNoTracking()
                on opportunity.EquipmentId equals equipment.Id
            join service in dbContext.Services.AsNoTracking()
                on opportunity.RecommendedServiceId equals service.Id
            where opportunity.Id == opportunityId &&
                  opportunity.CompanyId == companyId &&
                  customer.CompanyId == companyId &&
                  equipment.CompanyId == companyId &&
                  service.CompanyId == companyId
            select new MaintenanceOpportunityContactContext(
                opportunity.Id,
                opportunity.CustomerId,
                customer.Name,
                customer.WhatsApp,
                FormatEquipment(equipment.Type, equipment.Manufacturer, equipment.Model),
                service.Name,
                opportunity.DueDate,
                opportunity.Status)
        ).SingleOrDefaultAsync(cancellationToken);

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

        var opportunityIds = items.Select(item => item.Opportunity.Id).ToArray();
        var contacts = await dbContext.CustomerContacts.AsNoTracking()
            .Where(contact => contact.CompanyId == companyId && opportunityIds.Contains(contact.OpportunityId))
            .OrderByDescending(contact => contact.CreatedAtUtc)
            .Select(contact => new
            {
                contact.OpportunityId,
                Contact = new CustomerContactHistoryItem(
                    contact.Id,
                    contact.Channel,
                    contact.Status,
                    contact.Message,
                    contact.CreatedAtUtc,
                    contact.SentAtUtc)
            })
            .ToArrayAsync(cancellationToken);
        var contactsByOpportunity = contacts
            .GroupBy(item => item.OpportunityId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<CustomerContactHistoryItem>)group.Select(item => item.Contact).ToArray());

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
            GetUrgency(item.Opportunity.DueDate, today),
            contactsByOpportunity.GetValueOrDefault(item.Opportunity.Id, []))).ToArray();
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
