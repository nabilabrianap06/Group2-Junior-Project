using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AIEnvironmentalTracker.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UsageRecord>(entity =>
        {
            entity.HasKey(record => record.Id);
            entity.Property(record => record.AiModelName).HasMaxLength(120).IsRequired();
            entity.Property(record => record.WaterPerQuery).HasPrecision(18, 6);
            entity.Property(record => record.Co2PerQuery).HasPrecision(18, 6);
            entity.Property(record => record.EnergyPerQuery).HasPrecision(18, 6);
            entity.Property(record => record.TotalWater).HasPrecision(18, 6);
            entity.Property(record => record.TotalCo2).HasPrecision(18, 6);
            entity.Property(record => record.TotalEnergy).HasPrecision(18, 6);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(record => record.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(record => new { record.ApplicationUserId, record.Timestamp });
        });
    }
}
