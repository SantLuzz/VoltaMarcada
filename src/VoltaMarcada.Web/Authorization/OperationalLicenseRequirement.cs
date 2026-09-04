using Microsoft.AspNetCore.Authorization;

namespace VoltaMarcada.Web.Authorization;

public sealed class OperationalLicenseRequirement : IAuthorizationRequirement;

public static class OperationalLicenseRouteHandlerBuilderExtensions
{
    public static RouteHandlerBuilder RequireOperationalCompanyLicense(this RouteHandlerBuilder builder) =>
        builder.RequireAuthorization(AuthorizationPolicies.OperationalCompanyLicense);
}
