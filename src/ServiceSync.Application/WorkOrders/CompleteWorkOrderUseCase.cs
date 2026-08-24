using ServiceSync.Application.Abstractions.Operations;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Scheduling;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Application.WorkOrders;

public sealed class CompleteWorkOrderUseCase(IWorkOrderRepository repository, IAppointmentRepository appointmentRepository, IServiceRepository serviceRepository, IProfessionalRepository professionalRepository, IMaintenanceOpportunityRepository maintenanceOpportunityRepository, WorkOrderAuthorization authorization, ISchedulingConfiguration schedulingConfiguration, TimeProvider timeProvider)
{
    public async Task<OperationResult<WorkOrderOperationValue>> ExecuteAsync(Guid workOrderId, CompleteWorkOrderCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var order = await repository.GetForAccessAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        if (order is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.NotFound);
        var service = await serviceRepository.GetForCompanyAsync(order.ServiceId, scope.CompanyId, cancellationToken);
        if (service is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ServiceNotFound);
        if (order.ProfessionalId is not { } professionalId) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ProfessionalNotQualified);
        var professional = await professionalRepository.GetForCompanyAsync(professionalId, scope.CompanyId, cancellationToken);
        if (professional is null || !professional.IsActive || professional.ProfessionalServices.All(x => x.ServiceId != order.ServiceId)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ProfessionalNotQualified);
        if (order.Status != global::ServiceSync.Domain.Enums.WorkOrderStatus.InProgress) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidStatus);
        if (await appointmentRepository.HasOpenAppointmentsAsync(scope.CompanyId, order.Id, cancellationToken)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.OpenAppointments);
        if (order.ChecklistItems.Any(item => item.IsRequired && !item.IsCompleted)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ChecklistIncomplete);
        if (service.RequireBeforePhotos && order.Photos.All(photo => photo.Type != global::ServiceSync.Domain.Enums.WorkOrderPhotoType.Before)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.BeforePhotoRequired);
        if (service.RequireAfterPhotos && order.Photos.All(photo => photo.Type != global::ServiceSync.Domain.Enums.WorkOrderPhotoType.After)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.AfterPhotoRequired);

        var now = timeProvider.GetUtcNow();
        var recommendation = command.MaintenanceRecommendation;
        if (recommendation is null)
        {
            order.Complete(service.RequireBeforePhotos, service.RequireAfterPhotos, now);
            await repository.SaveChangesAsync(cancellationToken);
            return OperationResult<WorkOrderOperationValue>.Success(new WorkOrderOperationValue(order.Id));
        }

        if (!TryGetDueDate(recommendation, now, out var dueDate))
        {
            return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidMaintenanceRecommendation);
        }

        var recommendedService = await serviceRepository.GetForCompanyAsync(recommendation.RecommendedServiceId, scope.CompanyId, cancellationToken);
        if (recommendedService is null || !recommendedService.IsActive || string.IsNullOrWhiteSpace(recommendation.Notes) || recommendation.Notes.Trim().Length > 2000)
        {
            return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidMaintenanceRecommendation);
        }

        await using var transaction = await maintenanceOpportunityRepository.BeginTransactionAsync(cancellationToken);
        order.Complete(service.RequireBeforePhotos, service.RequireAfterPhotos, now);
        await maintenanceOpportunityRepository.AddAsync(
            MaintenanceOpportunity.Create(
                scope.CompanyId,
                order.CustomerId,
                order.EquipmentId,
                order.Id,
                recommendation.RecommendedServiceId,
                dueDate,
                recommendation.Notes,
                now),
            cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<WorkOrderOperationValue>.Success(new WorkOrderOperationValue(order.Id));
    }

    private bool TryGetDueDate(MaintenanceRecommendationInput recommendation, DateTimeOffset now, out DateOnly dueDate)
    {
        dueDate = default;
        if ((recommendation.IntervalDays is null) == (recommendation.DueDate is null) || recommendation.IntervalDays is <= 0)
        {
            return false;
        }

        if (recommendation.DueDate is { } explicitDueDate)
        {
            dueDate = explicitDueDate;
            return explicitDueDate != DateOnly.MinValue;
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(schedulingConfiguration.DefaultTimeZoneId);
        var completionDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);
        dueDate = completionDate.AddDays(recommendation.IntervalDays!.Value);
        return true;
    }
}
