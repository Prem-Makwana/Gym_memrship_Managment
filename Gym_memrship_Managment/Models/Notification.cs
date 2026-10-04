using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gym_memrship_Managment.Models
{
    public enum NotificationType { Info, Warning, Success, Danger, Expiry, Payment }

    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; } = NotificationType.Info;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAt { get; set; }
    }
}
