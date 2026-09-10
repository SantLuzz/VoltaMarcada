using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Domain.Entities;

public sealed class LicenseOverdueNotification
{
    private LicenseOverdueNotification() { }

    public Guid Id { get; private set; }
    public Guid CompanyLicenseId { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public DateOnly SentOn { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static LicenseOverdueNotification Create(Guid companyLicenseId, Guid ownerUserId, DateOnly sentOn, DateTimeOffset now)
    {
        if (companyLicenseId == Guid.Empty || ownerUserId == Guid.Empty)
        {
            throw new DomainException("License and owner IDs are required.");
        }

        return new LicenseOverdueNotification { Id = Guid.NewGuid(), CompanyLicenseId = companyLicenseId, OwnerUserId = ownerUserId, SentOn = sentOn, CreatedAtUtc = now.ToUniversalTime() };
    }
}
