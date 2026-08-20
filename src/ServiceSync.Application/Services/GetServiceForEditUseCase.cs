using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.Services;

public sealed class GetServiceForEditUseCase(
    ICurrentCompany currentCompany,
    IServiceRepository serviceRepository)
{
    public async Task<ServiceDto?> ExecuteAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var service = await serviceRepository.GetForCompanyAsync(serviceId, companyId, cancellationToken);
        return service is null ? null : ServiceDto.FromEntity(service);
    }
}
