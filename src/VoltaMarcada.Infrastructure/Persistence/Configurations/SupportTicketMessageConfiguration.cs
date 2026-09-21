using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class SupportTicketMessageConfiguration : IEntityTypeConfiguration<SupportTicketMessage>
{
    public void Configure(EntityTypeBuilder<SupportTicketMessage> builder)
    {
        builder.ToTable("SupportTicketMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Body).HasMaxLength(5000).IsRequired();
        builder.HasIndex(x => new { x.SupportTicketId, x.CreatedAtUtc });
        builder.HasMany(x => x.Attachments).WithOne().HasForeignKey(x => x.SupportTicketMessageId).OnDelete(DeleteBehavior.Cascade);
    }
}
