using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Infrastructure.Identity;
using ServiceSync.Infrastructure.Persistence;
using ServiceSync.Infrastructure.Persistence.Repositories;
using ServiceSync.Infrastructure.Storage;
using ServiceSync.Application.Abstractions.Storage;

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

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<ICompanyOnboardingRepository, CompanyOnboardingRepository>();
        services.AddScoped<ICompanyLogoStorage, LocalCompanyLogoStorage>();
        return services;
    }

    public static async Task ApplyDatabaseMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
