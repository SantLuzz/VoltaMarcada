namespace ServiceSync.Application.EmployeeInvitations;

public sealed record CreateEmployeeInvitationCommand(
    string FullName,
    string Email,
    string Document,
    string Phone,
    string Address);
