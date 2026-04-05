using MedicalOnboardingApplication.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly MedicalOnboardingApplicationContext _context;

    public DashboardController(MedicalOnboardingApplicationContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Admin"))
            return RedirectToAction("Index", "Admin");

        var user = await _context.Users
            .Include(u => u.EmployeeType)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        var assignedCourses = await _context.Courses
            .Where(c => c.CourseEmployeeTypes
                .Any(cet => cet.EmployeeTypeId == user.EmployeeTypeId))
            .ToListAsync();

        var completedCourseIds = await _context.UserCourseProgress
            .Where(p => p.UserId == user.Id)
            .Select(p => p.CourseId)
            .ToListAsync();

        bool allCoursesCompleted = assignedCourses.Any() &&
            assignedCourses.All(c => completedCourseIds.Contains(c.Id));

        // Check working hours
        var now = DateTime.Now;
        var currentDay = now.DayOfWeek;
        var currentTime = TimeOnly.FromDateTime(now);

        var todayShifts = await _context.WorkSchedules
            .Where(s => s.UserId == user.Id && s.Day == currentDay)
            .ToListAsync();

        bool isWorkingNow = todayShifts.Any(s =>
            currentTime >= s.StartTime && currentTime <= s.EndTime);

        // Find next shift info
        string nextShiftInfo = "";
        if (!isWorkingNow)
        {
            var dayNames = new Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Monday,    "Luni" },
                { DayOfWeek.Tuesday,   "Marți" },
                { DayOfWeek.Wednesday, "Miercuri" },
                { DayOfWeek.Thursday,  "Joi" },
                { DayOfWeek.Friday,    "Vineri" },
                { DayOfWeek.Saturday,  "Sâmbătă" },
                { DayOfWeek.Sunday,    "Duminică" }
            };

            var order = new[] {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
            };

            var allShifts = await _context.WorkSchedules
                .Where(s => s.UserId == user.Id)
                .OrderBy(s => s.Day)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            // Check later today first
            var laterToday = allShifts
                .Where(s => s.Day == currentDay && s.StartTime > currentTime)
                .OrderBy(s => s.StartTime)
                .FirstOrDefault();

            if (laterToday != null)
            {
                nextShiftInfo = $"Următoarea tură azi la {laterToday.StartTime:HH:mm}.";
            }
            else
            {
                // Find next working day
                var todayIndex = Array.IndexOf(order, currentDay);
                for (int i = 1; i <= 7; i++)
                {
                    var nextDay = order[(todayIndex + i) % 7];
                    var nextShift = allShifts
                        .Where(s => s.Day == nextDay)
                        .OrderBy(s => s.StartTime)
                        .FirstOrDefault();

                    if (nextShift != null)
                    {
                        nextShiftInfo = $"Următoarea tură: {dayNames[nextDay]} la {nextShift.StartTime:HH:mm}.";
                        break;
                    }
                }
            }
        }

        ViewBag.IsWorkingNow = isWorkingNow;
        ViewBag.NextShiftInfo = nextShiftInfo;

        return View();
    }
}