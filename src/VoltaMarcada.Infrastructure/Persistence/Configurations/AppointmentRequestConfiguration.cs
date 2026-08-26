using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class AppointmentRequestConfiguration : IEntityTypeConfiguration<AppointmentRequest>
{
    public void Configure(EntityTypeBuilder<AppointmentRequest> builder)
    {
        builder.ToTable("AppointmentRequests");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id).ValueGeneratedNever();
        builder.Property(request => request.Status).IsRequired();
        builder.Property(request => request.DurationMinutes).IsRequired();
        builder.Property(request => request.ProposedCustomerName).HasMaxLength(200);
        builder.Property(request => request.ProposedCustomerWhatsApp).HasMaxLength(20);
        builder.Property(request => request.ProposedCustomerEmail).HasMaxLength(320);
        builder.Property(request => request.ProposedAddressStreet).HasMaxLength(200);
        builder.Property(request => request.ProposedAddressNumber).HasMaxLength(50);
        builder.Property(request => request.ProposedAddressPostalCode).HasMaxLength(8);
        builder.Property(request => request.ProposedAddressComplement).HasMaxLength(200);
        builder.Property(request => request.RejectionReason).HasMaxLength(1000);
        builder.Property(request => request.ResponseTokenHash).HasMaxLength(128);
        builder.HasAlternateKey(request => new { request.CompanyId, request.Id }).HasName("AK_AppointmentRequests_CompanyId_Id");
        builder.HasIndex(request => new { request.CompanyId, request.Status, request.RequestedStartAtUtc }).HasDatabaseName("IX_AppointmentRequests_CompanyId_Status_RequestedStartAtUtc");
        builder.HasIndex(request => new { request.CompanyId, request.CustomerId }).HasDatabaseName("IX_AppointmentRequests_CompanyId_CustomerId");
        builder.HasIndex(request => request.ResponseTokenHash).HasDatabaseName("IX_AppointmentRequests_ResponseTokenHash").IsUnique().HasFilter("\"ResponseTokenHash\" IS NOT NULL");
        builder.HasOne<Company>().WithMany().HasForeignKey(request => request.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(request => new { request.CompanyId, request.CustomerId }).HasPrincipalKey(customer => new { customer.CompanyId, customer.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Service>().WithMany().HasForeignKey(request => new { request.CompanyId, request.ServiceId }).HasPrincipalKey(service => new { service.CompanyId, service.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Professional>().WithMany().HasForeignKey(request => new { request.CompanyId, request.ProfessionalId }).HasPrincipalKey(professional => new { professional.CompanyId, professional.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Professional>().WithMany().HasForeignKey(request => new { request.CompanyId, request.ProposedProfessionalId }).HasPrincipalKey(professional => new { professional.CompanyId, professional.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Equipment>().WithMany().HasForeignKey(request => new { request.CompanyId, request.EquipmentId }).HasPrincipalKey(equipment => new { equipment.CompanyId, equipment.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MaintenanceOpportunity>().WithMany().HasForeignKey(request => new { request.CompanyId, request.MaintenanceOpportunityId }).HasPrincipalKey(opportunity => new { opportunity.CompanyId, opportunity.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Appointment>().WithMany().HasForeignKey(request => new { request.CompanyId, request.AppointmentId }).HasPrincipalKey(appointment => new { appointment.CompanyId, appointment.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<State>().WithMany().HasForeignKey(request => request.ProposedAddressStateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<City>().WithMany().HasForeignKey(request => request.ProposedAddressCityId).OnDelete(DeleteBehavior.Restrict);
    }
}
