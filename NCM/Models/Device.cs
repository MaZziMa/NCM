using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NCM.Models
{
    public class Device
    {
        [Key]
        public int DeviceId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [Required, StringLength(50)]
        public string IPAddress { get; set; }

        [StringLength(50)]
        public string Type { get; set; }

        public bool Status { get; set; }

        public DateTime? LastBackupTime { get; set; }
        public string SshUsername { get; set; }
        public string SshPassword { get; set; } // hoặc mã hóa sau

        // Navigation: lịch sử backup

        public virtual ICollection<DeviceConfig> DeviceConfigs { get; set; }
            = new HashSet<DeviceConfig>();
    }
}
