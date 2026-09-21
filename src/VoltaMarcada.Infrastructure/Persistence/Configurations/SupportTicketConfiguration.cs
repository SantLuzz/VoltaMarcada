using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.ToTable("SupportTickets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Number).IsRequired();
        builder.HasIndex(x => x.Number).IsUnique();
        builder.Property(x => x.RequesterName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RequesterEmail).HasMaxLength(320).IsRequired();
        builder.Property(x => x.CompanyTradeName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(200).IsRequired();
        builder.HasMany(x => x.Messages).WithOne().HasForeignKey(x => x.SupportTicketId).OnDelete(DeleteBehavior.Cascade);
    }
}
