using System.ComponentModel.DataAnnotations;

namespace Gym_memrship_Managment.Models
{
    public class SystemSetting
    {
        [Key]
        public int SettingId { get; set; }

        [Required, MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Value { get; set; }

        [MaxLength(300)]
        public string? Description { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
