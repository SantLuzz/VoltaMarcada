using FluentValidation;
using ServiceSync.Application.Customers;
using Microsoft.Extensions.DependencyInjection;
using ServiceSync.Application.CompanyOnboarding;
using ServiceSync.Application.EmployeeInvitations;
using ServiceSync.Application.Equipment;
using ServiceSync.Application.Services;

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
        services.AddScoped<CreateEquipmentUseCase>();
        services.AddScoped<GetEquipmentUseCase>();
        services.AddScoped<GetCustomerEquipmentUseCase>();
        services.AddScoped<GetEquipmentForEditUseCase>();
        services.AddScoped<UpdateEquipmentUseCase>();
        services.AddScoped<DeleteEquipmentUseCase>();
        services.AddScoped<CreateServiceUseCase>();
        services.AddScoped<GetServicesUseCase>();
        services.AddScoped<GetServiceForEditUseCase>();
        services.AddScoped<UpdateServiceUseCase>();
        services.AddScoped<DeleteServiceUseCase>();
        services.AddScoped<CreateEmployeeInvitationUseCase>();
        services.AddScoped<GetOwnerCompanyOverviewUseCase>();
        services.AddScoped<RenewEmployeeInvitationUseCase>();
        services.AddScoped<RevokeEmployeeInvitationUseCase>();
        services.AddScoped<GetEmployeeInvitationForAcceptanceUseCase>();
        services.AddScoped<AcceptEmployeeInvitationUseCase>();
        services.AddScoped<UpdateCompanyMemberProfileUseCase>();
        return services;
    }
}
