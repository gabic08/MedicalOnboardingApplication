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
            return View(vm);
        }

        // Update names
        user.FirstName = vm.FirstName;
        user.LastName = vm.LastName;

        // Handle image upload
        if (vm.NewProfileImage != null && vm.NewProfileImage.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(vm.NewProfileImage.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("NewProfileImage", "Only image files are allowed.");
                vm.ExistingProfileImagePath = user.ProfileImagePath;
                return View(vm);
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

        TempData["Success"] = "Profile updated successfully.";

        return RedirectToAction(nameof(UserProfile));
    }
}
