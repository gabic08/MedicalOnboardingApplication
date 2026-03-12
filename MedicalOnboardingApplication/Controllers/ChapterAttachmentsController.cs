using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Enums;
using MedicalOnboardingApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers;

[Authorize(Roles = "Admin")]
public class ChapterAttachmentsController : Controller
{
    private readonly MedicalOnboardingApplicationContext _context;
    private readonly IWebHostEnvironment _env;

    public ChapterAttachmentsController(
        MedicalOnboardingApplicationContext context,
        IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET
    public async Task<IActionResult> Create(int chapterId)
    {
        var chapter = await _context.Chapters
            .FirstOrDefaultAsync(c => c.Id == chapterId);

        if (chapter == null)
            return NotFound();

        ViewBag.ChapterId = chapterId;
        ViewBag.CourseId = chapter.CourseId;

        return View();
    }

    // POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int chapterId, IFormFile file)
    {
        var chapter = await _context.Chapters
            .FirstOrDefaultAsync(c => c.Id == chapterId);

        if (chapter == null)
            return NotFound();

        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError("", "Vă rugăm să selectați un fișier pentru încărcare.");
            ViewBag.ChapterId = chapterId;
            ViewBag.CourseId = chapter.CourseId;
            return View();
        }

        // Validate and detect type
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        var allowedTypes = new Dictionary<string, AttachmentType>
        {
            [".jpg"] = AttachmentType.Image,
            [".jpeg"] = AttachmentType.Image,
            [".png"] = AttachmentType.Image,
            [".gif"] = AttachmentType.Image,
            [".webp"] = AttachmentType.Image,

            [".mp4"] = AttachmentType.Video,
            [".webm"] = AttachmentType.Video,
            [".mov"] = AttachmentType.Video,

            [".pdf"] = AttachmentType.Pdf
        };

        if (!allowedTypes.ContainsKey(extension))
        {
            ModelState.AddModelError("", "Sunt permise doar imagini, videoclipuri și fișiere PDF.");
            ViewBag.ChapterId = chapterId;
            ViewBag.CourseId = chapter.CourseId;
            return View();
        }

        var attachmentType = allowedTypes[extension];

        // Storage path
        var uploadsRoot = Path.Combine(
            _env.WebRootPath,
            "uploads",
            "chapters",
            chapterId.ToString());

        Directory.CreateDirectory(uploadsRoot);

        // Prevent filename collisions
        var safeFileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(uploadsRoot, safeFileName);

        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var filePath = $"/uploads/chapters/{chapterId}/{safeFileName}";

        var attachment = new ChapterAttachment
        {
            ChapterId = chapterId,
            FileName = Path.GetFileName(file.FileName),
            FilePath = filePath,
            Type = attachmentType
        };

        _context.ChapterAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        return RedirectToAction("Edit", "Chapters", new { id = chapterId });
    }

    // DELETE
    public async Task<IActionResult> Delete(int id)
    {
        var attachment = await _context.ChapterAttachments
            .Include(a => a.Chapter)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attachment == null)
            return NotFound();

        return View(attachment);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var attachment = await _context.ChapterAttachments
            .Include(a => a.Chapter)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attachment == null)
            return NotFound();

        // Delete file from disk
        if (!string.IsNullOrWhiteSpace(attachment.FilePath))
        {
            var fullPath = Path.Combine(_env.WebRootPath, attachment.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        _context.ChapterAttachments.Remove(attachment);
        await _context.SaveChangesAsync();

        return RedirectToAction("Edit", "Chapters", new { id = attachment.Chapter.Id });
    }
}
