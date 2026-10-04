using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gym_memrship_Managment.Models
{
    public enum PaymentMethod { Cash, Card, BankTransfer, UPI, Cheque, Online }
    public enum PaymentType { Registration, Monthly, Renewal, LateFee, Fine, Miscellaneous }

    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required, MaxLength(30)]
        public string ReceiptNumber { get; set; } = string.Empty;

        public int MemberId { get; set; }
        [ForeignKey(nameof(MemberId))]
        public MemberProfile Member { get; set; } = null!;

        public int? MembershipId { get; set; }
        [ForeignKey(nameof(MembershipId))]
        public Membership? Membership { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
        public PaymentType PaymentType { get; set; }

        [MaxLength(100)]
        public string? TransactionReference { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(100)]
        public string? ReceivedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
