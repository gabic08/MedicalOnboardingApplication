using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Filters;
using MedicalOnboardingApplication.Models;
using MedicalOnboardingApplication.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers;

[Authorize(Roles = "Admin")]
[RequireClinic]
public class AdminEmployeesController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MedicalOnboardingApplicationContext _context;

    public AdminEmployeesController(
        UserManager<ApplicationUser> userManager,
        MedicalOnboardingApplicationContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index(string search, int? employeeTypeId)
    {
        var currentAdmin = await _userManager.Users
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        var query = _userManager.Users
            .Include(u => u.EmployeeType)
            .Where(u => u.ClinicId == currentAdmin.ClinicId
                     && u.UserRoles.Any(ur => ur.Role.Name != "Admin"));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                (u.EmployeeType != null && u.EmployeeType.Name.ToLower().Contains(term)));
        }

        if (employeeTypeId.HasValue)
        {
            query = query.Where(u => u.EmployeeTypeId == employeeTypeId.Value);
        }

        var employees = await query.ToListAsync();

        ViewBag.Search = search;
        ViewBag.EmployeeTypeId = employeeTypeId;
        ViewBag.EmployeeTypes = await _context.EmployeeTypes.ToListAsync();

        return View(employees);
    }


    private static List<EmployeeScheduleDayViewModel> BuildEmptySchedule()
    {
        var order = new[] {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    };

        return order.Select(d => new EmployeeScheduleDayViewModel
        {
            Day = d,
            IsDayOff = false,
            Shifts = new List<ShiftViewModel>
        {
            new ShiftViewModel()
        }
        }).ToList();
    }

    private static readonly Dictionary<DayOfWeek, string> DayNames = new()
{
    { DayOfWeek.Monday,    "Luni" },
    { DayOfWeek.Tuesday,   "Marți" },
    { DayOfWeek.Wednesday, "Miercuri" },
    { DayOfWeek.Thursday,  "Joi" },
    { DayOfWeek.Friday,    "Vineri" },
    { DayOfWeek.Saturday,  "Sâmbătă" },
    { DayOfWeek.Sunday,    "Duminică" }
};

    public IActionResult Create()
    {
        ViewBag.EmployeeTypes = new SelectList(_context.EmployeeTypes, "Id", "Name");
        ViewBag.DayNames = DayNames;

        return View(new CreateEmployeeViewModel
        {
            Schedule = BuildEmptySchedule()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateEmployeeViewModel vm)
    {
        // Validate schedule
        foreach (var day in vm.Schedule)
        {
            if (!day.IsDayOff)
            {
                if (!day.Shifts.Any())
                {
                    ModelState.AddModelError("", $"{DayNames[day.Day]}: adaugă cel puțin o tură sau marchează ca zi liberă.");
                }
                else
                {
                    foreach (var shift in day.Shifts)
                    {
                        if (shift.EndTime <= shift.StartTime)
                            ModelState.AddModelError("", $"{DayNames[day.Day]}: ora de sfârșit trebuie să fie după ora de început.");
                    }
                }
            }
        }

        if (vm.EmployeeTypeId == null)
            ModelState.AddModelError("", "Tipul de angajat este obligatoriu.");

        if (!ModelState.IsValid)
        {
            ViewBag.EmployeeTypes = new SelectList(_context.EmployeeTypes, "Id", "Name", vm.EmployeeTypeId);
            ViewBag.DayNames = DayNames;
            return View(vm);
        }

        var admin = await _userManager.GetUserAsync(User);

        var existingUser = await _userManager.FindByEmailAsync(vm.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError("Email", "Un cont cu această adresă de email există deja.");
            ViewBag.EmployeeTypes = new SelectList(_context.EmployeeTypes, "Id", "Name", vm.EmployeeTypeId);
            ViewBag.DayNames = DayNames;
            return View(vm);
        }

        var user = new ApplicationUser
        {
            UserName = vm.Email,
            Email = vm.Email,
            FirstName = vm.FirstName,
            LastName = vm.LastName,
            EmployeeTypeId = vm.EmployeeTypeId,
            ClinicId = admin.ClinicId,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, "P@ssw0rd");

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            ViewBag.EmployeeTypes = new SelectList(_context.EmployeeTypes, "Id", "Name", vm.EmployeeTypeId);
            ViewBag.DayNames = DayNames;
            return View(vm);
        }

        await _userManager.AddToRoleAsync(user, "Employee");

        // Save schedule
        foreach (var day in vm.Schedule.Where(d => !d.IsDayOff))
        {
            foreach (var shift in day.Shifts)
            {
                _context.WorkSchedules.Add(new WorkSchedule
                {
                    UserId = user.Id,
                    Day = day.Day,
                    StartTime = shift.StartTime,
                    EndTime = shift.EndTime
                });
            }
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            return NotFound();

        var schedules = await _context.WorkSchedules
            .Where(s => s.UserId == id)
            .OrderBy(s => s.Day)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        var order = new[] {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    };

        var schedule = order.Select(d =>
        {
            var dayShifts = schedules.Where(s => s.Day == d).ToList();
            return new EmployeeScheduleDayViewModel
            {
                Day = d,
                IsDayOff = !dayShifts.Any(),
                Shifts = dayShifts.Any()
                    ? dayShifts.Select(s => new ShiftViewModel
                    {
                        StartTime = s.StartTime,
                        EndTime = s.EndTime
                    }).ToList()
                    : new List<ShiftViewModel> { new ShiftViewModel() }
            };
        }).ToList();

        ViewBag.EmployeeTypes = new SelectList(_context.EmployeeTypes, "Id", "Name", user.EmployeeTypeId);
        ViewBag.DayNames = DayNames;

        return View(new EditEmployeeViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            EmployeeTypeId = user.EmployeeTypeId,
            Schedule = schedule
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditEmployeeViewModel vm)
    {
        // Validate schedule
        foreach (var day in vm.Schedule)
        {
            if (!day.IsDayOff)
            {
                if (!day.Shifts.Any())
                {
                    ModelState.AddModelError("", $"{DayNames[day.Day]}: adaugă cel puțin o tură sau marchează ca zi liberă.");
                }
                else
                {
                    foreach (var shift in day.Shifts)
                    {
                        if (shift.EndTime <= shift.StartTime)
                            ModelState.AddModelError("", $"{DayNames[day.Day]}: ora de sfârșit trebuie să fie după ora de început.");
                    }
                }
            }
        }

        if (vm.EmployeeTypeId == null)
            ModelState.AddModelError("", "Tipul de angajat este obligatoriu.");

        if (!ModelState.IsValid)
        {
            ViewBag.EmployeeTypes = new SelectList(_context.EmployeeTypes, "Id", "Name", vm.EmployeeTypeId);
            ViewBag.DayNames = DayNames;
            return View(vm);
        }

        var user = await _userManager.FindByIdAsync(vm.Id.ToString());
        if (user == null)
            return NotFound();

        user.FirstName = vm.FirstName;
        user.LastName = vm.LastName;
        user.Email = vm.Email;
        user.UserName = vm.Email;
        user.EmployeeTypeId = vm.EmployeeTypeId;

        await _userManager.UpdateAsync(user);

        // Replace schedule
        var existing = _context.WorkSchedules.Where(s => s.UserId == vm.Id);
        _context.WorkSchedules.RemoveRange(existing);

        foreach (var day in vm.Schedule.Where(d => !d.IsDayOff))
        {
            foreach (var shift in day.Shifts)
            {
                _context.WorkSchedules.Add(new WorkSchedule
                {
                    UserId = vm.Id,
                    Day = day.Day,
                    StartTime = shift.StartTime,
                    EndTime = shift.EndTime
                });
            }
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        await _userManager.DeleteAsync(user);

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Report(int id)
    {
        var admin = await _userManager.Users
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (admin?.ClinicId == null)
            return RedirectToAction("Index", "Admin");

        var employee = await _userManager.Users
            .Include(u => u.EmployeeType)
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.Id == id && u.ClinicId == admin.ClinicId);

        if (employee == null)
            return NotFound();

        var assignedCourses = await _context.Courses
            .Include(c => c.Chapters)
            .Include(c => c.CourseEmployeeTypes)
            .Where(c => c.CourseEmployeeTypes
                .Any(cet => cet.EmployeeTypeId == employee.EmployeeTypeId))
            .OrderBy(c => c.Order)
            .ToListAsync();

        var completedCourseIds = await _context.UserCourseProgress
            .Where(p => p.UserId == employee.Id)
            .Select(p => p.CourseId)
            .ToListAsync();

        var completedChapterIds = await _context.UserChapterProgress
            .Where(p => p.UserId == employee.Id)
            .Select(p => p.ChapterId)
            .ToListAsync();

        var chapterProgressMap = await _context.UserChapterProgress
            .Where(p => p.UserId == employee.Id)
            .ToDictionaryAsync(p => p.ChapterId, p => p.CompletedAt);

        var courseProgressMap = await _context.UserCourseProgress
            .Where(p => p.UserId == employee.Id)
            .ToDictionaryAsync(p => p.CourseId, p => p.CompletedAt);

        var testSessions = await _context.TestSessions
            .Where(s => s.UserId == employee.Id && s.IsCompleted)
            .OrderBy(s => s.StartedAt)
            .ToListAsync();

        const double passingScore = 70.0;

        bool allCoursesCompleted = assignedCourses.Any() &&
            assignedCourses.All(c => completedCourseIds.Contains(c.Id));

        double averageTestScore = testSessions.Any()
            ? testSessions.Average(s => s.TotalQuestions > 0
                ? (double)s.CorrectAnswers / s.TotalQuestions * 100
                : 0)
            : 0;

        bool isCompliant = allCoursesCompleted && averageTestScore >= passingScore;

        string nonCompliantReason = "";
        if (!isCompliant)
        {
            if (!assignedCourses.Any())
                nonCompliantReason = "Niciun curs atribuit.";
            else if (!allCoursesCompleted)
            {
                var remaining = assignedCourses.Count(c => !completedCourseIds.Contains(c.Id));
                if (remaining == 1)
                    nonCompliantReason = "Un curs nefinalizat.";
                else if (remaining > 1)
                    nonCompliantReason = $"{remaining} cursuri nefinalizate.";
            }
            else if (!testSessions.Any())
                nonCompliantReason = "Niciun test susținut încă.";
            else
                nonCompliantReason = $"Scorul mediu la teste este {averageTestScore:0.0}%, necesar ≥ {passingScore}%.";
        }

        ViewBag.Employee = employee;
        ViewBag.AssignedCourses = assignedCourses;
        ViewBag.CompletedCourseIds = completedCourseIds;
        ViewBag.CompletedChapterIds = completedChapterIds;
        ViewBag.ChapterProgressMap = chapterProgressMap;
        ViewBag.CourseProgressMap = courseProgressMap;
        ViewBag.TestSessions = testSessions;
        ViewBag.IsCompliant = isCompliant;
        ViewBag.NonCompliantReason = nonCompliantReason;
        ViewBag.AverageTestScore = averageTestScore;
        ViewBag.PassingScore = passingScore;
        ViewBag.ReportDate = DateTime.Now;
        ViewBag.ClinicName = admin.Clinic?.Name;

        return View();
    }

    public async Task<IActionResult> Schedule(int id)
    {
        var admin = await _userManager.Users
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (admin?.ClinicId == null)
            return RedirectToAction("Index", "Admin");

        var employee = await _userManager.Users
            .Include(u => u.EmployeeType)
            .FirstOrDefaultAsync(u => u.Id == id && u.ClinicId == admin.ClinicId);

        if (employee == null)
            return NotFound();

        var schedules = await _context.WorkSchedules
            .Where(s => s.UserId == id)
            .OrderBy(s => s.Day)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        ViewBag.Employee = employee;
        ViewBag.Schedules = schedules;
        ViewBag.DayNames = DayNames;

        return View();
    }
}
