namespace VoltaMarcada.Application.Professionals;

public sealed record CreateProfessionalCommand(Guid CompanyUserId, IReadOnlyList<Guid> ServiceIds, string? TimeZoneId);

public sealed record UpdateProfessionalCommand(Guid CompanyUserId, IReadOnlyList<Guid> ServiceIds, string? TimeZoneId);
