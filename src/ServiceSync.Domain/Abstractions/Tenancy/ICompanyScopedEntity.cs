namespace ServiceSync.Domain.Abstractions.Tenancy;

public interface ICompanyScopedEntity
{
    Guid CompanyId { get; }
}
