using CloudinaryDotNet;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Infrastructure.Identity;
using ServiceSync.Infrastructure.Persistence;
using ServiceSync.Infrastructure.Persistence.Repositories;
using ServiceSync.Infrastructure.Storage;
using ServiceSync.Application.Abstractions.Storage;
using ServiceSync.Application.Abstractions.Communication;
using ServiceSync.Infrastructure.Communication;

namespace ServiceSync.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddDataProtection()
            .SetApplicationName("ServiceSync")
            .PersistKeysToDbContext<AppDbContext>();

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ICompanyOnboardingRepository, CompanyOnboardingRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IEquipmentRepository, EquipmentRepository>();
        services.AddScoped<IWorkOrderRepository, WorkOrderRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IProfessionalRepository, ProfessionalRepository>();
        services.AddScoped<IProfessionalAvailabilityRepository, ProfessionalAvailabilityRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IMaintenanceOpportunityRepository, MaintenanceOpportunityRepository>();
        services.AddScoped<ICustomerContactRepository, CustomerContactRepository>();
        services.AddScoped<IWhatsAppLinkBuilder, WhatsAppLinkBuilder>();
        services.AddScoped<IEmployeeInvitationRepository, EmployeeInvitationRepository>();
        services.AddScoped<IEmployeeInvitationAccountService, EmployeeInvitationAccountService>();
        services.AddOptions<CloudinaryOptions>()
            .Bind(configuration.GetSection(CloudinaryOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.CloudName) &&
                    !string.IsNullOrWhiteSpace(options.ApiKey) &&
                    !string.IsNullOrWhiteSpace(options.ApiSecret),
                "Cloudinary configuration is required.");
        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<CloudinaryOptions>>().Value;
            return new Cloudinary(new Account(options.CloudName, options.ApiKey, options.ApiSecret));
        });
        services.AddHttpClient();
        services.AddScoped<IFileStorage, CloudinaryFileStorage>();
        return services;
    }

    public static async Task ApplyDatabaseMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
