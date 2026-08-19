using FluentValidation;
using ServiceSync.Application.Customers;
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
        services.AddScoped<CreateCustomerUseCase>();
        services.AddScoped<GetCustomersUseCase>();
        services.AddScoped<GetCustomerForEditUseCase>();
        services.AddScoped<UpdateCustomerUseCase>();
        services.AddScoped<DeleteCustomerUseCase>();
        return services;
    }
}
