using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceSync.Infrastructure.Identity;

public static class PlatformAdministratorInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var email = configuration["PlatformAdministrator:Email"]?.Trim();
        var password = configuration["PlatformAdministrator:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync(PlatformRoles.PlatformAdministrator))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(PlatformRoles.PlatformAdministrator));
            EnsureSuccess(roleResult, "create the platform administrator role");
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            return;
        }

        user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var userResult = await userManager.CreateAsync(user, password);
        EnsureSuccess(userResult, "create the platform administrator user");

        var assignmentResult = await userManager.AddToRoleAsync(user, PlatformRoles.PlatformAdministrator);
        EnsureSuccess(assignmentResult, "assign the platform administrator role");
    }

    private static void EnsureSuccess(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Unable to {operation}.");
        }
    }
}
