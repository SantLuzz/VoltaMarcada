using Microsoft.Extensions.Options;
using VoltaMarcada.Application.Licensing;

namespace VoltaMarcada.Web.Configuration;

public sealed class LicenseBillingOptions
{
    public const string SectionName = "LicenseBilling";

    public decimal MonthlyPrice { get; init; }

    public string PixKey { get; init; } = string.Empty;

    public string BeneficiaryName { get; init; } = string.Empty;

    public string BeneficiaryCity { get; init; } = string.Empty;

    public long MaximumProofSizeBytes { get; init; }
}

public sealed class LicenseBillingConfiguration(IOptions<LicenseBillingOptions> options)
    : ILicenseBillingConfiguration
{
    private LicenseBillingOptions Value => options.Value;

    public decimal MonthlyPrice => Value.MonthlyPrice;

    public string PixKey => Value.PixKey;

    public string BeneficiaryName => Value.BeneficiaryName;

    public string BeneficiaryCity => Value.BeneficiaryCity;

    public long MaximumProofSizeBytes => Value.MaximumProofSizeBytes;
}
