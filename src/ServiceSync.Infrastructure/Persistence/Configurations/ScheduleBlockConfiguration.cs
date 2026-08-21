using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Configurations;

public sealed class ScheduleBlockConfiguration : IEntityTypeConfiguration<ScheduleBlock>
{
    public void Configure(EntityTypeBuilder<ScheduleBlock> builder)
    {
        builder.ToTable("ScheduleBlocks");
        builder.HasKey(block => block.Id);
        builder.Property(block => block.Id).ValueGeneratedNever();
        builder.Property(block => block.CompanyId).IsRequired();
        builder.Property(block => block.ProfessionalId).IsRequired();
        builder.Property(block => block.Type).IsRequired();
        builder.Property(block => block.StartDate).HasColumnType("date").IsRequired();
        builder.Property(block => block.EndDate).HasColumnType("date").IsRequired();
        builder.Property(block => block.IsFullDay).IsRequired();
        builder.Property(block => block.StartTime).HasColumnType("time without time zone");
        builder.Property(block => block.EndTime).HasColumnType("time without time zone");
        builder.Property(block => block.Notes).HasMaxLength(ScheduleBlock.MaxNotesLength);
        builder.Property(block => block.CreatedAtUtc).IsRequired();

        builder.HasAlternateKey(block => new { block.CompanyId, block.Id })
            .HasName("AK_ScheduleBlocks_CompanyId_Id");

        builder.HasIndex(block => new { block.CompanyId, block.ProfessionalId, block.StartDate, block.EndDate })
            .HasDatabaseName("IX_ScheduleBlocks_CompanyId_ProfessionalId_StartDate_EndDate");

        builder.HasOne<Professional>()
            .WithMany()
            .HasForeignKey(block => new { block.CompanyId, block.ProfessionalId })
            .HasPrincipalKey(professional => new { professional.CompanyId, professional.Id })
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
