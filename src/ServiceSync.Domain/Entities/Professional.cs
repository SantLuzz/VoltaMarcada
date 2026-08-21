using ServiceSync.Domain.Abstractions.Tenancy;
using ServiceSync.Domain.Exceptions;

namespace ServiceSync.Domain.Entities;

public sealed class Professional : ICompanyScopedEntity
{
    private readonly List<ProfessionalService> _professionalServices = [];

    private Professional()
    {
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid CompanyUserId { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<ProfessionalService> ProfessionalServices => _professionalServices;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Professional Create(
        Guid companyId,
        Guid companyUserId,
        IEnumerable<Guid> serviceIds,
        DateTimeOffset now)
    {
        EnsureCompanyAndMember(companyId, companyUserId);
        var normalizedServiceIds = NormalizeServiceIds(serviceIds);
        var utcNow = now.ToUniversalTime();
        var professional = new Professional
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CompanyUserId = companyUserId,
            IsActive = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        professional.ReplaceServices(normalizedServiceIds);
        return professional;
    }

    public void Update(Guid companyUserId, IEnumerable<Guid> serviceIds, DateTimeOffset now)
    {
        if (companyUserId == Guid.Empty)
        {
            throw new DomainException("Company member ID is required.");
        }

        CompanyUserId = companyUserId;
        ReplaceServices(NormalizeServiceIds(serviceIds));
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    private void ReplaceServices(IEnumerable<Guid> serviceIds)
    {
        _professionalServices.Clear();
        foreach (var serviceId in serviceIds)
        {
            _professionalServices.Add(ProfessionalService.Create(CompanyId, Id, serviceId));
        }
    }

    private static void EnsureCompanyAndMember(Guid companyId, Guid companyUserId)
    {
        if (companyId == Guid.Empty)
        {
            throw new DomainException("Company ID is required.");
        }

        if (companyUserId == Guid.Empty)
        {
            throw new DomainException("Company member ID is required.");
        }
    }

    private static Guid[] NormalizeServiceIds(IEnumerable<Guid> serviceIds)
    {
        if (serviceIds is null)
        {
            throw new DomainException("At least one service is required.");
        }

        var normalizedServiceIds = serviceIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (normalizedServiceIds.Length == 0)
        {
            throw new DomainException("At least one service is required.");
        }

        return normalizedServiceIds;
    }
}
