using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.MaintenanceOpportunities;

public sealed class CreateManualMaintenanceOpportunityUseCase(
    IMaintenanceOpportunityRepository maintenanceOpportunityRepository,
    MaintenanceOpportunityOwnerAuthorization authorization,
    ICustomerRepository customerRepository,
    IEquipmentRepository equipmentRepository,
    IServiceRepository serviceRepository,
    ISchedulingConfiguration schedulingConfiguration,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(
        CreateManualMaintenanceOpportunityCommand command,
        CancellationToken cancellationToken = default)
    {
        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null)
        {
            return OperationResult<Guid>.Failure(MaintenanceOpportunityErrors.Forbidden);
        }

        var now = timeProvider.GetUtcNow();
        if (!MaintenanceRecommendationDueDateResolver.TryResolve(
                command.Recommendation,
                now,
                schedulingConfiguration,
                out var dueDate) ||
            string.IsNullOrWhiteSpace(command.Recommendation?.Notes) ||
            command.Recommendation.Notes.Trim().Length > 2000)
        {
            return OperationResult<Guid>.Failure(MaintenanceOpportunityErrors.InvalidInput);
        }

        var customer = await customerRepository.GetForCompanyAsync(command.CustomerId, companyId.Value, cancellationToken);
        var equipment = await equipmentRepository.GetForCompanyAsync(command.EquipmentId, companyId.Value, cancellationToken);
        var service = await serviceRepository.GetForCompanyAsync(command.Recommendation.RecommendedServiceId, companyId.Value, cancellationToken);
        if (customer is null || equipment is null || equipment.CustomerId != customer.Id || service is null || !service.IsActive)
        {
            return OperationResult<Guid>.Failure(MaintenanceOpportunityErrors.InvalidInput);
        }

        var opportunity = MaintenanceOpportunity.CreateManual(
            companyId.Value,
            customer.Id,
            equipment.Id,
            service.Id,
            dueDate,
            command.Recommendation.Notes,
            now);
        await maintenanceOpportunityRepository.AddAsync(opportunity, cancellationToken);
        await maintenanceOpportunityRepository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(opportunity.Id);
    }
}

internal static class MaintenanceRecommendationDueDateResolver
{
    public static bool TryResolve(
        MaintenanceRecommendationInput? recommendation,
        DateTimeOffset now,
        ISchedulingConfiguration schedulingConfiguration,
        out DateOnly dueDate)
    {
        dueDate = default;
        if (recommendation is null ||
            (recommendation.IntervalDays is null) == (recommendation.DueDate is null) ||
            recommendation.IntervalDays is <= 0)
        {
            return false;
        }

        if (recommendation.DueDate is { } explicitDueDate)
        {
            dueDate = explicitDueDate;
            return explicitDueDate != DateOnly.MinValue;
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(schedulingConfiguration.DefaultTimeZoneId);
        var businessDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);
        var intervalDays = recommendation.IntervalDays!.Value;
        if (intervalDays > DateOnly.MaxValue.DayNumber - businessDate.DayNumber)
        {
            return false;
        }

        dueDate = businessDate.AddDays(intervalDays);
        return true;
    }
}
