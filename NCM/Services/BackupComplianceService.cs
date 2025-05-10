using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DiffPlex;
using DiffPlex.DiffBuilder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCM.Data;
using NCM.Models;
using DiffPlex.DiffBuilder.Model;

namespace NCM.Services
{
    public class BackupComplianceService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BackupComplianceService> _logger;

        public BackupComplianceService(
            IServiceScopeFactory scopeFactory,
            ILogger<BackupComplianceService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var devices = await db.Devices.ToListAsync(stoppingToken);
                foreach (var device in devices)
                {
                    try
                    {
                        // 1. Lấy cấu hình mới qua SSH/Telnet
                        var cfgContent = await FetchConfigFromDeviceAsync(device);

                        // 2. Lưu cấu hình mới vào bảng DeviceConfigs
                        var cfg = new DeviceConfig
                        {
                            DeviceId = device.DeviceId,
                            ConfigContent = cfgContent,
                            UploadTime = DateTime.UtcNow
                        };
                        db.DeviceConfigs.Add(cfg);

                        // (Tuỳ chọn) Cập nhật LastBackupTime trên Device
                        device.LastBackupTime = DateTime.UtcNow;
                        db.Devices.Update(device);

                        await db.SaveChangesAsync(stoppingToken);

                        // --- TỰ ĐỘNG CHẠY DIFF ---
                        var previousCfg = await db.DeviceConfigs
                            .Where(c => c.DeviceId == device.DeviceId && c.ConfigId != cfg.ConfigId)
                            .OrderByDescending(c => c.UploadTime)
                            .FirstOrDefaultAsync(stoppingToken);

                        if (previousCfg != null)
                        {
                            // Tạo diff giữa two versions
                            var differ = new SideBySideDiffBuilder(new Differ());
                            var diffModel = differ.BuildDiffModel(previousCfg.ConfigContent, cfg.ConfigContent);

                            // Xuất thành văn bản có dấu +,-, 
                            var sb = new StringBuilder();
                            foreach (var line in diffModel.NewText.Lines)
                            {
                                switch (line.Type)
                                {
                                    case ChangeType.Inserted: sb.AppendLine($"+ {line.Text}"); break;
                                    case ChangeType.Deleted: sb.AppendLine($"- {line.Text}"); break;
                                    case ChangeType.Unchanged: sb.AppendLine($"  {line.Text}"); break;
                                    case ChangeType.Modified: sb.AppendLine($"~ {line.Text}"); break;
                                }
                            }

                            // Lưu Diff vào DeviceConfigDiffs
                            var diffEntity = new DeviceConfigDiff
                            {
                                DeviceId = device.DeviceId,
                                OldConfigId = previousCfg.ConfigId,
                                NewConfigId = cfg.ConfigId,
                                DiffContent = sb.ToString(),
                                CreatedAt = DateTime.UtcNow
                            };
                            db.DeviceConfigDiffs.Add(diffEntity);
                            await db.SaveChangesAsync(stoppingToken);
                        }
                        // --- KẾT THÚC DIFF ---

                        _logger.LogInformation("Backup and diff completed for Device {DeviceId}", device.DeviceId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in backup/diff for Device {DeviceId}", device.DeviceId);
                    }
                }

                // Delay giữa các lần chạy (ví dụ 5 phút)
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        // Giả sử hàm này thực thi SSH/Telnet and return config content
        private Task<string> FetchConfigFromDeviceAsync(Device device)
        {
            // TODO: thay thế bằng logic lấy config thực tế qua SSH/Telnet
            throw new NotImplementedException();
        }
    }
}
