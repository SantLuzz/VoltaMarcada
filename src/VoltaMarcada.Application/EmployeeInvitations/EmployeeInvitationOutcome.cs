namespace VoltaMarcada.Application.EmployeeInvitations;

public sealed record EmployeeInvitationOutcome(string? Token = null, Guid? UserId = null);
