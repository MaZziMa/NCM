using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCM.Data;
using Renci.SshNet;

namespace NCM.Services
{
    /// <summary>
    /// Background service định kỳ thực hiện:
    ///  - Backup cấu hình từ các device qua SSH
    ///  - Kiểm tra compliance
    /// </summary>
    public class BackupComplianceService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BackupComplianceService> _logger;
        private readonly TimeSpan _interval;

        public BackupComplianceService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<BackupComplianceService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            // Đọc interval (giờ) từ config, mặc định 24h
            var hours = configuration.GetValue<double>("BackupCompliance:IntervalHours", 24);
            _interval = TimeSpan.FromHours(hours);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BackupComplianceService started, interval = {Interval}", _interval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // Lấy danh sách devices
                    var devices = db.Devices.ToList();

                    foreach (var device in devices)
                    {
                        // Ví dụ đơn giản: chạy backup/Compliance bằng script SSH
                        try
                        {
                            // test SSH
                            using var ssh = new SshClient(device.IPAddress, device.SshUsername,
                                /* decrypt password nếu bạn bảo vệ trước */
                                scope.ServiceProvider
                                     .GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>()
                                     .CreateProtector("SSHPasswords")
                                     .Unprotect(device.SshPassword)
                            )
                            { ConnectionInfo = { Timeout = TimeSpan.FromSeconds(5) } };

                            ssh.Connect();
                            var cmd = ssh.RunCommand("show running-config");
                            ssh.Disconnect();

                            // Lưu vào DeviceConfigs
                            var cfg = new Models.DeviceConfig
                            {
                                DeviceId = device.DeviceId,
                                ConfigContent = cmd.Result,
                                UploadTime = DateTime.UtcNow
                            };
                            db.DeviceConfigs.Add(cfg);
                            device.LastBackupTime = cfg.UploadTime;
                            db.Update(device);

                            // Kiểm tra compliance (ví dụ gọi controller logic hoặc lặp rule)
                            // ... tương tự action CheckCompliance

                            await db.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Backup & compliance for Device {DeviceId} done.", device.DeviceId);
                        }
                        catch (Exception exDev)
                        {
                            _logger.LogWarning(exDev, "Failed to backup/scan device {DeviceId}", device.DeviceId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in BackupComplianceService loop");
                }

                // Chờ đến lần chạy tiếp theo
                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("BackupComplianceService stopping.");
        }
    }
}
