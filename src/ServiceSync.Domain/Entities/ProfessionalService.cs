using ServiceSync.Domain.Abstractions.Tenancy;
using ServiceSync.Domain.Exceptions;

namespace ServiceSync.Domain.Entities;

public sealed class ProfessionalService : ICompanyScopedEntity
{
    private ProfessionalService()
    {
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid ProfessionalId { get; private set; }

    public Guid ServiceId { get; private set; }

    public static ProfessionalService Create(Guid companyId, Guid professionalId, Guid serviceId)
    {
        if (companyId == Guid.Empty)
        {
            throw new DomainException("Company ID is required.");
        }

        if (professionalId == Guid.Empty)
        {
            throw new DomainException("Professional ID is required.");
        }

        if (serviceId == Guid.Empty)
        {
            throw new DomainException("Service ID is required.");
        }

        return new ProfessionalService
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProfessionalId = professionalId,
            ServiceId = serviceId
        };
    }
}
