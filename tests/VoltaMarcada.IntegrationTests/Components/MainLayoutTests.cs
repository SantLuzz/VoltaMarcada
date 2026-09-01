using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor.Services;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Identity;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Web.Components.Layout;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class MainLayoutTests
{
    [Fact]
    public async Task RenderAsync_ShouldExposeAppointmentRequestsOnlyThroughScheduleAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var company = Company.Create(
            "VoltaMarcada Test Company Ltd.",
            "VoltaMarcada Test Company",
            "04.252.011/0001-10",
            "11999999999",
            "01001-000",
            "Praca da Se, 1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "volta-marcada-test-company",
            new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudServices();
        services.AddSingleton<IJSRuntime, NoopJsRuntime>();
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        services.AddDbContextFactory<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddSingleton<ICurrentCompany>(new UserWithCompany(company.Id));

        await using var serviceProvider = services.BuildServiceProvider();
        await using (var dbContext = await serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContextAsync())
        {
            dbContext.Companies.Add(company);
            dbContext.Users.Add(new ApplicationUser { Id = userId, UserName = "owner@voltamarcada.test", Email = "owner@voltamarcada.test" });
            dbContext.CompanyUsers.Add(CompanyUser.Create(userId, company.Id, CompanyUserRole.Owner, company.CreatedAtUtc));
            await dbContext.SaveChangesAsync();
        }

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        await using var renderer = new HtmlRenderer(serviceProvider, loggerFactory);
        var authenticationState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            authenticationType: "Test")));

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = renderer.BeginRenderingComponent<MainLayoutHost>(
                ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(MainLayoutHost.AuthenticationStateTask)] = Task.FromResult(authenticationState)
                }));
            await component.QuiescenceTask;
            return component.ToHtmlString();
        });

        Assert.Contains("/agenda", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/solicitacoes", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenderAsync_ShouldNotThrow_WhenAuthenticatedUserDoesNotBelongToACompany()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudServices();
        services.AddSingleton<IJSRuntime, NoopJsRuntime>();
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped<ICurrentCompany, UserWithoutCompany>();

        await using var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        await using var renderer = new HtmlRenderer(serviceProvider, loggerFactory);
        var authenticationState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            authenticationType: "Test")));

        var exception = await Record.ExceptionAsync(() => renderer.Dispatcher.InvokeAsync(() =>
            renderer.BeginRenderingComponent<MainLayoutHost>(
                ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(MainLayoutHost.AuthenticationStateTask)] = Task.FromResult(authenticationState)
                }))));

        Assert.Null(exception);
    }

    private sealed class UserWithoutCompany : ICurrentCompany
    {
        public Task<Guid?> FindCompanyIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(null);

        public Task<Guid> GetCompanyIdAsync(CancellationToken cancellationToken = default) =>
            throw new CurrentCompanyNotFoundException();
    }

    private sealed class UserWithCompany(Guid companyId) : ICurrentCompany
    {
        public Task<Guid?> FindCompanyIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(companyId);

        public Task<Guid> GetCompanyIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(companyId);
    }

    private sealed class NoopJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            ValueTask.FromResult(default(TValue)!);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
        }
    }

    private sealed class MainLayoutHost : ComponentBase
    {
        [Parameter]
        public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<Task<AuthenticationState>>>(0);
            builder.AddAttribute(1, "Value", AuthenticationStateTask);
            builder.AddAttribute(2, "IsFixed", true);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(childBuilder =>
            {
                childBuilder.OpenComponent<MainLayout>(4);
                childBuilder.AddAttribute(5, "Body", (RenderFragment)(bodyBuilder => bodyBuilder.AddContent(6, "Content")));
                childBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
