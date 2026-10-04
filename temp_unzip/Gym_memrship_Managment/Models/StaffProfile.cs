using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gym_memrship_Managment.Models
{
    public class StaffProfile
    {
        [Key]
        public int StaffProfileId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        [Required, MaxLength(20)]
        public string EmployeeCode { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Designation { get; set; } = string.Empty;

        public DateTime JoiningDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}
