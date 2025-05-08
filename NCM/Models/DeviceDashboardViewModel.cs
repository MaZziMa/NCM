using NCM.Models;

public class DeviceDashboardViewModel
{
    public Device Device { get; set; }
    public string SysName { get; set; }
    public string SysDescr { get; set; }
    public string Uptime { get; set; }
    public IEnumerable<DeviceSnmpMetric> History { get; set; }
}
