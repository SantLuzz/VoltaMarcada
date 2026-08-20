using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.Services;

public sealed class DeleteServiceUseCase(
    ICurrentCompany currentCompany,
    IServiceRepository repository)
{
    public async Task<ServiceOperationResult> ExecuteAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var service = await repository.GetForCompanyAsync(serviceId, companyId, cancellationToken);
        if (service is null)
        {
            return ServiceOperationResult.NotFound();
        }

        repository.Remove(service);
        await repository.SaveChangesAsync(cancellationToken);
        return ServiceOperationResult.Success(service.Id);
    }
}
