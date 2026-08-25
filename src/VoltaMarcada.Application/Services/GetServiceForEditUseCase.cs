using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Services;

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
