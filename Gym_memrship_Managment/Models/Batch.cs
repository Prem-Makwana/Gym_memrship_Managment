using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gym_memrship_Managment.Models
{
    public class Batch
    {
        [Key]
        public int BatchId { get; set; }

        [Required, MaxLength(100)]
        public string BatchName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? TrainerId { get; set; }
        [ForeignKey(nameof(TrainerId))]
        public Trainer? Trainer { get; set; }

        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        [Range(1, 100)]
        public int MaximumCapacity { get; set; } = 20;

        [MaxLength(200)]
        public string? Location { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<BatchSchedule> Schedules { get; set; } = new List<BatchSchedule>();
        public ICollection<BatchEnrollment> Enrollments { get; set; } = new List<BatchEnrollment>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }

    public class BatchSchedule
    {
        [Key]
        public int BatchScheduleId { get; set; }

        public int BatchId { get; set; }
        [ForeignKey(nameof(BatchId))]
        public Batch Batch { get; set; } = null!;

        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }

    public enum EnrollmentStatus { Active, Transferred, Dropped, Completed }

    public class BatchEnrollment
    {
        [Key]
        public int BatchEnrollmentId { get; set; }

        public int MemberId { get; set; }
        [ForeignKey(nameof(MemberId))]
        public MemberProfile Member { get; set; } = null!;

        public int BatchId { get; set; }
        [ForeignKey(nameof(BatchId))]
        public Batch Batch { get; set; } = null!;

        public DateTime EnrolledDate { get; set; } = DateTime.UtcNow;

        public DateTime? TransferredDate { get; set; }

        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    }
}
