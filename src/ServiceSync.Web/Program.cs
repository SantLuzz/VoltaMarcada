using MudBlazor.Services;
using Microsoft.AspNetCore.Authorization;
using ServiceSync.Application;
using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Tenancy;
using ServiceSync.Infrastructure;
using ServiceSync.Infrastructure.Identity;
using ServiceSync.Web.Components;
using ServiceSync.Web.Authorization;
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
builder.Services.AddScoped<IAuthorizationHandler, CompanyMemberAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, CompanyOwnerAuthorizationHandler>();
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
    });

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
app.MapCustomerEndpoints();
app.MapEquipmentEndpoints();
app.MapServiceEndpoints();
app.MapEmployeeInvitationEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
