using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Gym_memrship_Managment.Models
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string? ProfileImagePath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public MemberProfile? MemberProfile { get; set; }
        public StaffProfile? StaffProfile { get; set; }
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
