using VoltaMarcada.Domain.Abstractions.Tenancy;
using VoltaMarcada.Domain.Exceptions;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Domain.Entities;

public sealed class EmployeeInvitation : ICompanyScopedEntity
{
    private EmployeeInvitation()
    {
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Document { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;

    public string TokenHash { get; private set; } = string.Empty;

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? AcceptedAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public static EmployeeInvitation Create(
        Guid companyId,
        string fullName,
        string email,
        string document,
        string phone,
        string address,
        string tokenHash,
        Guid createdByUserId,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset now)
    {
        if (companyId == Guid.Empty)
        {
            throw new DomainException("Company ID is required.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException("Creator user ID is required.");
        }

        if (!BrazilianDocument.TryNormalize(document, out var normalizedDocument) || normalizedDocument.Length != 11)
        {
            throw new DomainException("A valid CPF is required.");
        }

        var createdAtUtc = now.ToUniversalTime();
        var normalizedExpiration = expiresAtUtc.ToUniversalTime();
        if (normalizedExpiration <= createdAtUtc)
        {
            throw new DomainException("Invitation expiration must be after creation.");
        }

        return new EmployeeInvitation
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            FullName = RequireText(fullName, "Full name"),
            Email = NormalizeEmail(email),
            Document = normalizedDocument,
            Phone = RequireText(phone, "Phone"),
            Address = RequireText(address, "Address"),
            TokenHash = RequireText(tokenHash, "Token hash"),
            CreatedByUserId = createdByUserId,
            ExpiresAtUtc = normalizedExpiration,
            CreatedAtUtc = createdAtUtc
        };
    }

    public bool IsPending(DateTimeOffset now) =>
        AcceptedAtUtc is null && RevokedAtUtc is null && ExpiresAtUtc > now.ToUniversalTime();

    public void Revoke(DateTimeOffset now)
    {
        if (!IsPending(now))
        {
            throw new DomainException("Only a pending invitation can be revoked.");
        }

        RevokedAtUtc = now.ToUniversalTime();
    }

    public void Accept(DateTimeOffset now)
    {
        if (!IsPending(now))
        {
            throw new DomainException("Only a pending invitation can be accepted.");
        }

        AcceptedAtUtc = now.ToUniversalTime();
    }

    private static string RequireText(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string NormalizeEmail(string? value)
    {
        var email = RequireText(value, "Email");
        if (!email.Contains('@'))
        {
            throw new DomainException("A valid email is required.");
        }

        return email.ToLowerInvariant();
    }
}
