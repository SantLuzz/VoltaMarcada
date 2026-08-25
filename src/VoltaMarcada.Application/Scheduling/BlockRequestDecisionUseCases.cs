using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.Scheduling;

public sealed class ApproveScheduleBlockRequestUseCase(WorkOrderAuthorization authorization, ICurrentUser currentUser, IAppointmentRepository appointmentRepository, IProfessionalRepository professionalRepository, IProfessionalAvailabilityRepository availabilityRepository, TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (!scope.IsOwner) return OperationResult<Guid>.Failure(SchedulingErrors.Forbidden);
        var request = await appointmentRepository.GetBlockRequestForAccessAsync(requestId, scope.CompanyId, null, cancellationToken);
        var professional = request is null ? null : await professionalRepository.GetForCompanyAsync(request.ProfessionalId, scope.CompanyId, cancellationToken);
        if (request is null || professional is null) return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
        if (request.Status is not (VoltaMarcada.Domain.Enums.ScheduleBlockRequestStatus.Pending or VoltaMarcada.Domain.Enums.ScheduleBlockRequestStatus.RequiresAppointmentResolution)) return OperationResult<Guid>.Failure(SchedulingErrors.InvalidStatus);
        if (!TryGetRequestedInterval(request, professional.TimeZoneId, out var startAtUtc, out var endAtUtc)) return OperationResult<Guid>.Failure(SchedulingErrors.InvalidInput);
        await using var transaction = await appointmentRepository.BeginSchedulingTransactionAsync(scope.CompanyId, professional.Id, cancellationToken);
        if (await appointmentRepository.HasAppointmentConflictAsync(scope.CompanyId, professional.Id, startAtUtc, endAtUtc, null, cancellationToken))
        {
            request.RequireAppointmentResolution();
            await appointmentRepository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperationResult<Guid>.Failure(SchedulingErrors.BlockRequestRequiresAppointmentResolution);
        }

        request.MarkPending();
        request.Approve(currentUser.UserId, timeProvider.GetUtcNow());
        await availabilityRepository.AddScheduleBlockAsync(ScheduleBlock.Create(scope.CompanyId, professional.Id, request.Type, request.StartDate, request.EndDate, request.IsFullDay, request.StartTime, request.EndTime, request.Notes, timeProvider.GetUtcNow()), cancellationToken);
        await appointmentRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<Guid>.Success(request.Id);
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
    public async Task<OperationResult<Guid>> ExecuteAsync(Guid requestId, string? reason, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (!scope.IsOwner) return OperationResult<Guid>.Failure(SchedulingErrors.Forbidden);
        var request = await appointmentRepository.GetBlockRequestForAccessAsync(requestId, scope.CompanyId, null, cancellationToken);
        if (request is null) return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
        if (request.Status is not (VoltaMarcada.Domain.Enums.ScheduleBlockRequestStatus.Pending or VoltaMarcada.Domain.Enums.ScheduleBlockRequestStatus.RequiresAppointmentResolution)) return OperationResult<Guid>.Failure(SchedulingErrors.InvalidStatus);
        request.Reject(currentUser.UserId, reason, timeProvider.GetUtcNow());
        await appointmentRepository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(request.Id);
    }
}
