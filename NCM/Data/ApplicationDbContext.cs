using Microsoft.EntityFrameworkCore;
using NCM.Models;

namespace NCM.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceConfig> DeviceConfigs { get; set; }
        public DbSet<ComplianceRule> ComplianceRules { get; set; }
        public DbSet<ComplianceResult> ComplianceResults { get; set; }
        public DbSet<DeviceSnmpMetric> DeviceSnmpMetrics { get; set; }
        public DbSet<DeviceConfigDiff> DeviceConfigDiffs { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DeviceConfigDiff>(entity =>
            {
                entity.HasKey(d => d.DiffId);

                // Khóa ngoại OldConfigId: không cascade delete
                entity.HasOne(d => d.OldConfig)
                      .WithMany()
                      .HasForeignKey(d => d.OldConfigId)
                      .OnDelete(DeleteBehavior.Restrict);  // NO ACTION :contentReference[oaicite:4]{index=4}

                // Khóa ngoại NewConfigId: không cascade delete
                entity.HasOne(d => d.NewConfig)
                      .WithMany()
                      .HasForeignKey(d => d.NewConfigId)
                      .OnDelete(DeleteBehavior.Restrict);  // NO ACTION :contentReference[oaicite:5]{index=5}

                // Khóa ngoại DeviceId: có thể để cascade hoặc restrict tuỳ nhu cầu
                entity.HasOne(d => d.Device)
                      .WithMany(dv => dv.ConfigDiffs)
                      .HasForeignKey(d => d.DeviceId)
                      .OnDelete(DeleteBehavior.Cascade);   // Hoặc Restrict nếu muốn :contentReference[oaicite:6]{index=6}
            });
        }


    }
}
