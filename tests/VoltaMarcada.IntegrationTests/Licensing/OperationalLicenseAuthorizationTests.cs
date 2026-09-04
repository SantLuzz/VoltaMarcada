using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoltaMarcada.Application;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.Licensing;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Identity;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using VoltaMarcada.Web.Authorization;
using VoltaMarcada.Web.Endpoints;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Licensing;

public sealed class OperationalLicenseAuthorizationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(CompanyUserRole.Owner, CompanyLicenseAccessMode.OwnerReadOnly)]
    [InlineData(CompanyUserRole.Employee, CompanyLicenseAccessMode.OwnerReadOnly)]
    [InlineData(CompanyUserRole.Owner, CompanyLicenseAccessMode.Terminated)]
    [InlineData(CompanyUserRole.Employee, CompanyLicenseAccessMode.Terminated)]
    public async Task Work_order_mutation_requires_operational_license(
        CompanyUserRole role,
        CompanyLicenseAccessMode mode)
    {
        await using var fixture = await TestApplication.StartAsync(role, mode);

        var response = await fixture.Client.PostAsJsonAsync("/work-orders/", (object?)null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Operational_company_member_reaches_protected_endpoint()
    {
        await using var fixture = await TestApplication.StartAsync(
            CompanyUserRole.Owner,
            CompanyLicenseAccessMode.Operational);

        var response = await fixture.Client.PostAsync("/operational-license-test", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Platform_administrator_without_company_membership_cannot_mutate_tenant_data()
    {
        await using var fixture = await TestApplication.StartPlatformAdministratorAsync();

        var response = await fixture.Client.PostAsJsonAsync("/work-orders/", (object?)null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Restricted_owner_can_still_read_existing_tenant_data()
    {
        await using var fixture = await TestApplication.StartAsync(
            CompanyUserRole.Owner,
            CompanyLicenseAccessMode.OwnerReadOnly);

        var response = await fixture.Client.GetAsync("/company-member-read-test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(CompanyLicenseAccessMode.OwnerReadOnly, CompanyUserRole.Owner, "/licenca/renovar")]
    [InlineData(CompanyLicenseAccessMode.OwnerReadOnly, CompanyUserRole.Employee, "/conta-bloqueada")]
    [InlineData(CompanyLicenseAccessMode.Terminated, CompanyUserRole.Owner, "/licenca/renovar")]
    [InlineData(CompanyLicenseAccessMode.Terminated, CompanyUserRole.Employee, "/conta-bloqueada")]
    public async Task Restricted_browser_navigation_uses_role_specific_destination(
        CompanyLicenseAccessMode mode,
        CompanyUserRole role,
        string expectedLocation)
    {
        await using var fixture = await TestApplication.StartAsync(role, mode);

        var response = await fixture.Client.GetAsync("/operational-license-test");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expectedLocation, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Every_tenant_mutation_endpoint_requires_operational_license()
    {
        await using var fixture = await TestApplication.StartAsync(
            CompanyUserRole.Owner,
            CompanyLicenseAccessMode.Operational,
            mapAllTenantEndpoints: true);

        var unprotectedRoutes = ((IEndpointRouteBuilder)fixture.App).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(IsTenantMutation)
            .Where(endpoint => endpoint.Metadata
                .GetOrderedMetadata<IAuthorizeData>()
                .All(data => data.Policy != AuthorizationPolicies.OperationalCompanyLicense))
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .OrderBy(route => route, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(unprotectedRoutes);
    }

    [Theory]
    [InlineData(-10, 4, CompanyLicenseAction.None)]
    [InlineData(-11, 3, CompanyLicenseAction.Join)]
    [InlineData(-18, -4, CompanyLicenseAction.Renew)]
    public async Task Trial_license_projects_remaining_days_and_owner_action(
        int startedDaysFromNow,
        int expectedDaysRemaining,
        CompanyLicenseAction expectedAction)
    {
        var companyId = Guid.NewGuid();
        var license = CompanyLicense.Create(companyId, Now.AddDays(startedDaysFromNow), 14);
        var useCase = CreateLicenseUseCase(companyId, license);

        var result = await useCase.ExecuteAsync();

        Assert.NotNull(result);
        Assert.Equal(CompanyLicenseStatus.Trial, result.Status);
        Assert.Equal(expectedDaysRemaining, result.DaysRemaining);
        Assert.Equal(expectedAction, result.Action);
        Assert.Equal(license.GetAccessMode(Now, LicenseLifecyclePolicy.Default), result.AccessMode);
    }

    [Theory]
    [InlineData(8, CompanyLicenseAction.None)]
    [InlineData(7, CompanyLicenseAction.Renew)]
    [InlineData(-8, CompanyLicenseAction.Renew)]
    public async Task Paid_license_projects_renewal_action_at_seven_days_or_less(
        int paidUntilDaysFromNow,
        CompanyLicenseAction expectedAction)
    {
        var companyId = Guid.NewGuid();
        var license = CompanyLicense.Create(companyId, Now.AddDays(-30), 14);
        license.Renew(Now.AddMonths(-1).AddDays(paidUntilDaysFromNow));
        var useCase = CreateLicenseUseCase(companyId, license);

        var result = await useCase.ExecuteAsync();

        Assert.NotNull(result);
        Assert.Equal(CompanyLicenseStatus.Paid, result.Status);
        Assert.Equal(paidUntilDaysFromNow, result.DaysRemaining);
        Assert.Equal(expectedAction, result.Action);
        Assert.Equal(paidUntilDaysFromNow < 0, result.IsOverdue);
    }

    [Fact]
    public async Task Current_company_license_returns_null_when_license_is_missing()
    {
        var companyId = Guid.NewGuid();
        var useCase = CreateLicenseUseCase(companyId, null);

        var result = await useCase.ExecuteAsync();

        Assert.Null(result);
    }

    private static GetCurrentCompanyLicenseUseCase CreateLicenseUseCase(
        Guid companyId,
        CompanyLicense? license) =>
        new(new LicenseRepositoryStub(license), new CurrentCompanyStub(companyId), new FixedTimeProvider(Now));
    private static bool IsTenantMutation(RouteEndpoint endpoint)
    {
        var route = endpoint.RoutePattern.RawText ?? string.Empty;
        var isTenantRoute = route.StartsWith("/work-orders", StringComparison.Ordinal)
            || route.StartsWith("/appointments", StringComparison.Ordinal)
            || route.StartsWith("/customers", StringComparison.Ordinal)
            || route.StartsWith("/equipment", StringComparison.Ordinal)
            || route.StartsWith("/services", StringComparison.Ordinal)
            || route.StartsWith("/professionals", StringComparison.Ordinal)
            || route.StartsWith("/maintenance-opportunities", StringComparison.Ordinal)
            || route.StartsWith("/schedule-block-requests", StringComparison.Ordinal);
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods ?? [];
        return isTenantRoute && methods.Any(method => method is "POST" or "PUT" or "PATCH" or "DELETE");
    }

    private sealed class TestApplication(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        public WebApplication App { get; } = app;

        public HttpClient Client { get; } = client;

        public static Task<TestApplication> StartAsync(
            CompanyUserRole role,
            CompanyLicenseAccessMode mode,
            bool mapAllTenantEndpoints = false) =>
            StartCoreAsync(role, mode, false, mapAllTenantEndpoints);

        public static Task<TestApplication> StartPlatformAdministratorAsync() =>
            StartCoreAsync(null, null, true, false);

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.DisposeAsync();
        }

        private static async Task<TestApplication> StartCoreAsync(
            CompanyUserRole? role,
            CompanyLicenseAccessMode? mode,
            bool isPlatformAdministrator,
            bool mapAllTenantEndpoints)
        {
            var userId = Guid.NewGuid();
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddRouting();
            builder.Services.AddAuthentication(TestAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, _ => { });
            builder.Services.AddApplication();
            builder.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
            builder.Services.AddDbContextFactory<AppDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            builder.Services.AddScoped<ILicenseRepository, LicenseRepository>();
            builder.Services.AddScoped<IAuthorizationHandler, CompanyMemberAuthorizationHandler>();
            builder.Services.AddScoped<IAuthorizationHandler, CompanyOwnerAuthorizationHandler>();
            builder.Services.AddScoped<IAuthorizationHandler, OperationalLicenseAuthorizationHandler>();
            builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, CompanyMemberAuthorizationResultHandler>();
            builder.Services.AddAuthorizationBuilder()
                .AddPolicy(AuthorizationPolicies.CompanyMember, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new CompanyMemberRequirement());
                })
                .AddPolicy(AuthorizationPolicies.CompanyOwner, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new CompanyOwnerRequirement());
                })
                .AddPolicy(AuthorizationPolicies.OperationalCompanyLicense, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new OperationalLicenseRequirement());
                });

            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
            if (isPlatformAdministrator)
            {
                claims.Add(new Claim(ClaimTypes.Role, PlatformRoles.PlatformAdministrator));
            }
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

            var app = builder.Build();
            app.UseRouting();
            app.Use(async (context, next) =>
            {
                context.User = principal;
                await next(context);
            });
            app.UseAuthorization();
            MapEndpoints(app, mapAllTenantEndpoints);

            await using (var scope = app.Services.CreateAsyncScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (role is { } companyRole && mode is { } accessMode)
                {
                    var companyId = Guid.NewGuid();
                    dbContext.CompanyUsers.Add(CompanyUser.Create(userId, companyId, companyRole, Now));
                    dbContext.CompanyLicenses.Add(CreateLicense(companyId, accessMode));
                    await dbContext.SaveChangesAsync();
                }
            }

            await app.StartAsync();
            var client = app.GetTestClient();
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            return new TestApplication(app, client);
        }

        private static void MapEndpoints(WebApplication app, bool mapAllTenantEndpoints)
        {
            app.MapWorkOrderEndpoints();
            app.MapMethods("/operational-license-test", ["GET", "POST"], () => Results.Ok())
                .RequireOperationalCompanyLicense();
            app.MapGet("/company-member-read-test", () => Results.Ok())
                .RequireAuthorization(AuthorizationPolicies.CompanyMember);
            if (!mapAllTenantEndpoints)
            {
                return;
            }

            app.MapAppointmentEndpoints();
            app.MapCustomerEndpoints();
            app.MapEquipmentEndpoints();
            app.MapServiceEndpoints();
            app.MapProfessionalEndpoints();
            app.MapMaintenanceOpportunityEndpoints();
            app.MapScheduleBlockRequestEndpoints();
        }

        private static CompanyLicense CreateLicense(Guid companyId, CompanyLicenseAccessMode mode) => mode switch
        {
            CompanyLicenseAccessMode.Operational => CompanyLicense.Create(companyId, Now, 14),
            CompanyLicenseAccessMode.OwnerReadOnly => CompanyLicense.Create(companyId, Now.AddDays(-18), 14),
            CompanyLicenseAccessMode.Terminated => CompanyLicense.Create(companyId, Now.AddDays(-24), 14),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    private sealed class CurrentCompanyStub(Guid companyId) : ICurrentCompany
    {
        public Task<Guid?> FindCompanyIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(companyId);

        public Task<Guid> GetCompanyIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(companyId);
    }

    private sealed class LicenseRepositoryStub(CompanyLicense? license) : ILicenseRepository
    {
        public Task<CompanyLicenseMembership?> GetMembershipAndLicenseAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<CompanyLicense?> GetForCompanyAsync(
            Guid companyId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(license?.CompanyId == companyId ? license : null);
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
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
