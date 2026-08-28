namespace VoltaMarcada.Application.Scheduling;

public sealed record AvailableProfessionalDto(Guid ProfessionalId, string ProfessionalName);

public sealed record AppointmentAlternativeDto(Guid ProfessionalId, string ProfessionalName, DateOnly Date, TimeOnly StartTime);

public sealed record AppointmentRequestAvailabilityAnalysis(
    IReadOnlyList<AvailableProfessionalDto> RequestedTimeProfessionals,
    IReadOnlyList<AppointmentAlternativeDto> Alternatives);
