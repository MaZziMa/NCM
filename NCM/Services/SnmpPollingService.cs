using Microsoft.EntityFrameworkCore;
using NCM.Data;
using NCM.Models;
using NCM.Services;

public class SnmpPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SnmpPollingService> _logger;
    private readonly SnmpService _snmpService;
    private readonly TimeSpan _interval;

    public SnmpPollingService(
        IServiceScopeFactory scopeFactory,
        SnmpService snmpService,
        IConfiguration config,
        ILogger<SnmpPollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _snmpService = snmpService;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(config.GetValue<int>("SnmpPolling:IntervalMinutes", 5));
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
                    var data = _snmpService.GetBasicInfo(device.IPAddress);
                    foreach (var kv in data)
                    {
                        db.DeviceSnmpMetrics.Add(new DeviceSnmpMetric
                        {
                            DeviceId = device.DeviceId,
                            MetricName = kv.Key,
                            MetricValue = kv.Value,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "SNMP polling failed for device {DeviceId}", device.DeviceId);
                }
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
