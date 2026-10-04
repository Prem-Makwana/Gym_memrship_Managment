using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gym_memrship_Managment.Models
{
    public class PTBooking
    {
        [Key]
        public int BookingId { get; set; }

        public int MemberId { get; set; }
        [ForeignKey(nameof(MemberId))]
        public MemberProfile Member { get; set; } = null!;

        public int TrainerSlotId { get; set; }
        [ForeignKey(nameof(TrainerSlotId))]
        public TrainerSlot TrainerSlot { get; set; } = null!;

        public DateTime BookingDate { get; set; }
        
        [MaxLength(20)]
        public string Status { get; set; } = "Scheduled"; 
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
