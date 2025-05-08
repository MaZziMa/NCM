using System;
using System.ComponentModel.DataAnnotations;

namespace NCM.Models
{
    public class DeviceSnmpMetric
    {
        [Key]
        public int MetricId { get; set; }           // ← dấu [Key] xác định khoá
        public int DeviceId { get; set; }
        public string MetricName { get; set; }
        public string MetricValue { get; set; }
        public DateTime Timestamp { get; set; }
        public Device Device { get; set; }
    }
}
