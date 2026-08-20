namespace ServiceSync.Application.EmployeeInvitations;

public sealed record UpdateCompanyMemberProfileCommand(
    Guid MembershipId,
    string FullName,
    string Document,
    string Phone,
    string Address);
