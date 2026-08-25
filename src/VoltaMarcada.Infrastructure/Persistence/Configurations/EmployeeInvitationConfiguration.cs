using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Infrastructure.Identity;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class EmployeeInvitationConfiguration : IEntityTypeConfiguration<EmployeeInvitation>
{
    public void Configure(EntityTypeBuilder<EmployeeInvitation> builder)
    {
        builder.ToTable("EmployeeInvitations");
        builder.HasKey(invitation => invitation.Id);
        builder.Property(invitation => invitation.Id).ValueGeneratedNever();
        builder.Property(invitation => invitation.FullName).HasMaxLength(200).IsRequired();
        builder.Property(invitation => invitation.Email).HasMaxLength(256).IsRequired();
        builder.Property(invitation => invitation.Document).HasMaxLength(11).IsRequired();
        builder.Property(invitation => invitation.Phone).HasMaxLength(20).IsRequired();
        builder.Property(invitation => invitation.Address).HasMaxLength(300).IsRequired();
        builder.Property(invitation => invitation.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(invitation => invitation.ExpiresAtUtc).IsRequired();
        builder.Property(invitation => invitation.CreatedAtUtc).IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(invitation => invitation.CompanyId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(invitation => invitation.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(invitation => invitation.TokenHash)
            .HasDatabaseName("UX_EmployeeInvitations_TokenHash")
            .IsUnique();

        builder.HasIndex(invitation => new { invitation.CompanyId, invitation.CreatedAtUtc })
            .HasDatabaseName("IX_EmployeeInvitations_CompanyId_CreatedAtUtc");

        builder.HasIndex(invitation => new
            {
                invitation.CompanyId,
                invitation.Email,
                invitation.AcceptedAtUtc,
                invitation.RevokedAtUtc
            })
            .HasDatabaseName("IX_EmployeeInvitations_CompanyId_Email_State");

        builder.HasIndex(invitation => new
            {
                invitation.CompanyId,
                invitation.Document,
                invitation.AcceptedAtUtc,
                invitation.RevokedAtUtc
            })
            .HasDatabaseName("IX_EmployeeInvitations_CompanyId_Document_State");
    }
}
