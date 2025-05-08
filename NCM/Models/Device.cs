using System;
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

        public string Type { get; set; }

        public bool Status { get; set; }

        public DateTime? LastBackupTime { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "SSH Username")]
        public string SshUsername { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "SSH Password")]
        public string SshPassword { get; set; }  // Đã mã hóa trước khi lưu

        // Navigation: lịch sử backup
        public virtual ICollection<DeviceConfig> DeviceConfigs { get; set; }
            = new HashSet<DeviceConfig>();
    }
}