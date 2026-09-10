using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.DependencyInjection;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Identity;
using VoltaMarcada.Web.Routing;

namespace VoltaMarcada.Web.Authorization;

public sealed class CompanyMemberAuthorizationResultHandler(IServiceScopeFactory scopeFactory)
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (!authorizeResult.Forbidden || context.User.Identity?.IsAuthenticated != true)
        {
            await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        var failedRequirements = authorizeResult.AuthorizationFailure?.FailedRequirements ?? [];
        if (failedRequirements.OfType<CompanyMemberRequirement>().Any())
        {
            if (context.User.IsInRole(PlatformRoles.PlatformAdministrator))
            {
                if (HttpMethods.IsGet(context.Request.Method))
                {
                    context.Response.Redirect("/plataforma");
                    return;
                }

                await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
                return;
            }
            var requestedPath = context.Request.PathBase.ToString()
                + context.Request.Path
                + context.Request.QueryString;
            var returnUrl = LocalReturnUrl.Normalize(requestedPath, "/");

            context.Response.Redirect(
                "/onboarding/company" + QueryString.Create("returnUrl", returnUrl));
            return;
        }

        if (HttpMethods.IsGet(context.Request.Method)
            && failedRequirements.OfType<OperationalLicenseRequirement>().Any())
        {
            var destination = await GetRestrictedDestinationAsync(context.User, context.RequestAborted);
            if (destination is not null)
            {
                context.Response.Redirect(destination);
                return;
            }
        }

        await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    private async Task<string?> GetRestrictedDestinationAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<ILicenseRepository>();
        var membership = await repository.GetMembershipAndLicenseAsync(userId, cancellationToken);
        return membership?.Role switch
        {
            CompanyUserRole.Owner => "/licenca/renovar",
            CompanyUserRole.Employee => "/conta-bloqueada",
            _ => null
        };
    }
}
