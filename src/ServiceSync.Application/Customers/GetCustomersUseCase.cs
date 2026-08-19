using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.Customers;

public sealed class GetCustomersUseCase(
    ICurrentCompany currentCompany,
    ICustomerRepository repository)
{
    public async Task<IReadOnlyList<CustomerDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var customers = await repository.GetAllForCompanyAsync(companyId, cancellationToken);

        return customers.Select(CustomerDto.FromEntity).ToArray();
    }
}
