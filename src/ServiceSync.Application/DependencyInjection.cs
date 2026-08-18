using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ServiceSync.Application.CompanyOnboarding;

namespace ServiceSync.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();
        services.AddScoped<CompleteCompanyOnboardingUseCase>();
        return services;
    }
}
