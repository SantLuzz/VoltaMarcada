using ServiceSync.Application.Abstractions.Operations;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.Customers;

public sealed class DeleteCustomerUseCase(
    ICurrentCompany currentCompany,
    ICustomerRepository repository,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid?>> ExecuteAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var customer = await repository.GetForCompanyAsync(customerId, companyId, cancellationToken);
        if (customer is null)
        {
            return OperationResult<Guid?>.Failure(CustomerErrors.NotFound);
        }

        customer.Deactivate(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid?>.Success(customer.Id);
    }
}
