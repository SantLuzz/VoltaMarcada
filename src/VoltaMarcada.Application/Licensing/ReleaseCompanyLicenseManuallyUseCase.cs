using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;

namespace VoltaMarcada.Application.Licensing;

public sealed record ManualLicenseReleaseCommand(Guid CompanyId, DateTimeOffset PaidUntilUtc);

public sealed class ReleaseCompanyLicenseManuallyUseCase(
    ILicenseRepository repository,
    TimeProvider timeProvider)
{
    public async Task<OperationResult> ExecuteAsync(
        ManualLicenseReleaseCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.CompanyId == Guid.Empty || command.PaidUntilUtc <= timeProvider.GetUtcNow())
        {
            return OperationResult.Failure(LicenseErrors.InvalidManualLicenseExpiration);
        }

        return await repository.GrantManualAccessAsync(command.CompanyId, command.PaidUntilUtc, timeProvider.GetUtcNow(), cancellationToken)
            ? OperationResult.Success()
            : OperationResult.Failure(LicenseErrors.CompanyLicenseNotFound);
    }
}