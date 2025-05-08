using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NCM.Models
{
    public class ComplianceResult
    {
        [Key]
        public int ResultId { get; set; }

        [Required]
        public int ConfigId { get; set; }

        [Required]
        public int RuleId { get; set; }

        [Required]
        public bool IsCompliant { get; set; }

        public DateTime CheckedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ConfigId")]
        public virtual DeviceConfig DeviceConfig { get; set; }

        [ForeignKey("RuleId")]
        public virtual ComplianceRule ComplianceRule { get; set; }
    }
}