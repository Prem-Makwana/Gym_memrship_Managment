using Gym_memrship_Managment.Data;
using Gym_memrship_Managment.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Gym_memrship_Managment.Seed
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                await db.Database.MigrateAsync();

                await SeedRolesAsync(roleManager);
                await SeedSystemSettingsAsync(db);
                var config = serviceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                await SeedUsersAsync(db, userManager, config);
                await SeedTrainersAsync(db);
                await SeedPlansAsync(db);
                await SeedBatchesAsync(db);
                await SeedMembersAsync(db, userManager);
                await SeedMembershipsAndPaymentsAsync(db);
                await SeedAttendanceAsync(db);

                logger.LogInformation("Database seeded successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var role in new[] { "Admin", "Staff", "Member" })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        private static async Task SeedSystemSettingsAsync(ApplicationDbContext db)
        {
            if (await db.SystemSettings.AnyAsync()) return;

            var settings = new[]
            {
                new SystemSetting { Key = "GymName", Value = "FitZone Pro Gym", Description = "Gym name" },
                new SystemSetting { Key = "GymAddress", Value = "123 Fitness Street, Health City", Description = "Gym address" },
                new SystemSetting { Key = "GymPhone", Value = "+1-555-GYM-FITX", Description = "Gym phone" },
                new SystemSetting { Key = "GymEmail", Value = "info@fitzonepro.com", Description = "Gym email" },
                new SystemSetting { Key = "Currency", Value = "USD", Description = "Currency code" },
                new SystemSetting { Key = "ExpiryThresholdDays", Value = "7", Description = "Days before expiry to show warning" },
                new SystemSetting { Key = "GracePeriodDays", Value = "3", Description = "Grace period after expiry" },
                new SystemSetting { Key = "LateFeePerDay", Value = "5.00", Description = "Late fee per day after grace period" },
            };
            db.SystemSettings.AddRange(settings);
            await db.SaveChangesAsync();
        }

        private static async Task SeedUsersAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager, Microsoft.Extensions.Configuration.IConfiguration config)
        {
            var usersToCreate = new[]
            {
                new { Email = "admin@gym.com", Password = config["SeedPasswords:Admin"] ?? "Admin@123!", FullName = "System Administrator", Role = "Admin" },
                new { Email = "staff@gym.com", Password = config["SeedPasswords:Staff"] ?? "Staff@123!", FullName = "Staff Member", Role = "Staff" },
                new { Email = "member@gym.com", Password = config["SeedPasswords:Member"] ?? "Member@123!", FullName = "Demo Member", Role = "Member" },
            };

            foreach (var u in usersToCreate)
            {
                if (await userManager.FindByEmailAsync(u.Email) != null) continue;
                var user = new ApplicationUser
                {
                    UserName = u.Email,
                    Email = u.Email,
                    FullName = u.FullName,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(user, u.Password);
                if (result.Succeeded) await userManager.AddToRoleAsync(user, u.Role);

                if (u.Role == "Staff")
                {
                    db.StaffProfiles.Add(new StaffProfile
                    {
                        UserId = user.Id,
                        EmployeeCode = "EMP001",
                        Designation = "Front Desk",
                        JoiningDate = DateTime.UtcNow.AddYears(-1),
                        IsActive = true
                    });
                }
            }
            await db.SaveChangesAsync();
        }

        private static async Task SeedTrainersAsync(ApplicationDbContext db)
        {
            if (await db.Trainers.AnyAsync()) return;

            db.Trainers.AddRange(
                new Trainer { FullName = "Alex Johnson", Phone = "5551234001", Email = "alex@fitzonepro.com", Specialization = "Strength & Conditioning", ExperienceYears = 8, JoiningDate = DateTime.UtcNow.AddYears(-3) },
                new Trainer { FullName = "Maria Garcia", Phone = "5551234002", Email = "maria@fitzonepro.com", Specialization = "Yoga & Flexibility", ExperienceYears = 6, JoiningDate = DateTime.UtcNow.AddYears(-2) },
                new Trainer { FullName = "James Wilson", Phone = "5551234003", Email = "james@fitzonepro.com", Specialization = "Cardio & HIIT", ExperienceYears = 5, JoiningDate = DateTime.UtcNow.AddYears(-2) }
            );
            await db.SaveChangesAsync();
        }

        private static async Task SeedPlansAsync(ApplicationDbContext db)
        {
            if (await db.MembershipPlans.AnyAsync()) return;

            db.MembershipPlans.AddRange(
                new MembershipPlan { PlanName = "Basic Monthly", Description = "Access to gym floor, 6amâ€“9pm", DurationInDays = 30, Price = 30, RegistrationFee = 20, RenewalFee = 5, LateFeePerDay = 2, GracePeriodDays = 3 },
                new MembershipPlan { PlanName = "Standard Quarterly", Description = "All basic + 1 batch class per day", DurationInDays = 90, Price = 75, RegistrationFee = 20, RenewalFee = 10, LateFeePerDay = 3, GracePeriodDays = 5 },
                new MembershipPlan { PlanName = "Premium Annual", Description = "Unlimited access + personal trainer 2x/week", DurationInDays = 365, Price = 250, RegistrationFee = 20, RenewalFee = 15, LateFeePerDay = 5, GracePeriodDays = 7 }
            );
            await db.SaveChangesAsync();
        }

        private static async Task SeedBatchesAsync(ApplicationDbContext db)
        {
            if (await db.Batches.AnyAsync()) return;

            var trainers = await db.Trainers.ToListAsync();
            var t1 = trainers[0];
            var t2 = trainers[1];
            var t3 = trainers[2];

            var batches = new[]
            {
                new Batch { BatchName = "Morning Strength", Description = "Heavy lifting & compound movements", TrainerId = t1.TrainerId, StartTime = new TimeOnly(6, 0), EndTime = new TimeOnly(7, 30), MaximumCapacity = 15, Location = "Main Gym Floor" },
                new Batch { BatchName = "Yoga & Flexibility", Description = "Yoga, stretching, mindfulness", TrainerId = t2.TrainerId, StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(8, 0), MaximumCapacity = 20, Location = "Studio A" },
                new Batch { BatchName = "HIIT Cardio Blast", Description = "High-intensity interval training", TrainerId = t3.TrainerId, StartTime = new TimeOnly(17, 30), EndTime = new TimeOnly(18, 30), MaximumCapacity = 25, Location = "Cardio Area" },
                new Batch { BatchName = "Evening Power", Description = "Strength & hypertrophy training", TrainerId = t1.TrainerId, StartTime = new TimeOnly(19, 0), EndTime = new TimeOnly(20, 30), MaximumCapacity = 12, Location = "Main Gym Floor" }
            };
            db.Batches.AddRange(batches);
            await db.SaveChangesAsync();

            var allBatches = await db.Batches.ToListAsync();
            var schedules = new List<BatchSchedule>();
            var weekdays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
            var mwf = new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday };
            var tts = new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday };

            foreach (var b in allBatches)
            {
                var days = b.BatchName.Contains("Yoga") ? tts : weekdays;
                foreach (var day in days)
                    schedules.Add(new BatchSchedule { BatchId = b.BatchId, DayOfWeek = day, StartTime = b.StartTime, EndTime = b.EndTime });
            }
            db.BatchSchedules.AddRange(schedules);
            await db.SaveChangesAsync();
        }

        private static async Task SeedMembersAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            if (await db.MemberProfiles.AnyAsync()) return;

            var sampleMembers = new[]
            {
                new { Name = "John Smith", Phone = "5550001001", Email = "john.smith@email.com", Gender = Gender.Male, Days = -300 },
                new { Name = "Sarah Davis", Phone = "5550001002", Email = "sarah.davis@email.com", Gender = Gender.Female, Days = -250 },
                new { Name = "Michael Brown", Phone = "5550001003", Email = "michael.brown@email.com", Gender = Gender.Male, Days = -180 },
                new { Name = "Emily Wilson", Phone = "5550001004", Email = "emily.wilson@email.com", Gender = Gender.Female, Days = -120 },
                new { Name = "David Taylor", Phone = "5550001005", Email = "david.taylor@email.com", Gender = Gender.Male, Days = -90 },
                new { Name = "Jessica Martinez", Phone = "5550001006", Email = "jessica.m@email.com", Gender = Gender.Female, Days = -60 },
                new { Name = "Ryan Anderson", Phone = "5550001007", Email = "ryan.a@email.com", Gender = Gender.Male, Days = -45 },
                new { Name = "Amanda Thomas", Phone = "5550001008", Email = "amanda.t@email.com", Gender = Gender.Female, Days = -30 },
                new { Name = "Kevin Jackson", Phone = "5550001009", Email = "kevin.j@email.com", Gender = Gender.Male, Days = -20 },
                new { Name = "Laura White", Phone = "5550001010", Email = "laura.w@email.com", Gender = Gender.Female, Days = -15 },
                new { Name = "Chris Harris", Phone = "5550001011", Email = "chris.h@email.com", Gender = Gender.Male, Days = -10 },
                new { Name = "Megan Clark", Phone = "5550001012", Email = "megan.c@email.com", Gender = Gender.Female, Days = -7 },
                new { Name = "Brandon Lewis", Phone = "5550001013", Email = "brandon.l@email.com", Gender = Gender.Male, Days = -5 },
                new { Name = "Ashley Robinson", Phone = "5550001014", Email = "ashley.r@email.com", Gender = Gender.Female, Days = -3 },
                new { Name = "Tyler Walker", Phone = "5550001015", Email = "tyler.w@email.com", Gender = Gender.Male, Days = -1 }
            };

            int counter = 1;
            foreach (var m in sampleMembers)
            {
                var member = new MemberProfile
                {
                    MembershipNumber = $"GYM{DateTime.UtcNow.Year}{counter:D4}",
                    FullName = m.Name,
                    Gender = m.Gender,
                    DateOfBirth = DateTime.UtcNow.AddYears(-25).AddDays(counter * 7),
                    Phone = m.Phone,
                    Email = m.Email,
                    JoinDate = DateTime.UtcNow.AddDays(m.Days),
                    Status = MemberStatus.Active,
                    CreatedAt = DateTime.UtcNow.AddDays(m.Days),
                    UpdatedAt = DateTime.UtcNow
                };
                db.MemberProfiles.Add(member);
                counter++;
            }

            // Link the demo member user
            var demoMember = await userManager.FindByEmailAsync("member@gym.com");
            if (demoMember != null && !await db.MemberProfiles.AnyAsync(m => m.UserId == demoMember.Id))
            {
                db.MemberProfiles.Add(new MemberProfile
                {
                    MembershipNumber = $"GYM{DateTime.UtcNow.Year}0000",
                    UserId = demoMember.Id,
                    FullName = "Demo Member",
                    Gender = Gender.Male,
                    Phone = "5550000000",
                    Email = "member@gym.com",
                    JoinDate = DateTime.UtcNow.AddMonths(-2),
                    Status = MemberStatus.Active,
                    CreatedAt = DateTime.UtcNow.AddMonths(-2),
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();
        }

        private static async Task SeedMembershipsAndPaymentsAsync(ApplicationDbContext db)
        {
            if (await db.Memberships.AnyAsync()) return;

            var members = await db.MemberProfiles.ToListAsync();
            var plans = await db.MembershipPlans.ToListAsync();
            var rng = new Random(42);

            foreach (var member in members)
            {
                var plan = plans[rng.Next(plans.Count)];
                var startDate = member.JoinDate;
                var endDate = startDate.AddDays(plan.DurationInDays);
                var finalAmount = plan.Price + plan.RegistrationFee;

                var membership = new Membership
                {
                    MemberId = member.MemberId,
                    PlanId = plan.PlanId,
                    StartDate = startDate,
                    EndDate = endDate,
                    BaseAmount = finalAmount,
                    Discount = 0,
                    FinalAmount = finalAmount,
                    PaidAmount = finalAmount,
                    DueAmount = 0,
                    RenewalCount = 0,
                    CreatedAt = startDate,
                    UpdatedAt = startDate
                };
                db.Memberships.Add(membership);

                var receipt = $"RCP{startDate:yyyyMM}{membership.MembershipId:D4}";
                db.Payments.Add(new Payment
                {
                    ReceiptNumber = receipt,
                    MemberId = member.MemberId,
                    MembershipId = membership.MembershipId,
                    PaymentDate = startDate,
                    Amount = finalAmount,
                    PaymentMethod = (PaymentMethod)(rng.Next(3)),
                    PaymentType = PaymentType.Registration,
                    Notes = "Initial enrollment",
                    ReceivedBy = "admin@gym.com",
                    CreatedAt = startDate
                });
            }
            await db.SaveChangesAsync();
        }

        private static async Task SeedAttendanceAsync(ApplicationDbContext db)
        {
            if (await db.Attendances.AnyAsync()) return;

            var members = await db.MemberProfiles.ToListAsync();
            var batches = await db.Batches.ToListAsync();
            var rng = new Random(42);

            // Enroll members in batches
            var batchIdx = 0;
            foreach (var member in members)
            {
                var batch = batches[batchIdx % batches.Count];
                db.BatchEnrollments.Add(new BatchEnrollment
                {
                    MemberId = member.MemberId,
                    BatchId = batch.BatchId,
                    EnrolledDate = member.JoinDate,
                    Status = EnrollmentStatus.Active
                });
                batchIdx++;
            }
            await db.SaveChangesAsync();

            // Seed 14 days of attendance
            for (var i = 13; i >= 0; i--)
            {
                var date = DateTime.UtcNow.Date.AddDays(-i);
                var presentMembers = members.Where(_ => rng.Next(100) < 70).ToList(); // 70% attendance rate
                foreach (var member in presentMembers)
                {
                    var checkIn = date.AddHours(rng.Next(6, 10));
                    db.Attendances.Add(new Attendance
                    {
                        MemberId = member.MemberId,
                        Date = date,
                        CheckInTime = checkIn,
                        CheckOutTime = checkIn.AddHours(rng.Next(1, 3)),
                        DurationMinutes = rng.Next(60, 180),
                        Status = AttendanceStatus.Present
                    });
                }
            }
            await db.SaveChangesAsync();
        }
    }
}



