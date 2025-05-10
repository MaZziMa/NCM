using System;
using System.Collections.Generic;

namespace NCM.Models
{
    public class Dashboard
    {
        // Thông tin thiết bị
        public Device Device { get; set; }

        // Các chỉ số SNMP mới nhất
        public string SysName { get; set; }
        public string SysDescr { get; set; }
        public string Uptime { get; set; }

        // Lịch sử SNMP cho chart
        public IEnumerable<DeviceSnmpMetric> History { get; set; }
    }
}
