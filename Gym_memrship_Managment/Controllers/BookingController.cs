using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gym_memrship_Managment.Data;
using Gym_memrship_Managment.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Security.Claims;

namespace Gym_memrship_Managment.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BookingController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> BookTrainer()
        {
            var slots = await _db.TrainerSlots
                .Include(s => s.Trainer)
                .Where(s => s.IsActive)
                .ToListAsync();
            return View(slots);
        }

        [HttpPost]
        public async Task<IActionResult> BookSlot(int slotId, DateTime date)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var member = await _db.MemberProfiles.FirstOrDefaultAsync(m => m.UserId == userId);
            
            if (member == null) return BadRequest("Member profile not found.");

            var existing = await _db.PTBookings
                .FirstOrDefaultAsync(b => b.TrainerSlotId == slotId && b.BookingDate.Date == date.Date);

            if (existing != null && existing.Status != "Cancelled")
            {
                return BadRequest("Slot is already booked for this date.");
            }

            var booking = new PTBooking
            {
                MemberId = member.MemberId,
                TrainerSlotId = slotId,
                BookingDate = date,
                Status = "Scheduled"
            };

            _db.PTBookings.Add(booking);
            await _db.SaveChangesAsync();

            return RedirectToAction("MyProfile", "Member");
        }
        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var member = await _db.MemberProfiles.FirstOrDefaultAsync(m => m.UserId == userId);
            
            if (member == null) return RedirectToAction("Index", "Home");

            var bookings = await _db.PTBookings
                .Include(b => b.TrainerSlot)
                .ThenInclude(ts => ts.Trainer)
                .Where(b => b.MemberId == member.MemberId)
                .OrderByDescending(b => b.BookingDate)
                .ThenBy(b => b.TrainerSlot.StartTime)
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var member = await _db.MemberProfiles.FirstOrDefaultAsync(m => m.UserId == userId);
            
            if (member == null) return BadRequest("Member not found.");

            var booking = await _db.PTBookings
                .FirstOrDefaultAsync(b => b.BookingId == id && b.MemberId == member.MemberId);

            if (booking != null && booking.Status == "Scheduled")
            {
                booking.Status = "Cancelled";
                await _db.SaveChangesAsync();
                TempData["Success"] = "Booking cancelled successfully.";
            }

            return RedirectToAction(nameof(MyBookings));
        }
    }
}
