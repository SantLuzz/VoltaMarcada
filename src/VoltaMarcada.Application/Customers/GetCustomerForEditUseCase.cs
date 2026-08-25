using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Customers;

public sealed class GetCustomerForEditUseCase(
    ICurrentCompany currentCompany,
    ICustomerRepository repository)
{
    public async Task<CustomerDto?> ExecuteAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var customer = await repository.GetForCompanyAsync(customerId, companyId, cancellationToken);

        return customer is null ? null : CustomerDto.FromEntity(customer);
    }
}
