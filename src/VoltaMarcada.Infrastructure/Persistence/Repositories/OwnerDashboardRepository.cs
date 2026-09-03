using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Dashboard;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class OwnerDashboardRepository(AppDbContext dbContext) : IOwnerDashboardRepository
{
    public async Task<OwnerDashboardDto> GetAsync(
        Guid companyId,
        OwnerDashboardPeriod period,
        CancellationToken cancellationToken = default)
    {
        var openOpportunities = dbContext.MaintenanceOpportunities.AsNoTracking()
            .Where(opportunity => opportunity.CompanyId == companyId &&
                                  opportunity.Status != MaintenanceOpportunityStatus.Converted &&
                                  opportunity.Status != MaintenanceOpportunityStatus.ClosedWithoutConversion);

        var completedWorkOrders = await dbContext.WorkOrders.AsNoTracking()
            .Where(workOrder => workOrder.CompanyId == companyId &&
                                workOrder.Status == WorkOrderStatus.Completed &&
                                workOrder.CompletedAtUtc >= period.MonthStartAtUtc &&
                                workOrder.CompletedAtUtc < period.NextMonthStartAtUtc &&
                                dbContext.AppointmentRequests.AsNoTracking().Any(request =>
                                    request.CompanyId == companyId &&
                                    request.MaintenanceOpportunityId != null &&
                                    request.AppointmentId == workOrder.SourceAppointmentId))
            .Include(workOrder => workOrder.Items)
            .ToArrayAsync(cancellationToken);

        var potentialRevenue = await (
            from opportunity in openOpportunities
            join service in dbContext.Services.AsNoTracking().Where(service => service.CompanyId == companyId)
                on new { opportunity.CompanyId, ServiceId = opportunity.RecommendedServiceId }
                equals new { service.CompanyId, ServiceId = service.Id }
            where service.BasePrice != null
            select service.BasePrice ?? 0m)
            .SumAsync(cancellationToken);

        var actions = new OwnerDashboardActionCounts(
            await dbContext.Appointments.AsNoTracking().CountAsync(
                appointment => appointment.CompanyId == companyId &&
                               appointment.StartAtUtc >= period.TodayStartAtUtc &&
                               appointment.StartAtUtc < period.TomorrowStartAtUtc,
                cancellationToken),
            await openOpportunities.CountAsync(opportunity => opportunity.DueDate < period.BusinessDate, cancellationToken),
            await openOpportunities.CountAsync(
                opportunity => opportunity.DueDate >= period.BusinessDate && opportunity.DueDate <= period.BusinessDate.AddDays(7),
                cancellationToken),
            await dbContext.AppointmentRequests.AsNoTracking().CountAsync(
                request => request.CompanyId == companyId && request.Status == AppointmentRequestStatus.Pending,
                cancellationToken),
            await dbContext.Quotes.AsNoTracking().CountAsync(
                quote => quote.CompanyId == companyId && quote.Status == QuoteStatus.Sent && quote.DecidedAtUtc == null,
                cancellationToken),
            await dbContext.WorkOrders.AsNoTracking().CountAsync(
                workOrder => workOrder.CompanyId == companyId &&
                             (workOrder.Status == WorkOrderStatus.Pending || workOrder.Status == WorkOrderStatus.InProgress),
                cancellationToken));

        var valueFunnel = new OwnerMaintenanceValueFunnel(
            await dbContext.MaintenanceOpportunities.AsNoTracking().CountAsync(
                opportunity => opportunity.CompanyId == companyId &&
                               opportunity.CreatedAtUtc >= period.MonthStartAtUtc &&
                               opportunity.CreatedAtUtc < period.NextMonthStartAtUtc,
                cancellationToken),
            await dbContext.CustomerContacts.AsNoTracking()
                .Where(contact => contact.CompanyId == companyId &&
                                  contact.Status == CustomerContactStatus.Sent &&
                                  contact.SentAtUtc >= period.MonthStartAtUtc &&
                                  contact.SentAtUtc < period.NextMonthStartAtUtc)
                .Select(contact => contact.CustomerId)
                .Distinct()
                .CountAsync(cancellationToken),
            await dbContext.AppointmentRequests.AsNoTracking().CountAsync(
                request => request.CompanyId == companyId &&
                           request.MaintenanceOpportunityId != null &&
                           request.CreatedAtUtc >= period.MonthStartAtUtc &&
                           request.CreatedAtUtc < period.NextMonthStartAtUtc,
                cancellationToken),
            await dbContext.MaintenanceOpportunities.AsNoTracking().CountAsync(
                opportunity => opportunity.CompanyId == companyId &&
                               opportunity.ConversionType == MaintenanceOpportunityConversionType.Appointment &&
                               opportunity.ConvertedAtUtc >= period.MonthStartAtUtc &&
                               opportunity.ConvertedAtUtc < period.NextMonthStartAtUtc,
                cancellationToken),
            completedWorkOrders.Length,
            completedWorkOrders.Sum(workOrder => workOrder.TotalAmount),
            await openOpportunities.CountAsync(cancellationToken),
            potentialRevenue);

        return new OwnerDashboardDto(actions, valueFunnel);
    }
}
