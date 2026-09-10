using VoltaMarcada.Application.Abstractions.Persistence;

namespace VoltaMarcada.Application.Licensing;

public sealed class GetPlatformLicensePaymentsUseCase(ILicenseRepository repository)
{
    public Task<IReadOnlyList<PlatformLicensePaymentDto>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        repository.GetPlatformPaymentsAsync(cancellationToken);
}
