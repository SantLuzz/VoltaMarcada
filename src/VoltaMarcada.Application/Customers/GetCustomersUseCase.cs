using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Customers;

public sealed class GetCustomersUseCase(
    ICurrentCompany currentCompany,
    ICustomerRepository repository)
{
    public async Task<IReadOnlyList<CustomerDto>> ExecuteAsync(
        GetCustomersQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var customers = await repository.GetAllForCompanyAsync(
            companyId,
            query ?? GetCustomersQuery.Empty,
            cancellationToken);

        return customers.Select(CustomerDto.FromEntity).ToArray();
    }
}
