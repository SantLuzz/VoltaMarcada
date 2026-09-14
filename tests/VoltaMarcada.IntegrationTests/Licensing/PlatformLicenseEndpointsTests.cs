using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoltaMarcada.Application.Licensing;
using VoltaMarcada.Web.Authorization;
using VoltaMarcada.Web.Endpoints;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Licensing;

public sealed class PlatformLicenseEndpointsTests
{
    [Fact]
    public async Task List_forbids_an_authenticated_user_without_platform_administrator_role()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();
        builder.Services.AddScoped<GetPlatformLicensePaymentsUseCase>(_ => null!);
        builder.Services.AddScoped<GetPlatformLicensePaymentProofUseCase>(_ => null!);
        builder.Services.AddScoped<ReleaseCompanyLicenseManuallyUseCase>(_ => null!);
        builder.Services.AddSingleton<VoltaMarcada.Infrastructure.Licensing.OverdueLicenseNotificationService>(_ => null!);
        builder.Services.AddScoped<ReviewLicensePaymentUseCase>(_ => null!);
        builder.Services.AddAuthentication(TestAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, _ => { });
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(AuthorizationPolicies.PlatformAdministrator, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole("PlatformAdministrator");
            });

        var app = builder.Build();
        app.UseRouting();
        app.Use(async (context, next) =>
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
                "Test"));
            await next(context);
        });
        app.UseAuthorization();
        app.MapPlatformLicenseEndpoints();
        await app.StartAsync();
        try
        {
            var response = await app.GetTestClient().GetAsync("/platform/licenses/payments/");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            await app.DisposeAsync();
        }
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "Test";
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());
    }
}
