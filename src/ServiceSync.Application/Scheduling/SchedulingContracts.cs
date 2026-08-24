using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.Scheduling;

public sealed record CreateAppointmentCommand(
    Guid ProfessionalId,
    DateOnly Date,
    TimeOnly StartTime,
    int DurationMinutes,
    Guid? CustomerId,
    Guid? EquipmentId,
    Guid? ServiceId,
    Guid? WorkOrderId,
    string? Notes);

public sealed record RescheduleAppointmentCommand(DateOnly Date, TimeOnly StartTime, string Reason);

public sealed record AdjustAppointmentCommand(Guid ProfessionalId, DateOnly Date, TimeOnly StartTime, string Reason, Guid? WorkOrderId);

public sealed record UpdateAppointmentNotesCommand(string? Notes);

public sealed record CreateScheduleBlockRequestCommand(
    ScheduleBlockType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsFullDay,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Notes);

public sealed record AppointmentDto(
    Guid Id,
    Guid ProfessionalId,
    Guid? CustomerId,
    Guid? EquipmentId,
    Guid? ServiceId,
    Guid? WorkOrderId,
    DateTimeOffset StartAtUtc,
    DateTimeOffset EndAtUtc,
    AppointmentStatus Status,
    string? Notes,
    string ProfessionalTimeZoneId,
    string ProfessionalName,
    string? CustomerName,
    string? EquipmentName,
    string? ServiceName,
    IReadOnlyList<AppointmentRescheduleHistoryDto> RescheduleHistory);

public sealed record AppointmentRescheduleHistoryDto(
    DateTimeOffset PreviousStartAtUtc,
    DateTimeOffset PreviousEndAtUtc,
    DateTimeOffset NewStartAtUtc,
    DateTimeOffset NewEndAtUtc,
    string Reason,
    DateTimeOffset CreatedAtUtc);

public sealed record AppointmentCustomerDetailsDto(
    Guid Id,
    string Name,
    string WhatsApp,
    string? Email,
    string? PrimaryAddress);

public sealed record AppointmentDetailsDto(
    Guid Id,
    Guid ProfessionalId,
    Guid? WorkOrderId,
    DateTimeOffset StartAtUtc,
    DateTimeOffset EndAtUtc,
    AppointmentStatus Status,
    string? Notes,
    AppointmentCustomerDetailsDto? Customer,
    string ProfessionalTimeZoneId);

public sealed record AppointmentSlotDto(TimeOnly StartTime);

public sealed record ScheduleBlockRequestDto(
    Guid Id,
    Guid ProfessionalId,
    string ProfessionalName,
    ScheduleBlockType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsFullDay,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Notes,
    ScheduleBlockRequestStatus Status,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<string> AffectedAppointments);
