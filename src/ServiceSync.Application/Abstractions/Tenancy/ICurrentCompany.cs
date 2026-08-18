namespace ServiceSync.Application.Abstractions.Tenancy;

public interface ICurrentCompany
{
    Guid CompanyId { get; }
}
