using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.Services;

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
