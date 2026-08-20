using FluentValidation;
using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;
using ServiceSync.Domain.Entities;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Application.EmployeeInvitations;

public sealed class CreateEmployeeInvitationUseCase(
    ICurrentUser currentUser,
    ICurrentCompany currentCompany,
    IEmployeeInvitationRepository repository,
    IEmployeeInvitationAccountService accountService,
    IValidator<CreateEmployeeInvitationCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<EmployeeInvitationResult> ExecuteAsync(
        CreateEmployeeInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            return new EmployeeInvitationResult(EmployeeInvitationResultCode.Unauthenticated);
        }

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid || !BrazilianDocument.TryNormalize(command.Document, out var document))
        {
            return new EmployeeInvitationResult(EmployeeInvitationResultCode.InvalidInput);
        }

        Guid companyId;
        try
        {
            companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        }
        catch (CurrentCompanyNotFoundException)
        {
            return new EmployeeInvitationResult(EmployeeInvitationResultCode.Unauthorized);
        }

        if (!await repository.IsCurrentUserOwnerAsync(currentUser.UserId, companyId, cancellationToken))
        {
            return new EmployeeInvitationResult(EmployeeInvitationResultCode.Unauthorized);
        }

        var email = command.Email.Trim().ToLowerInvariant();
        var now = timeProvider.GetUtcNow();
        if (await accountService.EmailExistsAsync(email, cancellationToken))
        {
            return new EmployeeInvitationResult(EmployeeInvitationResultCode.EmailAlreadyRegistered);
        }

        if (await repository.HasPendingEmailAsync(companyId, email, now, cancellationToken))
        {
            return new EmployeeInvitationResult(EmployeeInvitationResultCode.DuplicateEmail);
        }

        if (await repository.HasPendingOrAcceptedDocumentAsync(companyId, document, now, cancellationToken))
        {
            return new EmployeeInvitationResult(EmployeeInvitationResultCode.DuplicateDocument);
        }

        var token = EmployeeInvitationToken.Create();
        var invitation = EmployeeInvitation.Create(
            companyId,
            command.FullName,
            email,
            document,
            command.Phone,
            command.Address,
            token.Hash,
            currentUser.UserId,
            now.AddDays(7),
            now);

        await repository.AddAsync(invitation, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return EmployeeInvitationResult.Success(token.Value);
    }
}
