using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gym_memrship_Managment.Models
{
    public enum FineStatus { Pending, Paid, Waived }

    public class Fine
    {
        [Key]
        public int FineId { get; set; }

        public int MemberId { get; set; }
        [ForeignKey(nameof(MemberId))]
        public MemberProfile Member { get; set; } = null!;

        public int? MembershipId { get; set; }
        [ForeignKey(nameof(MembershipId))]
        public Membership? Membership { get; set; }

        [Required, MaxLength(300)]
        public string Reason { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime DateIssued { get; set; } = DateTime.UtcNow;

        public FineStatus Status { get; set; } = FineStatus.Pending;

        public DateTime? PaidDate { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }
    }
}
