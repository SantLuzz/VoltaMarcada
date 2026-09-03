using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class WorkOrderHistoryEntryConfiguration : IEntityTypeConfiguration<WorkOrderHistoryEntry>
{
    public void Configure(EntityTypeBuilder<WorkOrderHistoryEntry> builder)
    {
        builder.ToTable("WorkOrderHistoryEntries"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => new { x.CompanyId, x.WorkOrderId, x.CreatedAtUtc }).HasDatabaseName("IX_WorkOrderHistoryEntries_CompanyId_WorkOrderId_CreatedAtUtc");
    }
}
