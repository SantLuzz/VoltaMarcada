using Microsoft.Extensions.Options;
using VoltaMarcada.Application.CompanyOnboarding;

namespace VoltaMarcada.Web.Configuration;

public sealed class CompanyLicenseTrialOptions
{
    public const string SectionName = "parameters";

    public int FreeTrialDays { get; init; } = 14;
}

public sealed class CompanyLicenseTrialConfiguration(IOptions<CompanyLicenseTrialOptions> options)
    : ICompanyLicenseTrialConfiguration
{
    public int TrialDays => options.Value.FreeTrialDays;
}
