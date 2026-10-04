using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gym_memrship_Managment.Models
{
    public enum MemberStatus { Active, Inactive, Suspended, Blacklisted }
    public enum Gender { Male, Female, Other }

    public class MemberProfile
    {
        [Key]
        public int MemberId { get; set; }

        [Required, MaxLength(20)]
        public string MembershipNumber { get; set; } = string.Empty;

        public string? UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        public Gender Gender { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [Required, MaxLength(15)]
        public string Phone { get; set; } = string.Empty;

        [Required, MaxLength(150), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? EmergencyContactName { get; set; }

        [MaxLength(15)]
        public string? EmergencyContactPhone { get; set; }

        public DateTime JoinDate { get; set; } = DateTime.UtcNow;

        public string? ProfileImage { get; set; }

        public MemberStatus Status { get; set; } = MemberStatus.Active;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<Fine> Fines { get; set; } = new List<Fine>();
        public ICollection<BatchEnrollment> BatchEnrollments { get; set; } = new List<BatchEnrollment>();
    }
}
