using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gym_memrship_Managment.Models
{
    public class MembershipPlan
    {
        [Key]
        public int PlanId { get; set; }

        [Required, MaxLength(100)]
        public string PlanName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Range(1, 3650)]
        public int DurationInDays { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RegistrationFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RenewalFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LateFeePerDay { get; set; }

        public int GracePeriodDays { get; set; } = 3;

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    }
}
