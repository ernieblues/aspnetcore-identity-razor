using AspNetCoreMVC.Authorization;
using AspNetCoreMVC.Data;
using AspNetCoreMVC.Models;
using AspNetCoreMVC.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AspNetCoreMVC.Controllers
{
    [Authorize]
    public class SchedulesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthorizationService _auth;
        private readonly UserManager<ApplicationUser> _userManager;

        public SchedulesController(ApplicationDbContext context, IAuthorizationService auth, UserManager<ApplicationUser> userManager)
        {
            _auth = auth;
            _context = context;
            _userManager = userManager;
        }

        // --------------------------------------------------
        //                     CREATE
        // --------------------------------------------------

        // Create GET: Schedules/Create
        public async Task<IActionResult> Create()
        {
            // Schedule model with default values
            var model = new Schedule
            {
                OwnerId = _userManager.GetUserId(User),
                Date = DateTime.Today,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(17, 0)
            };

            // Populate dropdown menus
            if ((await _auth.AuthorizeAsync(User, model, ScheduleOperations.AssignOwner)).Succeeded)
                ViewBag.Users = SelectListForUsers(null);

            ViewBag.Times = SelectListForTimes();

            return View(model);
        }

        // Create POST: Schedules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OwnerId,Date,Day,StartTime,EndTime,Status")] Schedule input)
        {
            // Authorization
            if (!(await _auth.AuthorizeAsync(User, input, ScheduleOperations.Create)).Succeeded)
                return Forbid();

            // Validate times
            if (input.EndTime <= input.StartTime)
                ModelState.AddModelError(nameof(input.EndTime), "End time must be after start time.");

            // Invalid form
            if (!ModelState.IsValid)
            {
                // Populate dropdown menus
                if ((await _auth.AuthorizeAsync(User, input, ScheduleOperations.AssignOwner)).Succeeded)
                    ViewBag.Users = SelectListForUsers(input);

                ViewBag.Times = SelectListForTimes();

                return View(input);
            }

            // Owner and Status
            var currentUserId = _userManager.GetUserId(User);

            if (input.OwnerId != currentUserId)
            {
                // Owner: forbid tampering if not authorized
                if (!(await _auth.AuthorizeAsync(User, input, ScheduleOperations.AssignOwner)).Succeeded)
                    return Forbid();
            }

            if (input.Status != ScheduleStatus.Submitted)
            {
                // Status: forbid tampering if not authorized
                if (!(await _auth.AuthorizeAsync(User, input, ScheduleOperations.AssignStatus)).Succeeded)
                    return Forbid();
            }

            // Day of the week
            input.Day = input.Date.DayOfWeek.ToString();

            // Save and redirect
            _context.Schedules.Add(input);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Calendar));
        }

        // --------------------------------------------------
        //                      READ
        // --------------------------------------------------

        // Calendar GET: Schedules/Calendar
        public IActionResult Calendar(DateTime? weekStart, string? nav)
        {
            var start = weekStart ?? StartOfWeek(DateTime.Today);

            if (nav == "back") start = start.AddDays(-7);
            if (nav == "forward") start = start.AddDays(7);

            var end = start.AddDays(6);

            // Query schedules for the week
            IQueryable<Schedule> query = _context.Schedules
                .Include(s => s.User)
                .AsNoTracking()
                .Where(s => s.Date >= start && s.Date <= end);

            // Authorization check
            var isAuthorized = User.IsInRole(Constants.ScheduleManagersRole) ||
                User.IsInRole(Constants.ScheduleAdministratorsRole);

            // Get current user ID
            var currentUserId = _userManager.GetUserId(User);

            // If not authorized, filter schedules
            if (!isAuthorized)
            {
                query = query.Where(s =>
                    s.Status == ScheduleStatus.Approved ||
                    s.OwnerId == currentUserId);
            }

            //  Sort schedules by date and start time
            IList<Schedule> schedules = query
                .OrderBy(s => s.Date)
                .ThenBy(s => s.StartTime)
                .ToList();

            // Group schedules by day of the week
            var week = new WeekData
            {
                StartOfWeek = start,
                EndOfWeek = end,
                Monday = schedules.Where(s => s.Date.DayOfWeek == DayOfWeek.Monday).ToList(),
                Tuesday = schedules.Where(s => s.Date.DayOfWeek == DayOfWeek.Tuesday).ToList(),
                Wednesday = schedules.Where(s => s.Date.DayOfWeek == DayOfWeek.Wednesday).ToList(),
                Thursday = schedules.Where(s => s.Date.DayOfWeek == DayOfWeek.Thursday).ToList(),
                Friday = schedules.Where(s => s.Date.DayOfWeek == DayOfWeek.Friday).ToList(),
                Saturday = schedules.Where(s => s.Date.DayOfWeek == DayOfWeek.Saturday).ToList(),
                Sunday = schedules.Where(s => s.Date.DayOfWeek == DayOfWeek.Sunday).ToList()
            };

            // Pass current user ID to the view
            ViewBag.UserId = currentUserId;

            return View(week);
        }

        // Details GET: Schedules/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var schedule = await _context.Schedules
                .Include(s => s.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ScheduleId == id);

            if (schedule == null)
            {
                return NotFound();
            }

            // Get the current user ID
            var currentUserId = _userManager.GetUserId(User);

            // Pass current user ID to the view
            ViewBag.UserId = currentUserId;

            return View(schedule);
        }

        // Index GET: Schedules
        public async Task<IActionResult> Index(string sort = "date_asc")
        {
            // Query schedules with eager loading and no tracking
            IQueryable<Schedule> query = _context.Schedules
                .Include(s => s.User)
                .AsNoTracking();

            // Authorization check
            var isAuthorized = User.IsInRole(Constants.ScheduleManagersRole) ||
                               User.IsInRole(Constants.ScheduleAdministratorsRole);

            // Get current user ID
            var currentUserId = _userManager.GetUserId(User);

            // If not authorized, filter schedules
            if (!isAuthorized)
            {
                query = query.Where(s =>
                    s.Status == ScheduleStatus.Approved ||
                    s.OwnerId == currentUserId);
            }

            // Sort schedules based on the provided sort parameter
            query = sort switch
            {
                "user_asc" => query.OrderBy(s => s.User.UserName)
                                    .ThenBy(s => s.Date)
                                    .ThenBy(s => s.StartTime),

                "user_desc" => query.OrderByDescending(s => s.User.UserName)
                                    .ThenBy(s => s.Date)
                                    .ThenBy(s => s.StartTime),

                "date_desc" => query.OrderByDescending(s => s.Date)
                                    .ThenBy(s => s.User.UserName)
                                    .ThenBy(s => s.StartTime),

                _ => query.OrderBy(s => s.Date)   // "date_asc"
                                    .ThenBy(s => s.User.UserName)
                                    .ThenBy(s => s.StartTime),
            };

            // Set view bag for sorting links
            ViewBag.UserSort = sort == "user_asc" ? "user_desc" : "user_asc";
            ViewBag.DateSort = sort == "date_asc" ? "date_desc" : "date_asc";

            // Execute the query and get the list of schedules
            IList<Schedule> schedules = await query.ToListAsync();

            // Pass current user ID to the view
            ViewBag.UserId = currentUserId;

            return View(schedules);
        }

        // --------------------------------------------------
        //                     UPDATE
        // --------------------------------------------------

        // Approve POST:
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            // Find schedule
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();

            // Authorization required to approve schedule
            if (!(await _auth.AuthorizeAsync(User, schedule, ScheduleOperations.Approve)).Succeeded)
                return Forbid();

            schedule.Status = ScheduleStatus.Approved;

            // Save changes and redirect
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        // Edit GET: Schedules/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var schedule = await _context.Schedules
                .Include(s => s.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (schedule == null)
                return NotFound();

            // Populate dropdown menus
            ViewBag.Users = SelectListForUsers(schedule);
            ViewBag.Times = SelectListForTimes();

            return View(schedule);
        }

        // Edit POST: Schedules/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
             int id, [Bind("ScheduleId,OwnerId,Date,Day,StartTime,EndTime,Status")] Schedule input)
        {
            // ID mismatch
            if (id != input.ScheduleId) return NotFound();

            // Validate times
            if (input.EndTime <= input.StartTime)
                ModelState.AddModelError(nameof(input.EndTime), "End time must be after start time.");

            // Invalid form
            if (!ModelState.IsValid)
            {
                // Repopulate dropdown menus
                ViewBag.Users = SelectListForUsers(input);
                ViewBag.Times = SelectListForTimes();
                return View(input);
            }

            // Load existing schedule
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();

            // Authorize update operation
            if (!(await _auth.AuthorizeAsync(User, schedule, ScheduleOperations.Update)).Succeeded)
                return Forbid();

            // Current user ID
            var currentUserId = _userManager.GetUserId(User);

            // Owner: forbid tampering if not authorized
            if (input.OwnerId != currentUserId)
            {
                if (!(await _auth.AuthorizeAsync(User, schedule, ScheduleOperations.AssignOwner)).Succeeded)
                    return Forbid();
            }

            schedule.OwnerId = input.OwnerId;

            // Status: forbid tampering if not authorized
            if (input.Status != ScheduleStatus.Submitted &&
                !(await _auth.AuthorizeAsync(User, schedule, ScheduleOperations.AssignStatus)).Succeeded)
            {
                return Forbid();
            }

            schedule.Status = input.Status;

            // Update date and time fields
            schedule.Date = input.Date;
            schedule.Day = schedule.Date.DayOfWeek.ToString();
            schedule.StartTime = input.StartTime;
            schedule.EndTime = input.EndTime;

            // Save changes
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Calendar));
        }

        // Reject POST:
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            // Find schedule
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null) return NotFound();

            // Authorization required to reject schedule
            if (!(await _auth.AuthorizeAsync(User, schedule, ScheduleOperations.Reject)).Succeeded)
                return Forbid();

            schedule.Status = ScheduleStatus.Rejected;

            // Save changes and redirect
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        // --------------------------------------------------
        //                     DELETE
        // --------------------------------------------------

        // Delete GET: Schedules/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var schedule = await _context.Schedules
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.ScheduleId == id);
            if (schedule == null) return NotFound();

            return View(schedule);
        }

        // DeleteConfirmed POST: Schedules/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Find schedule
            var schedule = await _context.Schedules.FindAsync(id);

            // Schedule not found
            if (schedule == null) return NotFound();

            // Authorization required to delete schedule
            if (!(await _auth.AuthorizeAsync(User, schedule, ScheduleOperations.Delete)).Succeeded)
                return Forbid();

            // Delete schedule
            _context.Schedules.Remove(schedule);

            // Save changes and redirect
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Calendar));
        }

        // --------------------------------------------------
        //                  Helper Methods
        // --------------------------------------------------

        // Get all schedule data
        private List<Schedule> GetScheduleData()
        {
            return _context.Schedules.ToList();
        }

        // SelectList for times
        private SelectList SelectListForTimes()
        {
            var times = new List<SelectListItem>(48);
            for (int i = 0; i < 48; i++)
            {
                var t = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(i * 30));
                times.Add(new SelectListItem
                {
                    Value = t.ToString("h:mm tt", CultureInfo.InvariantCulture), // posted value
                    Text = t.ToString("h:mm tt", CultureInfo.InvariantCulture) // displayed text
                });
            }

            return new SelectList(times, "Value", "Text");
        }

        // SelectList for users
        private SelectList SelectListForUsers(Schedule? schedule)
        {
            var users = _context.Users.OrderBy(u => u.UserName).ToList();

            var selectList = new SelectList(users, "Id", "UserName", schedule?.OwnerId );

            return selectList;
        }

        // Get the start of the week (Monday)
        private static DateTime StartOfWeek(DateTime d)
        {
            int diff = (7 + (d.DayOfWeek - DayOfWeek.Monday)) % 7;
            return d.Date.AddDays(-diff);
        }
    }
}
