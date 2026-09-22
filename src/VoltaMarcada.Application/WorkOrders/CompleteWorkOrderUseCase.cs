using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.WorkOrders;

public sealed class CompleteWorkOrderUseCase(IWorkOrderRepository repository, IAppointmentRepository appointmentRepository, IServiceRepository serviceRepository, IProfessionalRepository professionalRepository, IMaintenanceOpportunityRepository maintenanceOpportunityRepository, ICompanyRepository companyRepository, IWorkOrderChargeRepository chargeRepository, WorkOrderAuthorization authorization, ICurrentUser currentUser, ISchedulingConfiguration schedulingConfiguration, TimeProvider timeProvider)
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
        if (order.Status != global::VoltaMarcada.Domain.Enums.WorkOrderStatus.InProgress) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidStatus);
        if (await appointmentRepository.HasOpenAppointmentsAsync(scope.CompanyId, order.Id, cancellationToken)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.OpenAppointments);
        if (order.ChecklistItems.Any(item => item.IsRequired && !item.IsCompleted)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ChecklistIncomplete);
        if (service.RequireBeforePhotos && order.Photos.All(photo => photo.Type != global::VoltaMarcada.Domain.Enums.WorkOrderPhotoType.Before)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.BeforePhotoRequired);
        if (service.RequireAfterPhotos && order.Photos.All(photo => photo.Type != global::VoltaMarcada.Domain.Enums.WorkOrderPhotoType.After)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.AfterPhotoRequired);

        var now = timeProvider.GetUtcNow();
        var company = await companyRepository.GetByIdAsync(scope.CompanyId, cancellationToken);
        if (company is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.NotFound);

        if (company.PaymentControlEnabled && order.TotalAmount > 0)
        {
            var noChargeReason = command.PaymentDecision?.NoChargeReason;
            if (!string.IsNullOrWhiteSpace(noChargeReason))
            {
                order.RecordNoCharge(noChargeReason, currentUser.UserId, now);
            }
            else
            {
                if (await chargeRepository.GetForWorkOrderAsync(scope.CompanyId, order.Id, cancellationToken) is not null)
                {
                    return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidStatus);
                }

                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(schedulingConfiguration.DefaultTimeZoneId);
                var businessDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, timeZone).DateTime);
                var installments = command.PaymentDecision?.Installments;
                await chargeRepository.AddAsync(
                    installments is { Count: > 0 }
                        ? WorkOrderCharge.CreateAutomatic(scope.CompanyId, order.Id, order.TotalAmount, installments, businessDate, now)
                        : WorkOrderCharge.CreateAutomatic(scope.CompanyId, order.Id, order.TotalAmount, businessDate, now),
                    cancellationToken);
            }
        }

        var recommendation = command.MaintenanceRecommendation;
        if (recommendation is null)
        {
            order.Complete(service.RequireBeforePhotos, service.RequireAfterPhotos, now);
            order.RecordHistory(global::VoltaMarcada.Domain.Enums.WorkOrderHistoryEventType.Completed, currentUser.UserId == Guid.Empty ? null : currentUser.UserId, WorkOrderHistoryDescriptions.Completed(), now);
            await repository.SaveChangesAsync(cancellationToken);
            return OperationResult<WorkOrderOperationValue>.Success(new WorkOrderOperationValue(order.Id));
        }

        if (!MaintenanceRecommendationDueDateResolver.TryResolve(recommendation, now, schedulingConfiguration, out var dueDate))
        {
            return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidMaintenanceRecommendation);
        }

        var recommendationItems = command.MaintenanceRecommendationItems is { Count: > 0 }
            ? command.MaintenanceRecommendationItems
            : [new MaintenanceRecommendationItemInput(order.EquipmentId, recommendation.RecommendedServiceId)];
        var equipmentIds = order.Items.Where(item => item.EquipmentId is not null).Select(item => item.EquipmentId!.Value).Append(order.EquipmentId).Distinct().ToHashSet();
        if (recommendationItems.Any(item => item.EquipmentId == Guid.Empty || item.RecommendedServiceId == Guid.Empty || !equipmentIds.Contains(item.EquipmentId)) || string.IsNullOrWhiteSpace(recommendation.Notes) || recommendation.Notes.Trim().Length > 2000)
        {
            return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidMaintenanceRecommendation);
        }

        await using var transaction = await maintenanceOpportunityRepository.BeginTransactionAsync(cancellationToken);
        order.Complete(service.RequireBeforePhotos, service.RequireAfterPhotos, now);
        order.RecordHistory(global::VoltaMarcada.Domain.Enums.WorkOrderHistoryEventType.Completed, currentUser.UserId == Guid.Empty ? null : currentUser.UserId, WorkOrderHistoryDescriptions.Completed(), now);
        foreach (var recommendationItem in recommendationItems)
        {
            var recommendedService = await serviceRepository.GetForCompanyAsync(recommendationItem.RecommendedServiceId, scope.CompanyId, cancellationToken);
            if (recommendedService is null || !recommendedService.IsActive)
            {
                return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidMaintenanceRecommendation);
            }

            await maintenanceOpportunityRepository.AddAsync(MaintenanceOpportunity.Create(scope.CompanyId, order.CustomerId, recommendationItem.EquipmentId, order.Id, recommendationItem.RecommendedServiceId, dueDate, recommendation.Notes, now), cancellationToken);
        }
        await repository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<WorkOrderOperationValue>.Success(new WorkOrderOperationValue(order.Id));
    }

}
