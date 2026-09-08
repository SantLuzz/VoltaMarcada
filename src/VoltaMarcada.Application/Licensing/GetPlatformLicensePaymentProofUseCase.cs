using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Storage;

namespace VoltaMarcada.Application.Licensing;

public sealed class GetPlatformLicensePaymentProofUseCase(
    ILicenseRepository repository,
    IFileStorage fileStorage)
{
    public async Task<FileStorageDownloadResult?> ExecuteAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await repository.GetPaymentForPlatformAsync(paymentId, cancellationToken);
        return string.IsNullOrWhiteSpace(payment?.ProofFileKey)
            ? null
            : await fileStorage.GetAsync(payment.ProofFileKey, cancellationToken);
    }
}
