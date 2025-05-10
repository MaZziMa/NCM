using System;
using System.ComponentModel.DataAnnotations;

namespace NCM.Models
{
    public class DeviceConfigDiff
    {
        [Key]                                   // ← khai báo DiffId là khóa chính
        public int DiffId { get; set; }

        public int DeviceId { get; set; }
        public int OldConfigId { get; set; }
        public int NewConfigId { get; set; }
        public string DiffContent { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public Device Device { get; set; }
        public DeviceConfig OldConfig { get; set; }
        public DeviceConfig NewConfig { get; set; }

    }
}
