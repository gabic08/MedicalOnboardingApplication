using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers;

public class ChaptersController : Controller
{
    private readonly MedicalOnboardingApplicationContext _context;

    public ChaptersController(MedicalOnboardingApplicationContext context)
    {
        _context = context;
    }

    // GET: Chapters/Create?courseId=1
    public async Task<IActionResult> Create(int courseId)
    {
        var maxOrder = await _context.Chapters
            .Where(c => c.CourseId == courseId)
            .MaxAsync(c => (int?)c.Order) ?? 0;

        var chapter = new Chapter
        {
            CourseId = courseId,
            Order = maxOrder + 1
        };

        return View(chapter);
    }

    // POST: Chapters/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Content,Order,CourseId")] Chapter chapter)
    {
        if (!ModelState.IsValid)
            return View(chapter);

        // Get max order for this course
        var maxOrder = await _context.Chapters
            .Where(c => c.CourseId == chapter.CourseId)
            .MaxAsync(c => (int?)c.Order) ?? 0;

        // Normalize order
        var requestedOrder = chapter.Order < 1 ? 1 :
                             chapter.Order > maxOrder + 1 ? maxOrder + 1 :
                             chapter.Order;

        // Shift chapters DOWN
        var chaptersToShift = await _context.Chapters
            .Where(c => c.CourseId == chapter.CourseId &&
                        c.Order >= requestedOrder)
            .OrderBy(c => c.Order)
            .ToListAsync();

        foreach (var c in chaptersToShift)
        {
            c.Order++;
        }

        chapter.Order = requestedOrder;

        _context.Chapters.Add(chapter);
        await _context.SaveChangesAsync();

        return RedirectToAction("Edit", "Chapters", new { id = chapter.Id });

    }

    // GET: Chapters/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var chapter = await _context.Chapters
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (chapter == null)
            return NotFound();

        return View(chapter);
    }

    // POST: Chapters/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,Order,CourseId")] Chapter chapter)
    {
        if (id != chapter.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(chapter);

        var existing = await _context.Chapters
            .FirstOrDefaultAsync(c => c.Id == id);

        if (existing == null)
            return NotFound();

        var oldOrder = existing.Order;

        var maxOrder = await _context.Chapters
            .Where(c => c.CourseId == chapter.CourseId &&
                        c.Id != chapter.Id)
            .MaxAsync(c => (int?)c.Order) ?? 0;

        // Normalize order
        var requestedOrder = chapter.Order < 1 ? 1 :
                             chapter.Order > maxOrder + 1 ? maxOrder + 1 :
                             chapter.Order;

        // SHIFT LOGIC
        if (requestedOrder < oldOrder)
        {
            // Moving UP → push others down
            var chaptersToShift = await _context.Chapters
                .Where(c => c.CourseId == chapter.CourseId &&
                            c.Id != chapter.Id &&
                            c.Order >= requestedOrder &&
                            c.Order < oldOrder)
                .ToListAsync();

            foreach (var c in chaptersToShift)
            {
                c.Order++;
            }
        }
        else if (requestedOrder > oldOrder)
        {
            // Moving DOWN → pull others up
            var chaptersToShift = await _context.Chapters
                .Where(c => c.CourseId == chapter.CourseId &&
                            c.Id != chapter.Id &&
                            c.Order > oldOrder &&
                            c.Order <= requestedOrder)
                .ToListAsync();

            foreach (var c in chaptersToShift)
            {
                c.Order--;
            }
        }

        // Update chapter
        existing.Title = chapter.Title;
        existing.Content = chapter.Content;
        existing.Order = requestedOrder;

        await _context.SaveChangesAsync();

        return RedirectToAction("Edit", "Chapters", new { id = chapter.Id });
    }

    // GET: Chapters/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var chapter = await _context.Chapters
            .FirstOrDefaultAsync(c => c.Id == id);

        if (chapter == null)
            return NotFound();

        return View(chapter);
    }

    // POST: Chapters/DeleteConfirmed/5
    [HttpPost, ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var chapter = await _context.Chapters
            .FirstOrDefaultAsync(c => c.Id == id);

        if (chapter == null)
            return NotFound();

        var deletedOrder = chapter.Order;
        var courseId = chapter.CourseId;

        _context.Chapters.Remove(chapter);

        // Shift everything ABOVE it UP
        var chaptersToShift = await _context.Chapters
            .Where(c => c.CourseId == courseId &&
                        c.Order > deletedOrder)
            .OrderBy(c => c.Order)
            .ToListAsync();

        foreach (var c in chaptersToShift)
        {
            c.Order--;
        }

        await _context.SaveChangesAsync();

        return RedirectToAction("Manage", "AdminCourses", new { id = courseId });
    }

    // GET: Chapters/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var chapter = await _context.Chapters
            .Include(c => c.Attachments)
            .Include(c => c.Course)
                .ThenInclude(c => c.Chapters)
                    .ThenInclude(ch => ch.Progress)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (chapter == null)
            return NotFound();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        ViewBag.IsAdmin = User.IsInRole("Admin");
        ViewBag.CompletedChapterIds = await _context.UserChapterProgress
            .Where(p => p.UserId == user.Id)
            .Select(p => p.ChapterId)
            .ToListAsync();

        return View(chapter);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkComplete(int chapterId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (user == null)
            return Unauthorized();

        var alreadyCompleted = await _context.UserChapterProgress
            .AnyAsync(p => p.UserId == user.Id && p.ChapterId == chapterId);

        if (!alreadyCompleted)
        {
            _context.UserChapterProgress.Add(new UserChapterProgress
            {
                UserId = user.Id,
                ChapterId = chapterId,
                CompletedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnmarkComplete(int chapterId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        if (user == null)
            return Unauthorized();

        var progress = await _context.UserChapterProgress
            .FirstOrDefaultAsync(p => p.UserId == user.Id && p.ChapterId == chapterId);

        if (progress != null)
        {
            _context.UserChapterProgress.Remove(progress);
            await _context.SaveChangesAsync();
        }

        return Ok();
    }
}
