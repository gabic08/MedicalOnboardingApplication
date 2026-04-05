using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using MedicalOnboardingApplication.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IWebHostEnvironment _env;
    private readonly MedicalOnboardingApplicationContext _context;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IWebHostEnvironment env,
        MedicalOnboardingApplicationContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _env = env;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> UserProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Account");

        var vm = new UserProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            ExistingProfileImagePath = user.ProfileImagePath
        };


        var clinic = await _context.Clinics
            .FirstOrDefaultAsync(c => c.Id == user.ClinicId);

        ViewBag.Clinic = clinic;
        ViewBag.IsEmployee = await _userManager.IsInRoleAsync(user, "Employee");

        // Load schedule for employees
        if (await _userManager.IsInRoleAsync(user, "Employee"))
        {
            var schedules = await _context.WorkSchedules
                .Where(s => s.UserId == user.Id)
                .OrderBy(s => s.Day)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            ViewBag.Schedules = schedules;
            ViewBag.DayNames = new Dictionary<DayOfWeek, string>
        {
            { DayOfWeek.Monday,    "Luni" },
            { DayOfWeek.Tuesday,   "Marți" },
            { DayOfWeek.Wednesday, "Miercuri" },
            { DayOfWeek.Thursday,  "Joi" },
            { DayOfWeek.Friday,    "Vineri" },
            { DayOfWeek.Saturday,  "Sâmbătă" },
            { DayOfWeek.Sunday,    "Duminică" }
        };
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserProfile(UserProfileViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Account");

        // Check if email is taken by another user
        if (vm.Email != user.Email)
        {
            var existingUser = await _userManager.FindByEmailAsync(vm.Email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError("Email", "Această adresă de email este deja folosită.");
            }
        }

        if (!ModelState.IsValid)
        {
            vm.ExistingProfileImagePath = user.ProfileImagePath;
            vm.CurrentPassword = null;
            vm.NewPassword = null;
            vm.ConfirmNewPassword = null;
            return View(vm);
        }

        // Update names
        user.FirstName = vm.FirstName;
        user.LastName = vm.LastName;

        bool emailChanged = vm.Email != user.Email;
        if (emailChanged)
        {
            user.Email = vm.Email;
            user.UserName = vm.Email;
            user.NormalizedEmail = vm.Email.ToUpper();
            user.NormalizedUserName = vm.Email.ToUpper();
        }


        // Handle image removal
        if (vm.RemoveProfileImage)
        {
            // Delete old file from disk if it exists
            if (!string.IsNullOrEmpty(user.ProfileImagePath))
            {
                var oldPath = Path.Combine(_env.WebRootPath,
                    user.ProfileImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }
            user.ProfileImagePath = null;
        }
        // Handle image upload (only if not removing)
        else if (vm.NewProfileImage != null && vm.NewProfileImage.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(vm.NewProfileImage.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("NewProfileImage", "Sunt permise doar fișiere imagine.");
                vm.ExistingProfileImagePath = user.ProfileImagePath;
                return View(vm);
            }

            // Delete old file from disk if it exists
            if (!string.IsNullOrEmpty(user.ProfileImagePath))
            {
                var oldPath = Path.Combine(_env.WebRootPath,
                    user.ProfileImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await vm.NewProfileImage.CopyToAsync(stream);

            user.ProfileImagePath = $"/uploads/profiles/{fileName}";
        }

        // Handle password change (only if provided)
        if (!string.IsNullOrWhiteSpace(vm.CurrentPassword) &&
            !string.IsNullOrWhiteSpace(vm.NewPassword))
        {
            var passwordResult = await _userManager.ChangePasswordAsync(
                user,
                vm.CurrentPassword,
                vm.NewPassword);

            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                    ModelState.AddModelError("", error.Description);

                vm.ExistingProfileImagePath = user.ProfileImagePath;
                return View(vm);
            }
        }

        await _userManager.UpdateAsync(user);
        if (emailChanged)
        {
            await _signInManager.RefreshSignInAsync(user);
        }

        TempData["Success"] = "Profilul a fost actualizat cu succes.";
        return RedirectToAction(nameof(UserProfile));
    }
}