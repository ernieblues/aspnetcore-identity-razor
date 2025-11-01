using AspNetCoreMVC.Authorization;
using AspNetCoreMVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreMVC.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider, string seedUserPW)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // For testing purposes seed all with the same password.
                // Password is set with the following:
                // dotnet user-secrets set SeedUserPW <pw>

                var adminId = await EnsureUser(serviceProvider, seedUserPW, "admin@contoso.com");
                await EnsureRole(serviceProvider, adminId, Constants.ScheduleAdministratorsRole);
                
                var managerId = await EnsureUser(serviceProvider, seedUserPW, "manager@contoso.com");
                await EnsureRole(serviceProvider, managerId, Constants.ScheduleManagersRole);

                var userId = await EnsureUser(serviceProvider, seedUserPW, "user@contoso.com");

                var timId = await EnsureUser(serviceProvider, seedUserPW, "tim.cook@mail.com");
                await EnsureRole(serviceProvider, timId, Constants.ScheduleManagersRole);

                var sallyId = await EnsureUser(serviceProvider, seedUserPW, "sally.server@mail.com");

                var billyId = await EnsureUser(serviceProvider, seedUserPW, "billy.barback@mail.com");

                SeedDB(context, userId, timId, sallyId, billyId);
            }
        }

        private static async Task<string> EnsureUser(IServiceProvider serviceProvider, string seedUserPW, string email)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, seedUserPW);
            }

            return user.Id;
        }

        private static async Task<IdentityResult> EnsureRole(IServiceProvider serviceProvider,
                                                                      string uid, string role)
        {
            IdentityResult IR;
            var roleManager = serviceProvider.GetService<RoleManager<ApplicationRole>>()
                ?? throw new Exception("roleManager null");

            if (!await roleManager.RoleExistsAsync(role))
            {
                IR = await roleManager.CreateAsync(new ApplicationRole(role));
            }

            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var user = await userManager.FindByIdAsync(uid)
                ?? throw new Exception($"User with ID '{uid}' not found.");

            IR = await userManager.AddToRoleAsync(user, role);

            return IR;
        }

        public static void SeedDB(ApplicationDbContext context, string userId, string timId, string sallyId, string billyId)
        {
            if (context.Schedules.Any())
            {
                return;   // DB has been seeded
            }

            var userIds = new[] { timId, userId, sallyId, billyId };

            var shifts = new[]
            {
                new { Start = new TimeOnly(8, 0),  End = new TimeOnly(16, 0) },
                new { Start = new TimeOnly(9, 0),  End = new TimeOnly(17, 0) },
                new { Start = new TimeOnly(10, 0), End = new TimeOnly(18, 0) },
                new { Start = new TimeOnly(12, 0), End = new TimeOnly(20, 0) }
            };

            var statuses = Enum.GetValues(typeof(ScheduleStatus)).Cast<ScheduleStatus>().ToArray();
            var random = new Random();
            var schedules = new List<Schedule>();

            // Step 1: Get today's date
            DateTime today = DateTime.Today;

            // Step 2: Get the start of the week (Monday)
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime recentMonday = DateTime.Today.AddDays(-diff);

            // Step 3: Define the schedule range
            DateTime startDate = recentMonday.AddDays(-28); // 4 weeks before
            DateTime endDate = recentMonday.AddDays(60);  // 8.5 weeks after

            // Step 4: Generate schedules
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                string dayName = date.DayOfWeek.ToString();

                // Randomly select 3 or 4 users to work that day
                int numWorkers = random.Next(3, 5); // 3 or 4
                var shuffledUserIds = userIds.OrderBy(_ => random.Next()).Take(numWorkers);

                foreach (var ownerId in shuffledUserIds)
                {
                    var shift = shifts[random.Next(shifts.Length)];

                    schedules.Add(new Schedule
                    {
                        OwnerId = ownerId,
                        Date = date,
                        Day = dayName,
                        StartTime = shift.Start,
                        EndTime = shift.End,
                        Status = statuses[random.Next(statuses.Length)]
                    });
                }
            }

            context.Schedules.AddRange(schedules);
            context.SaveChanges();
        }
    }
}