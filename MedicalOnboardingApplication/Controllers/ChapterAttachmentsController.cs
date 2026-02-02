using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers;

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
        ViewBag.ChapterId = chapterId;

        var courseId = (await _context.Chapters.FirstOrDefaultAsync(c => c.Id == chapterId))?.CourseId;
        ViewBag.CourseId = courseId;

        return View();
    }

    // POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        int chapterId,
        AttachmentType type,
        IFormFile file,
        string url)
    {
        var chapter = await _context.Chapters.FindAsync(chapterId);
        if (chapter == null)
            return NotFound();

        if (type == AttachmentType.Link && string.IsNullOrWhiteSpace(url))
        {
            ModelState.AddModelError("", "URL is required for link attachments");
            ViewBag.ChapterId = chapterId;
            return View();
        }

        if (type != AttachmentType.Link && (file == null || file.Length == 0))
        {
            ModelState.AddModelError("", "Please select a file to upload");
            ViewBag.ChapterId = chapterId;
            return View();
        }

        string filePath = null;
        string fileName = null;

        if (file != null)
        {
            var uploadsRoot = Path.Combine(
                _env.WebRootPath,
                "uploads",
                "chapters",
                chapterId.ToString());

            Directory.CreateDirectory(uploadsRoot);

            fileName = Path.GetFileName(file.FileName);
            var fullPath = Path.Combine(uploadsRoot, fileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            filePath = $"/uploads/chapters/{chapterId}/{fileName}";
        }

        var attachment = new ChapterAttachment
        {
            ChapterId = chapterId,
            FileName = fileName,
            FilePath = filePath,
            Url = url,
            Type = type
        };

        _context.ChapterAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        return RedirectToAction("Manage", "AdminCourses", new { id = chapter.CourseId });
    }
}