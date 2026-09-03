using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VoltaMarcada.Infrastructure.Identity;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Web.Endpoints;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Endpoints;

public sealed class AccountLoginTests
{
    [Fact]
    public async Task Login_ShouldExplainThatTheAccountIsLocked_WhenTheUserExceededFailedAttempts()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();
        services.Configure<IdentityOptions>(options =>
        {
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 1;
        });

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var signInManager = scope.ServiceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = new ApplicationUser
        {
            UserName = "locked@voltamarcada.test",
            Email = "locked@voltamarcada.test",
            LockoutEnabled = true
        };

        Assert.True((await userManager.CreateAsync(user, "ValidPassword123!")).Succeeded);
        Assert.True((await userManager.AccessFailedAsync(user)).Succeeded);
        Assert.True(await userManager.IsLockedOutAsync(user));

        var result = await InvokeLoginAsync(
            new AccountEndpoints.LoginAccountRequest(user.Email, "ValidPassword123!", null),
            signInManager,
            dbContext);

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = scope.ServiceProvider;
        await result.ExecuteAsync(httpContext);
        var query = QueryHelpers.ParseQuery(new Uri("https://voltamarcada.test" + httpContext.Response.Headers.Location).Query);

        Assert.True(query.ContainsKey("error"), httpContext.Response.Headers.Location.ToString());
        Assert.Equal("Conta bloqueada por excesso de tentativas. Tente novamente mais tarde.", query["error"].ToString());
    }

    private static async Task<IResult> InvokeLoginAsync(
        AccountEndpoints.LoginAccountRequest request,
        SignInManager<ApplicationUser> signInManager,
        AppDbContext dbContext)
    {
        var loginMethod = typeof(AccountEndpoints).GetMethod("LoginAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
        var task = (Task<IResult>)loginMethod.Invoke(null, [request, signInManager, dbContext, CancellationToken.None])!;

        return await task;
    }
}