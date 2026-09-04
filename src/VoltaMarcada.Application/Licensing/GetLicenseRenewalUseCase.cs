using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Payments;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Licensing;

public sealed class GetLicenseRenewalUseCase(
    ILicenseRepository repository,
    ICurrentCompany currentCompany,
    ILicenseBillingConfiguration billing,
    IPixPayloadGenerator pixPayloadGenerator)
{
    public async Task<OperationResult<LicenseRenewalDto>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var pendingPayment = await repository.GetPendingPaymentAsync(companyId, cancellationToken);
        var pix = pixPayloadGenerator.Generate(
            new PixPaymentRequest(
                billing.PixKey,
                billing.BeneficiaryName,
                billing.BeneficiaryCity,
                billing.MonthlyPrice));

        return OperationResult<LicenseRenewalDto>.Success(
            new LicenseRenewalDto(
                billing.MonthlyPrice,
                pix,
                pendingPayment is null ? null : SubmitLicensePaymentUseCase.Map(pendingPayment)));
    }
}
