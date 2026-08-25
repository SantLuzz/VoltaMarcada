using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Services;

public sealed class GetServicesUseCase(
    ICurrentCompany currentCompany,
    IServiceRepository repository)
{
    public async Task<IReadOnlyList<ServiceDto>> ExecuteAsync(
        GetServicesQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var services = await repository.GetAllForCompanyAsync(
            companyId,
            query ?? GetServicesQuery.Empty,
            cancellationToken);

        return services.Select(ServiceDto.FromEntity).ToArray();
    }
}
