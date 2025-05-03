using Microsoft.EntityFrameworkCore;
using NCM.Models;

namespace NCM.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceConfig> DeviceConfigs { get; set; }
        public DbSet<ComplianceRule> ComplianceRules { get; set; }
        public DbSet<ComplianceResult> ComplianceResults { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1-n Device → DeviceConfig
            builder.Entity<Device>()
                .HasMany(d => d.DeviceConfigs)
                .WithOne(c => c.Device)
                .HasForeignKey(c => c.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
