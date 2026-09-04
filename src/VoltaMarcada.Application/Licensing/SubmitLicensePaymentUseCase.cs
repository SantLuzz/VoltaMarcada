using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.Licensing;

public sealed class SubmitLicensePaymentUseCase(
    ILicenseRepository repository,
    ICurrentCompany currentCompany,
    IFileStorage fileStorage,
    ILicenseBillingConfiguration billing,
    TimeProvider timeProvider)
{
    private static readonly IReadOnlyDictionary<string, string> AllowedContentTypesByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

    public async Task<OperationResult<LicensePaymentSubmissionDto>> ExecuteAsync(
        LicensePaymentProofUpload upload,
        CancellationToken cancellationToken = default)
    {
        if (!TryValidate(upload, out var extension))
        {
            return OperationResult<LicensePaymentSubmissionDto>.Failure(LicenseErrors.InvalidPaymentProof);
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        if (await repository.HasPendingPaymentAsync(companyId, cancellationToken))
        {
            return OperationResult<LicensePaymentSubmissionDto>.Failure(LicenseErrors.PendingPaymentAlreadyExists);
        }

        var now = timeProvider.GetUtcNow();
        var payment = LicensePayment.Create(companyId, billing.MonthlyPrice, now);
        var fileName = $"{payment.Id}{extension}";
        var stored = await fileStorage.UploadAsync(
            new FileStorageUploadRequest(
                upload.File with { FileName = fileName },
                $"pagamentos/{companyId}",
                fileName,
                FileStorageAccess.Private),
            cancellationToken);

        payment.AttachProof(stored.Key, upload.File.ContentType, upload.File.Length, now);

        try
        {
            var outcome = await repository.AddPaymentAsync(payment, cancellationToken);
            if (outcome == LicensePaymentPersistenceOutcome.PendingPaymentAlreadyExists)
            {
                await fileStorage.DeleteAsync(stored.Key, CancellationToken.None);
                return OperationResult<LicensePaymentSubmissionDto>.Failure(LicenseErrors.PendingPaymentAlreadyExists);
            }
        }
        catch
        {
            await fileStorage.DeleteAsync(stored.Key, CancellationToken.None);
            throw;
        }

        return OperationResult<LicensePaymentSubmissionDto>.Success(Map(payment));
    }

    private bool TryValidate(LicensePaymentProofUpload? upload, out string extension)
    {
        extension = string.Empty;
        if (upload?.File is null ||
            upload.File.Content is null ||
            !upload.File.Content.CanRead ||
            upload.File.Length is <= 0 ||
            upload.File.Length > billing.MaximumProofSizeBytes ||
            string.IsNullOrWhiteSpace(upload.File.FileName) ||
            string.IsNullOrWhiteSpace(upload.File.ContentType))
        {
            return false;
        }

        extension = Path.GetExtension(upload.File.FileName).ToLowerInvariant();
        return AllowedContentTypesByExtension.TryGetValue(extension, out var expectedContentType) &&
            string.Equals(expectedContentType, upload.File.ContentType.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    internal static LicensePaymentSubmissionDto Map(LicensePayment payment) =>
        new(payment.Id, payment.Amount, payment.Status, payment.SubmittedAtUtc);
}
