using Gym_memrship_Managment.Data;
using Gym_memrship_Managment.Interfaces;
using Gym_memrship_Managment.Models;
using Gym_memrship_Managment.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gym_memrship_Managment.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class BatchController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IBatchService _batchService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _audit;

        public BatchController(ApplicationDbContext db, IBatchService batchService,
            UserManager<ApplicationUser> userManager, IAuditService audit)
        {
            _db = db;
            _batchService = batchService;
            _userManager = userManager;
            _audit = audit;
        }

        public async Task<IActionResult> Index()
        {
            var batches = await _db.Batches
                .Include(b => b.Trainer)
                .Include(b => b.Enrollments)
                .Include(b => b.Schedules)
                .Where(b => !b.IsDeleted)
                .OrderBy(b => b.StartTime)
                .ToListAsync();
            return View(batches);
        }

        [HttpGet, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            return View(new BatchCreateVm
            {
                Trainers = await _db.Trainers.Where(t => t.Status == TrainerStatus.Active && !t.IsDeleted).ToListAsync()
            });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(BatchCreateVm model)
        {
            if (model.EndTime <= model.StartTime)
                ModelState.AddModelError("EndTime", "End time must be after start time.");

            if (!ModelState.IsValid)
            {
                model.Trainers = await _db.Trainers.Where(t => t.Status == TrainerStatus.Active && !t.IsDeleted).ToListAsync();
                return View(model);
            }

            var batch = new Batch
            {
                BatchName = model.BatchName,
                Description = model.Description,
                TrainerId = model.TrainerId,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                MaximumCapacity = model.MaximumCapacity,
                Location = model.Location,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Batches.Add(batch);
            await _db.SaveChangesAsync();

            foreach (var day in model.SelectedDays)
            {
                _db.BatchSchedules.Add(new BatchSchedule
                {
                    BatchId = batch.BatchId,
                    DayOfWeek = day,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime
                });
            }
            await _db.SaveChangesAsync();

            TempData["Success"] = "Batch created.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var batch = await _db.Batches.Include(b => b.Schedules).FirstOrDefaultAsync(b => b.BatchId == id && !b.IsDeleted);
            if (batch == null) return NotFound();

            return View(new BatchCreateVm
            {
                BatchName = batch.BatchName,
                Description = batch.Description,
                TrainerId = batch.TrainerId,
                Trainers = await _db.Trainers.Where(t => t.Status == TrainerStatus.Active && !t.IsDeleted).ToListAsync(),
                StartTime = batch.StartTime,
                EndTime = batch.EndTime,
                MaximumCapacity = batch.MaximumCapacity,
                Location = batch.Location,
                SelectedDays = batch.Schedules.Select(s => s.DayOfWeek).ToList()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BatchCreateVm model)
        {
            var batch = await _db.Batches.Include(b => b.Schedules).FirstOrDefaultAsync(b => b.BatchId == id && !b.IsDeleted);
            if (batch == null) return NotFound();

            batch.BatchName = model.BatchName;
            batch.Description = model.Description;
            batch.TrainerId = model.TrainerId;
            batch.StartTime = model.StartTime;
            batch.EndTime = model.EndTime;
            batch.MaximumCapacity = model.MaximumCapacity;
            batch.Location = model.Location;
            batch.UpdatedAt = DateTime.UtcNow;

            _db.BatchSchedules.RemoveRange(batch.Schedules);
            foreach (var day in model.SelectedDays)
            {
                _db.BatchSchedules.Add(new BatchSchedule
                {
                    BatchId = batch.BatchId,
                    DayOfWeek = day,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime
                });
            }
            await _db.SaveChangesAsync();
            TempData["Success"] = "Batch updated.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> BatchMembers(int id)
        {
            var batch = await _db.Batches
                .Include(b => b.Enrollments).ThenInclude(e => e.Member)
                .Include(b => b.Trainer)
                .FirstOrDefaultAsync(b => b.BatchId == id && !b.IsDeleted);
            if (batch == null) return NotFound();

            var occupancy = await _batchService.GetCurrentOccupancyAsync(id);
            return View(new BatchMembersVm
            {
                Batch = batch,
                Enrollments = batch.Enrollments.Where(e => e.Status == EnrollmentStatus.Active).ToList(),
                Occupied = occupancy,
                Capacity = batch.MaximumCapacity
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollMember(int batchId, int memberId)
        {
            var (success, message) = await _batchService.EnrollMemberAsync(memberId, batchId);
            if (success) TempData["Success"] = message;
            else TempData["Error"] = message;
            return RedirectToAction("BatchMembers", new { id = batchId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DropMember(int batchId, int memberId)
        {
            var (success, message) = await _batchService.DropMemberAsync(memberId, batchId);
            if (success) TempData["Success"] = message;
            else TempData["Error"] = message;
            return RedirectToAction("BatchMembers", new { id = batchId });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var batch = await _db.Batches.FindAsync(id);
            if (batch == null) return NotFound();
            batch.IsDeleted = true;
            batch.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Batch deleted.";
            return RedirectToAction("Index");
        }
    }

    [Authorize(Roles = "Admin,Staff")]
    public class TrainerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IAuditService _audit;
        private readonly UserManager<ApplicationUser> _userManager;

        public TrainerController(ApplicationDbContext db, IAuditService audit, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _audit = audit;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var trainers = await _db.Trainers.Include(t => t.Batches).Where(t => !t.IsDeleted).OrderBy(t => t.FullName).ToListAsync();
            return View(trainers);
        }

        [HttpGet, Authorize(Roles = "Admin")]
        public IActionResult Create() => View(new TrainerCreateVm());

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(TrainerCreateVm model)
        {
            if (!ModelState.IsValid) return View(model);

            _db.Trainers.Add(new Trainer
            {
                FullName = model.FullName,
                Phone = model.Phone,
                Email = model.Email,
                Specialization = model.Specialization,
                ExperienceYears = model.ExperienceYears,
                JoiningDate = model.JoiningDate
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Trainer created.";
            return RedirectToAction("Index");
        }

        [HttpGet, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var t = await _db.Trainers.FindAsync(id);
            if (t == null || t.IsDeleted) return NotFound();
            return View(new TrainerCreateVm
            {
                FullName = t.FullName,
                Phone = t.Phone,
                Email = t.Email,
                Specialization = t.Specialization,
                ExperienceYears = t.ExperienceYears,
                JoiningDate = t.JoiningDate
            });
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, TrainerCreateVm model)
        {
            if (!ModelState.IsValid) return View(model);
            var t = await _db.Trainers.FindAsync(id);
            if (t == null || t.IsDeleted) return NotFound();

            t.FullName = model.FullName;
            t.Phone = model.Phone;
            t.Email = model.Email;
            t.Specialization = model.Specialization;
            t.ExperienceYears = model.ExperienceYears;
            t.JoiningDate = model.JoiningDate;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Trainer updated.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var t = await _db.Trainers.Include(tr => tr.Batches).ThenInclude(b => b.Enrollments)
                .FirstOrDefaultAsync(tr => tr.TrainerId == id && !tr.IsDeleted);
            if (t == null) return NotFound();
            return View(t);
        }

        [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var t = await _db.Trainers.FindAsync(id);
            if (t == null) return NotFound();
            t.IsDeleted = true;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Trainer deleted.";
            return RedirectToAction("Index");
        }
    }
}
