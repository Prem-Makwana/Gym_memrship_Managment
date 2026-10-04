using System.ComponentModel.DataAnnotations;

namespace Gym_memrship_Managment.Models
{
    public enum TrainerStatus { Active, Inactive, OnLeave }

    public class Trainer
    {
        [Key]
        public int TrainerId { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required, MaxLength(15)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(150), EmailAddress]
        public string? Email { get; set; }

        [MaxLength(200)]
        public string? Specialization { get; set; }

        public int ExperienceYears { get; set; }

        public DateTime JoiningDate { get; set; } = DateTime.UtcNow;

        public string? ProfileImage { get; set; }

        public TrainerStatus Status { get; set; } = TrainerStatus.Active;

        public bool IsDeleted { get; set; } = false;

        // Navigation
        public ICollection<Batch> Batches { get; set; } = new List<Batch>();
    }
}
