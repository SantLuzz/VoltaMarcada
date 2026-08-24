using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServiceSync.Domain.Entities;
using ServiceSync.Infrastructure.Identity;

namespace ServiceSync.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<CompanyUser> CompanyUsers => Set<CompanyUser>();

    public DbSet<EmployeeInvitation> EmployeeInvitations => Set<EmployeeInvitation>();

    public DbSet<State> States => Set<State>();

    public DbSet<City> Cities => Set<City>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();

    public DbSet<Equipment> Equipment => Set<Equipment>();

    public DbSet<Service> Services => Set<Service>();

    public DbSet<ServiceChecklistItem> ServiceChecklistItems => Set<ServiceChecklistItem>();

    public DbSet<Professional> Professionals => Set<Professional>();

    public DbSet<ProfessionalService> ProfessionalServices => Set<ProfessionalService>();

    public DbSet<ProfessionalAvailability> ProfessionalAvailabilities => Set<ProfessionalAvailability>();

    public DbSet<ProfessionalDailySchedule> ProfessionalDailySchedules => Set<ProfessionalDailySchedule>();

    public DbSet<ScheduleBlock> ScheduleBlocks => Set<ScheduleBlock>();

    public DbSet<ScheduleBlockRequest> ScheduleBlockRequests => Set<ScheduleBlockRequest>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<AppointmentRescheduleHistory> AppointmentRescheduleHistories => Set<AppointmentRescheduleHistory>();

    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();

    public DbSet<WorkOrderItem> WorkOrderItems => Set<WorkOrderItem>();

    public DbSet<WorkOrderChecklistItem> WorkOrderChecklistItems => Set<WorkOrderChecklistItem>();

    public DbSet<WorkOrderPhoto> WorkOrderPhotos => Set<WorkOrderPhoto>();

    public DbSet<MaintenanceOpportunity> MaintenanceOpportunities => Set<MaintenanceOpportunity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
