using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NCM.Models
{
    public class DeviceConfig
    {
        [Key]
        public int ConfigId { get; set; }                 // ← primary key

        [Required]
        public int DeviceId { get; set; }                 // ← foreign key

        [Required]
        public string ConfigText { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("DeviceId")]
        public virtual Device Device { get; set; }
    }
}
