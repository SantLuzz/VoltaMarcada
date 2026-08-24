using ServiceSync.Application.Abstractions.Operations;
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
    public async Task<OperationResult<EmployeeInvitationOutcome>> ExecuteAsync(
        CreateEmployeeInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.Unauthenticated);
        }

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid || !BrazilianDocument.TryNormalize(command.Document, out var document))
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.InvalidInput);
        }

        if (await currentCompany.FindCompanyIdAsync(cancellationToken) is not { } companyId)
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.Unauthorized);
        }

        if (!await repository.IsCurrentUserOwnerAsync(currentUser.UserId, companyId, cancellationToken))
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.Unauthorized);
        }

        var email = command.Email.Trim().ToLowerInvariant();
        var now = timeProvider.GetUtcNow();
        if (await accountService.EmailExistsAsync(email, cancellationToken))
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.EmailAlreadyRegistered);
        }

        if (await repository.HasPendingEmailAsync(companyId, email, now, cancellationToken))
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.DuplicateEmail);
        }

        if (await repository.HasPendingOrAcceptedDocumentAsync(companyId, document, now, cancellationToken))
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.DuplicateDocument);
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
        return OperationResult<EmployeeInvitationOutcome>.Success(new EmployeeInvitationOutcome(Token: token.Value));
    }
}
