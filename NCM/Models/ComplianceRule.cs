using System.ComponentModel.DataAnnotations;

namespace NCM.Models
{
    public class ComplianceRule
    {
        [Key]
        public int RuleId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        public string MustContain { get; set; }

        public string MustNotContain { get; set; }
    }
}
