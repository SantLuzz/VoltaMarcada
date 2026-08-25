using VoltaMarcada.Application.Abstractions.Persistence;

namespace VoltaMarcada.Application.EmployeeInvitations;

public sealed class GetEmployeeInvitationForAcceptanceUseCase(
    IEmployeeInvitationRepository repository,
    TimeProvider timeProvider)
{
    public async Task<PublicEmployeeInvitationDto?> ExecuteAsync(
        string? token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var invitation = await repository.GetPendingByTokenHashAsync(
            EmployeeInvitationToken.CreateHash(token),
            timeProvider.GetUtcNow(),
            cancellationToken);
        return invitation is null
            ? null
            : new PublicEmployeeInvitationDto(invitation.FullName, MaskEmail(invitation.Email));
    }

    private static string MaskEmail(string email)
    {
        var separatorIndex = email.IndexOf('@', StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return "***";
        }

        var visibleLength = Math.Min(2, separatorIndex);
        return email[..visibleLength] + "***" + email[separatorIndex..];
    }
}
