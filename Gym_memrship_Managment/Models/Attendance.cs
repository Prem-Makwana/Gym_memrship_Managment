using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gym_memrship_Managment.Models
{
    public enum AttendanceStatus { Present, Absent, Late, CheckedOut }

    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }

        public int MemberId { get; set; }
        [ForeignKey(nameof(MemberId))]
        public MemberProfile Member { get; set; } = null!;

        public int? BatchId { get; set; }
        [ForeignKey(nameof(BatchId))]
        public Batch? Batch { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow.Date;

        public DateTime CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        public int? DurationMinutes { get; set; }

        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

        [MaxLength(200)]
        public string? Notes { get; set; }
    }
}
