namespace VoltaMarcada.Domain.Abstractions.Tenancy;

public interface ICompanyScopedEntity
{
    Guid CompanyId { get; }
}
