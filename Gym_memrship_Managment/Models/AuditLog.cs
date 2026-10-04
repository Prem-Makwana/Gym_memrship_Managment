using System.ComponentModel.DataAnnotations;

namespace Gym_memrship_Managment.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        public string? UserId { get; set; }

        [MaxLength(100)]
        public string? UserEmail { get; set; }

        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EntityName { get; set; }

        [MaxLength(50)]
        public string? EntityId { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? IPAddress { get; set; }

        public string? Metadata { get; set; }
    }
}
