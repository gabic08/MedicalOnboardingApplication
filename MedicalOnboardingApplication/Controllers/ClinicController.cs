using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers;

[Authorize(Roles = "Admin")]
public class ClinicController : Controller
{
    private readonly MedicalOnboardingApplicationContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public ClinicController(
        MedicalOnboardingApplicationContext context,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env)
    {
        _context = context;
        _userManager = userManager;
        _env = env;
    }

    // ===============================
    // GET: Clinic/Details
    // ===============================
    public async Task<IActionResult> Details()
    {
        var user = await _userManager.Users
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (user == null)
            return NotFound();

        ViewBag.AutoEdit = user.Clinic == null;

        return View(user.Clinic ?? new Clinic());
    }

    // ===============================
    // POST: Clinic/Edit (Save)
    // ===============================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Clinic clinic, IFormFile image)
    {
        var user = await _userManager.Users
            .Include(u => u.Clinic)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (user == null)
            return NotFound();

        if (!ModelState.IsValid)
            return View("Details", clinic);

        if (string.IsNullOrWhiteSpace(clinic.Subdomain))
        {
            ModelState.AddModelError("", "Subdomeniul este obligatoriu.");
            return View("Details", clinic);
        }

        // Handle image upload
        if (image != null && image.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(image.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError("", "Sunt permise doar fișiere imagine.");
                return View("Details", clinic);
            }

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "clinics");
            Directory.CreateDirectory(uploadsFolder);

            // Delete old image if exists
            if (!string.IsNullOrEmpty(user.Clinic?.ImagePath))
            {
                var oldPath = Path.Combine(_env.WebRootPath,
                    user.Clinic.ImagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(stream);

            clinic.ImagePath = $"/uploads/clinics/{fileName}";
        }
        else
        {
            // Keep existing image
            clinic.ImagePath = user.Clinic?.ImagePath;
        }

        clinic.Subdomain = clinic.Subdomain.Trim().ToLower();

        var subdomainTaken = await _context.Clinics
            .AnyAsync(c => c.Id != clinic.Id && c.Subdomain == clinic.Subdomain);

        if (subdomainTaken)
        {
            ModelState.AddModelError("Subdomain", "Acest subdomeniu este deja folosit.");
            return View("Details", clinic);
        }


        if (user.ClinicId == null)
        {
            _context.Clinics.Add(clinic);
            await _context.SaveChangesAsync();
            user.ClinicId = clinic.Id;
            await _userManager.UpdateAsync(user);
        }
        else
        {
            var existingClinic = await _context.Clinics
                .FirstOrDefaultAsync(c => c.Id == user.ClinicId);

            if (existingClinic == null)
                return NotFound();

            existingClinic.Name = clinic.Name;
            existingClinic.Address = clinic.Address;
            existingClinic.Phone = clinic.Phone;
            existingClinic.Email = clinic.Email;
            existingClinic.Website = clinic.Website;
            existingClinic.City = clinic.City;
            existingClinic.Country = clinic.Country;
            existingClinic.ImagePath = clinic.ImagePath;
            existingClinic.Subdomain = clinic.Subdomain;
            await _context.SaveChangesAsync();
        }

        TempData["Success"] = "Informațiile clinicii au fost salvate cu succes.";
        return RedirectToAction(nameof(Details));
    }
}
