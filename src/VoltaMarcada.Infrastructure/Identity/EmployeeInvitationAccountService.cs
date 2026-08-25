using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Persistence;

namespace VoltaMarcada.Infrastructure.Identity;

public sealed class EmployeeInvitationAccountService(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager) : IEmployeeInvitationAccountService
{
    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        await userManager.FindByEmailAsync(email) is not null;

    public async Task<EmployeeInvitationAccountResult> CreateAccountAndAcceptAsync(
        string tokenHash,
        string password,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var invitation = await dbContext.EmployeeInvitations.SingleOrDefaultAsync(
            item => item.TokenHash == tokenHash,
            cancellationToken);
        if (invitation is null || !invitation.IsPending(now))
        {
            await transaction.RollbackAsync(cancellationToken);
            return EmployeeInvitationAccountResult.InvitationUnavailable();
        }

        if (await userManager.FindByEmailAsync(invitation.Email) is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return EmployeeInvitationAccountResult.EmailAlreadyRegistered();
        }

        var user = new ApplicationUser
        {
            FullName = invitation.FullName,
            Document = invitation.Document,
            Phone = invitation.Phone,
            Address = invitation.Address,
            UserName = invitation.Email,
            Email = invitation.Email
        };
        var identityResult = await userManager.CreateAsync(user, password);
        if (!identityResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return identityResult.Errors.Any(error =>
                error.Code is "DuplicateUserName" or "DuplicateEmail")
                ? EmployeeInvitationAccountResult.EmailAlreadyRegistered()
                : new EmployeeInvitationAccountResult(EmployeeInvitationAccountOutcome.InvalidPassword, null);
        }

        dbContext.CompanyUsers.Add(CompanyUser.Create(user.Id, invitation.CompanyId, CompanyUserRole.Employee, now));
        invitation.Accept(now);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return EmployeeInvitationAccountResult.Success(user.Id);
    }

    public async Task<EmployeeProfileUpdateOutcome> UpdateProfileAsync(
        Guid userId,
        string fullName,
        string document,
        string phone,
        string address,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
        if (user is null)
        {
            return EmployeeProfileUpdateOutcome.UserNotFound;
        }

        user.FullName = fullName.Trim();
        user.Document = document;
        user.Phone = phone.Trim();
        user.Address = address.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return EmployeeProfileUpdateOutcome.Success;
    }
}
