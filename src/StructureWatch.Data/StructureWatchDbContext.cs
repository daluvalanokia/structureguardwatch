// StructureWatch.Data/StructureWatchDbContext.cs
using Microsoft.EntityFrameworkCore;
using StructureWatch.Data.Configurations;
using StructureWatch.Data.Entities;

namespace StructureWatch.Data;

public class StructureWatchDbContext : DbContext
{
    public StructureWatchDbContext(DbContextOptions<StructureWatchDbContext> options) : base(options) { }

    public DbSet<BuildingAnalysisEntity> BuildingAnalyses => Set<BuildingAnalysisEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BuildingAnalysisConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
