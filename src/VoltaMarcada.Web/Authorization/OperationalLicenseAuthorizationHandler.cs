using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Identity;

namespace VoltaMarcada.Web.Authorization;

public sealed class OperationalLicenseAuthorizationHandler(
    ILicenseRepository licenseRepository,
    TimeProvider timeProvider)
    : AuthorizationHandler<OperationalLicenseRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationalLicenseRequirement requirement)
    {
        if (context.User.IsInRole(PlatformRoles.PlatformAdministrator))
        {
            return;
        }

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return;
        }

        var membership = await licenseRepository.GetMembershipAndLicenseAsync(userId);
        if (membership?.License.GetAccessMode(timeProvider.GetUtcNow(), LicenseLifecyclePolicy.Default)
            == CompanyLicenseAccessMode.Operational)
        {
            context.Succeed(requirement);
        }
    }
}
