using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gym_memrship_Managment.Models
{
    public enum MembershipStatus { Active, ExpiringSoon, Expired, Suspended, Cancelled }

    public class Membership
    {
        [Key]
        public int MembershipId { get; set; }

        public int MemberId { get; set; }
        [ForeignKey(nameof(MemberId))]
        public MemberProfile Member { get; set; } = null!;

        public int PlanId { get; set; }
        [ForeignKey(nameof(PlanId))]
        public MembershipPlan Plan { get; set; } = null!;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BaseAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal FinalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PaidAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DueAmount { get; set; }

        public int RenewalCount { get; set; } = 0;

        /// <summary>Manually overridden status (Suspended/Cancelled). Null = computed from dates.</summary>
        public MembershipStatus? OverrideStatus { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<Fine> Fines { get; set; } = new List<Fine>();

        [NotMapped]
        public MembershipStatus ComputedStatus
        {
            get
            {
                if (OverrideStatus.HasValue &&
                    (OverrideStatus == MembershipStatus.Suspended || OverrideStatus == MembershipStatus.Cancelled))
                    return OverrideStatus.Value;

                var today = DateTime.UtcNow.Date;
                if (EndDate.Date < today) return MembershipStatus.Expired;
                if ((EndDate.Date - today).TotalDays <= 7) return MembershipStatus.ExpiringSoon;
                return MembershipStatus.Active;
            }
        }

        [NotMapped]
        public int DaysRemaining => Math.Max(0, (EndDate.Date - DateTime.UtcNow.Date).Days);
    }
}
