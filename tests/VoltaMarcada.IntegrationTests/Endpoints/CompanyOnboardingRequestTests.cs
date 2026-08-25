using VoltaMarcada.Web.Endpoints;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Endpoints;

public sealed class CompanyOnboardingRequestTests
{
    [Fact]
    public void Constructor_ShouldMakeLogoOptional_WhenNoLogoIsSubmitted()
    {
        var constructor = typeof(CompanyOnboardingEndpoints.CompleteCompanyOnboardingRequest)
            .GetConstructors()
            .Single();

        var logoParameter = constructor.GetParameters()
            .Single(parameter => parameter.Name == "Logo");

        Assert.True(logoParameter.HasDefaultValue);
        Assert.Null(logoParameter.DefaultValue);
    }
}
