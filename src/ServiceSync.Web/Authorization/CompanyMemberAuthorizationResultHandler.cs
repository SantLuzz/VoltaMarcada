using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using ServiceSync.Web.Routing;

namespace ServiceSync.Web.Authorization;

public sealed class CompanyMemberAuthorizationResultHandler
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden
            && context.User.Identity?.IsAuthenticated == true
            && policy.Requirements.OfType<CompanyMemberRequirement>().Any())
        {
            var requestedPath = context.Request.PathBase.ToString()
                + context.Request.Path
                + context.Request.QueryString;
            var returnUrl = LocalReturnUrl.Normalize(requestedPath, "/");

            context.Response.Redirect(
                "/onboarding/company" + QueryString.Create("returnUrl", returnUrl));
            return Task.CompletedTask;
        }

        return defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
