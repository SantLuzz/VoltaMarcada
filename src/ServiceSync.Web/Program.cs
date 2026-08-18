using MudBlazor.Services;
using ServiceSync.Application;
using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Tenancy;
using ServiceSync.Infrastructure;
using ServiceSync.Infrastructure.Identity;
using ServiceSync.Web.Components;
using ServiceSync.Web.Endpoints;
using ServiceSync.Web.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<ICurrentCompany, CurrentCompany>();

var app = builder.Build();

await app.Services.ApplyDatabaseMigrationsAsync();
await PlatformAdministratorInitializer.InitializeAsync(app.Services, builder.Configuration);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapAccountEndpoints();
app.MapCompanyOnboardingEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
