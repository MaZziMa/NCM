using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NCM.Models
{
    public class DeviceConfig
    {
        [Key]
        public int ConfigId { get; set; }

        [Required]
        public int DeviceId { get; set; }

        [Required]
        public string ConfigContent { get; set; }

        [Required]
        public DateTime UploadTime { get; set; }

        [ForeignKey("DeviceId")]
        public virtual Device Device { get; set; }
    }
}