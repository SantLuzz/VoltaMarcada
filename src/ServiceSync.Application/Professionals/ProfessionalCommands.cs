namespace ServiceSync.Application.Professionals;

public sealed record CreateProfessionalCommand(Guid CompanyUserId, IReadOnlyList<Guid> ServiceIds);

public sealed record UpdateProfessionalCommand(Guid CompanyUserId, IReadOnlyList<Guid> ServiceIds);
