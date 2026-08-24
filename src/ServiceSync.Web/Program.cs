using MudBlazor.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using ServiceSync.Application;
using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Tenancy;
using ServiceSync.Infrastructure;
using ServiceSync.Infrastructure.Identity;
using ServiceSync.Web.Components;
using ServiceSync.Web.Authorization;
using ServiceSync.Web.Endpoints;
using ServiceSync.Web.Errors;
using ServiceSync.Web.Identity;
using ServiceSync.Web.Configuration;
using ServiceSync.Application.Scheduling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddExceptionHandler<UnexpectedExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddOptions<SchedulingOptions>()
    .Bind(builder.Configuration.GetSection(SchedulingOptions.SectionName))
    .Validate(options => options.SlotIntervalMinutes > 0 && 60 % options.SlotIntervalMinutes == 0, "Slot interval must divide one hour.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.DefaultTimeZoneId), "Default time zone is required.")
    .Validate(options => options.AllowedTimeZoneIds.Count > 0 && options.AllowedTimeZoneIds.Contains(options.DefaultTimeZoneId, StringComparer.Ordinal), "Allowed time zones must contain the default time zone.")
    .ValidateOnStart();
builder.Services.AddSingleton<ISchedulingConfiguration, SchedulingConfiguration>();
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

app.UseExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto
});

app.UseStatusCodePages(statusCodeContext =>
{
    var response = statusCodeContext.HttpContext.Response;
    var target = response.StatusCode switch
    {
        StatusCodes.Status400BadRequest => "/error",
        StatusCodes.Status404NotFound => "/not-found",
        _ => null
    };

    if (target is not null)
    {
        response.Redirect(target);
    }

    return Task.CompletedTask;
});

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
app.MapProfessionalEndpoints();
app.MapEmployeeInvitationEndpoints();
app.MapWorkOrderEndpoints();
app.MapMaintenanceOpportunityEndpoints();
app.MapAppointmentEndpoints();
app.MapScheduleBlockRequestEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
