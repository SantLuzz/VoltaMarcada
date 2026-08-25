using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class StateConfiguration : IEntityTypeConfiguration<State>
{
    public void Configure(EntityTypeBuilder<State> builder)
    {
        builder.ToTable("States");
        builder.HasKey(state => state.Id);
        builder.Property(state => state.Id).ValueGeneratedNever();
        builder.Property(state => state.Name).HasMaxLength(100).IsRequired();
        builder.Property(state => state.Abbreviation).HasMaxLength(2).IsRequired();
        builder.HasIndex(state => state.Abbreviation)
            .HasDatabaseName("UX_States_Abbreviation")
            .IsUnique();
    }
}
