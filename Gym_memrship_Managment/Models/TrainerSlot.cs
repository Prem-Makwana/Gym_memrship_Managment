using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gym_memrship_Managment.Models
{
    public class TrainerSlot
    {
        [Key]
        public int TrainerSlotId { get; set; }

        public int TrainerId { get; set; }
        [ForeignKey(nameof(TrainerId))]
        public Trainer Trainer { get; set; } = null!;

        public DayOfWeek DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
