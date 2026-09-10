using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor.Services;
using VoltaMarcada.Application;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.Dashboard;
using VoltaMarcada.Application.EmployeeInvitations;
using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using VoltaMarcada.Web.Components.Pages;
using VoltaMarcada.Web.Formatting;
using VoltaMarcada.Web.Operations;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class DashboardLicenseStatusTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Owner_dashboard_renders_trial_days_and_join_action()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudServices();
        services.AddApplication();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
        services.AddSingleton<IJSRuntime, NoopJsRuntime>();
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        services.AddSingleton<ICurrentUser>(new CurrentUserStub(userId));
        services.AddSingleton<ICurrentCompany>(new CurrentCompanyStub(companyId));
        services.AddSingleton<ISchedulingConfiguration, SchedulingConfigurationStub>();
        services.AddSingleton<IBrazilianDisplayFormatter, BrazilianDisplayFormatter>();
        services.AddScoped<IComponentOperationExecutor, ComponentOperationExecutor>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IProfessionalRepository, ProfessionalRepository>();
        services.AddScoped<IEmployeeInvitationRepository, EmployeeInvitationRepository>();
        services.AddScoped<IOwnerDashboardRepository, OwnerDashboardRepository>();
        services.AddScoped<ILicenseRepository, LicenseRepository>();
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        await using var serviceProvider = services.BuildServiceProvider();
        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.CompanyUsers.Add(CompanyUser.Create(userId, companyId, CompanyUserRole.Owner, Now));
            dbContext.CompanyLicenses.Add(CompanyLicense.Create(companyId, Now.AddDays(-11), 14));
            await dbContext.SaveChangesAsync();
        }

        await using var renderer = new HtmlRenderer(
            serviceProvider,
            serviceProvider.GetRequiredService<ILoggerFactory>());
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = renderer.BeginRenderingComponent<Dashboard>();
            await component.QuiescenceTask;
            return System.Net.WebUtility.HtmlDecode(component.ToHtmlString());
        });

        Assert.Contains("Per\u00EDodo de teste", html, StringComparison.Ordinal);
        Assert.Contains("3 dias restantes", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/licenca/renovar\"", html, StringComparison.Ordinal);
        Assert.Contains("Aderir", html, StringComparison.Ordinal);
    }

    private sealed class CurrentUserStub(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public Guid UserId { get; } = userId;
    }

    private sealed class CurrentCompanyStub(Guid companyId) : ICurrentCompany
    {
        public Task<Guid?> FindCompanyIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(companyId);

        public Task<Guid> GetCompanyIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(companyId);
    }

    private sealed class SchedulingConfigurationStub : ISchedulingConfiguration
    {
        public int SlotIntervalMinutes => 30;

        public int AvailabilitySearchDays => 7;

        public int AvailabilityResultLimit => 10;

        public string DefaultTimeZoneId => "America/Sao_Paulo";

        public IReadOnlySet<string> AllowedTimeZoneIds { get; } =
            new HashSet<string>(StringComparer.Ordinal) { "America/Sao_Paulo" };
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
        }
    }
    private sealed class NoopJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args) =>
            ValueTask.FromResult(default(TValue)!);
    }
}
