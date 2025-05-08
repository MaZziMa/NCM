using System.ComponentModel.DataAnnotations;

namespace NCM.Models
{
    public class ComplianceRule
    {
        [Key]
        public int RuleId { get; set; }

        [Required, StringLength(100)]
        public string RuleName { get; set; }

        [Required, StringLength(200)]
        public string RequiredString { get; set; }

        [StringLength(250)]
        public string Description { get; set; }
    }
}