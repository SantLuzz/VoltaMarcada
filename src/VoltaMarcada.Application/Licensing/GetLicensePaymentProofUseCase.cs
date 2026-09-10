using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Licensing;

public sealed class GetLicensePaymentProofUseCase(
    ILicenseRepository repository,
    ICurrentCompany currentCompany,
    IFileStorage fileStorage)
{
    public async Task<FileStorageDownloadResult?> ExecuteAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var payment = await repository.GetPaymentAsync(paymentId, companyId, cancellationToken);
        if (string.IsNullOrWhiteSpace(payment?.ProofFileKey))
        {
            return null;
        }

        return await fileStorage.GetAsync(payment.ProofFileKey, cancellationToken);
    }
}
