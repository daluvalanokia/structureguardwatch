// StructureWatch.Data/Configurations/BuildingAnalysisConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StructureWatch.Data.Entities;

namespace StructureWatch.Data.Configurations;

public class BuildingAnalysisConfiguration : IEntityTypeConfiguration<BuildingAnalysisEntity>
{
    public void Configure(EntityTypeBuilder<BuildingAnalysisEntity> builder)
    {
        builder.ToTable("BuildingAnalyses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OsmId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.BuildingName)
            .HasMaxLength(256);

        builder.Property(x => x.BuildingType)
            .HasMaxLength(64);

        builder.Property(x => x.Height)
            .HasMaxLength(32);

        builder.Property(x => x.Levels)
            .HasMaxLength(16);

        builder.Property(x => x.LoadCapacity)
            .IsRequired();

        builder.Property(x => x.StructuralIntegrity)
            .IsRequired();

        builder.Property(x => x.SeismicRisk)
            .IsRequired();

        builder.Property(x => x.WindLoad)
            .IsRequired();

        builder.Property(x => x.OccupancyClass)
            .IsRequired();

        builder.Property(x => x.Summary)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.RiskFactors)
            .HasConversion(
                v => string.Join(";", v),
                v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(x => x.OsmId);
        builder.HasIndex(x => x.AnalyzedDate);
    }
}
