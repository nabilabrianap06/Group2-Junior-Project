using AiEnvironmentalTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace AiEnvironmentalTracker.Data
{
    /// <summary>
    /// EF Core database context wired to PostgreSQL (Supabase).
    /// Handles persistence and indexes for AI proxy telemetry and chat logs.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<AiUsageLog> AiUsageLogs { get; set; } = null!;
        public DbSet<ChatLog> ChatLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AiUsageLog>(entity =>
            {
                entity.ToTable("ai_usage_logs");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                      .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(e => e.Provider)
                      .IsRequired()
                      .HasMaxLength(64);

                entity.Property(e => e.ModelName)
                      .IsRequired()
                      .HasMaxLength(128);

                entity.Property(e => e.AnalogyString)
                      .HasMaxLength(256);

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.Provider);
                entity.HasIndex(e => e.ModelName);
            });

            modelBuilder.Entity<ChatLog>(entity =>
            {
                entity.ToTable("chat_logs");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                      .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(e => e.UserPrompt)
                      .IsRequired()
                      .HasMaxLength(4000);

                entity.Property(e => e.AIResponse)
                      .IsRequired();

                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

                entity.HasIndex(e => e.CreatedAt);
            });
        }
    }
}
