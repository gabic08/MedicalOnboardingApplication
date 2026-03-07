using MedicalOnboardingApplication.Models;
using MedicalOnboardingApplication.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MedicalOnboardingApplication.Controllers;

public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env)
    {
        _userManager = userManager;
        _env = env;
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
            ExistingProfileImagePath = user.ProfileImagePath
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UserProfile(UserProfileViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("Login", "Account");

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
        TempData["Success"] = "Profilul a fost actualizat cu succes.";
        return RedirectToAction(nameof(UserProfile));
    }
}