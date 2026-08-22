using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.WorkOrders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Application.Scheduling;

public sealed class ApproveScheduleBlockRequestUseCase(WorkOrderAuthorization authorization, ICurrentUser currentUser, IAppointmentRepository appointmentRepository, IProfessionalRepository professionalRepository, IProfessionalAvailabilityRepository availabilityRepository, TimeProvider timeProvider)
{
    public async Task<SchedulingOperationResult> ExecuteAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (!scope.IsOwner) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.Forbidden);
        var request = await appointmentRepository.GetBlockRequestForAccessAsync(requestId, scope.CompanyId, null, cancellationToken);
        var professional = request is null ? null : await professionalRepository.GetForCompanyAsync(request.ProfessionalId, scope.CompanyId, cancellationToken);
        if (request is null || professional is null) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.NotFound);
        if (request.Status is not (ServiceSync.Domain.Enums.ScheduleBlockRequestStatus.Pending or ServiceSync.Domain.Enums.ScheduleBlockRequestStatus.RequiresAppointmentResolution)) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.InvalidStatus);
        if (!TryGetRequestedInterval(request, professional.TimeZoneId, out var startAtUtc, out var endAtUtc)) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.InvalidInput);
        if (await appointmentRepository.HasAppointmentConflictAsync(scope.CompanyId, professional.Id, startAtUtc, endAtUtc, null, cancellationToken))
        {
            request.RequireAppointmentResolution();
            await appointmentRepository.SaveChangesAsync(cancellationToken);
            return SchedulingOperationResult.Failure(SchedulingOperationResultCode.BlockRequestRequiresAppointmentResolution);
        }

        request.MarkPending();
        request.Approve(currentUser.UserId, timeProvider.GetUtcNow());
        await availabilityRepository.AddScheduleBlockAsync(ScheduleBlock.Create(scope.CompanyId, professional.Id, request.Type, request.StartDate, request.EndDate, request.IsFullDay, request.StartTime, request.EndTime, request.Notes, timeProvider.GetUtcNow()), cancellationToken);
        await appointmentRepository.SaveChangesAsync(cancellationToken);
        return SchedulingOperationResult.Success(request.Id);
    }

    private static bool TryGetRequestedInterval(ScheduleBlockRequest request, string timeZoneId, out DateTimeOffset startAtUtc, out DateTimeOffset endAtUtc)
    {
        var startTime = request.IsFullDay ? TimeOnly.MinValue : request.StartTime!.Value;
        var endDate = request.IsFullDay ? request.EndDate.AddDays(1) : request.EndDate;
        var endTime = request.IsFullDay ? TimeOnly.MinValue : request.EndTime!.Value;
        if (!AppointmentAvailability.TryGetUtcInterval(timeZoneId, request.StartDate, startTime, 1, out startAtUtc, out _))
        {
            endAtUtc = default;
            return false;
        }

        return AppointmentAvailability.TryGetUtcInterval(timeZoneId, endDate, endTime, 1, out endAtUtc, out _);
    }
}

public sealed class RejectScheduleBlockRequestUseCase(WorkOrderAuthorization authorization, ICurrentUser currentUser, IAppointmentRepository appointmentRepository, TimeProvider timeProvider)
{
    public async Task<SchedulingOperationResult> ExecuteAsync(Guid requestId, string? reason, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (!scope.IsOwner) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.Forbidden);
        var request = await appointmentRepository.GetBlockRequestForAccessAsync(requestId, scope.CompanyId, null, cancellationToken);
        if (request is null) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.NotFound);
        if (request.Status is not (ServiceSync.Domain.Enums.ScheduleBlockRequestStatus.Pending or ServiceSync.Domain.Enums.ScheduleBlockRequestStatus.RequiresAppointmentResolution)) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.InvalidStatus);
        request.Reject(currentUser.UserId, reason, timeProvider.GetUtcNow());
        await appointmentRepository.SaveChangesAsync(cancellationToken);
        return SchedulingOperationResult.Success(request.Id);
    }
}
