using ServiceSync.Domain.Abstractions.Tenancy;
using ServiceSync.Domain.Enums;
using ServiceSync.Domain.Exceptions;

namespace ServiceSync.Domain.Entities;

public sealed class CompanyUser : ICompanyScopedEntity
{
    private CompanyUser()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid CompanyId { get; private set; }

    public CompanyUserRole Role { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static CompanyUser Create(
        Guid userId,
        Guid companyId,
        CompanyUserRole role,
        DateTimeOffset now)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User ID is required.");
        }

        if (companyId == Guid.Empty)
        {
            throw new DomainException("Company ID is required.");
        }

        if (!Enum.IsDefined(role))
        {
            throw new DomainException("Company user role is invalid.");
        }

        return new CompanyUser
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CompanyId = companyId,
            Role = role,
            CreatedAtUtc = now.ToUniversalTime()
        };
    }
}
