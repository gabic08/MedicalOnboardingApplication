using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using MedicalOnboardingApplication.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers;

[Authorize]
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

        var vm = new CreateChapterViewModel
        {
            CourseId = courseId,
            Order = maxOrder + 1
        };

        return View(vm);
    }

    // POST: Chapters/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateChapterViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var maxOrder = await _context.Chapters
            .Where(c => c.CourseId == vm.CourseId)
            .MaxAsync(c => (int?)c.Order) ?? 0;

        var requestedOrder = vm.Order < 1 ? 1 :
                             vm.Order > maxOrder + 1 ? maxOrder + 1 :
                             vm.Order;

        var chaptersToShift = await _context.Chapters
            .Where(c => c.CourseId == vm.CourseId &&
                        c.Order >= requestedOrder)
            .OrderBy(c => c.Order)
            .ToListAsync();

        foreach (var c in chaptersToShift)
            c.Order++;

        var chapter = new Chapter
        {
            Title = vm.Title,
            Content = vm.Content,
            Order = requestedOrder,
            CourseId = vm.CourseId
        };

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

        var vm = new EditChapterViewModel
        {
            Id = chapter.Id,
            Title = chapter.Title,
            Content = chapter.Content,
            Order = chapter.Order,
            CourseId = chapter.CourseId
        };

        ViewBag.Attachments = chapter.Attachments;

        return View(vm);
    }

    // POST: Chapters/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditChapterViewModel vm)
    {
        if (id != vm.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(vm);

        var existing = await _context.Chapters
            .FirstOrDefaultAsync(c => c.Id == id);

        if (existing == null)
            return NotFound();

        var oldOrder = existing.Order;

        var maxOrder = await _context.Chapters
            .Where(c => c.CourseId == vm.CourseId && c.Id != vm.Id)
            .MaxAsync(c => (int?)c.Order) ?? 0;

        var requestedOrder = vm.Order < 1 ? 1 :
                             vm.Order > maxOrder + 1 ? maxOrder + 1 :
                             vm.Order;

        if (requestedOrder < oldOrder)
        {
            var chaptersToShift = await _context.Chapters
                .Where(c => c.CourseId == vm.CourseId &&
                            c.Id != vm.Id &&
                            c.Order >= requestedOrder &&
                            c.Order < oldOrder)
                .ToListAsync();

            foreach (var c in chaptersToShift)
                c.Order++;
        }
        else if (requestedOrder > oldOrder)
        {
            var chaptersToShift = await _context.Chapters
                .Where(c => c.CourseId == vm.CourseId &&
                            c.Id != vm.Id &&
                            c.Order > oldOrder &&
                            c.Order <= requestedOrder)
                .ToListAsync();

            foreach (var c in chaptersToShift)
                c.Order--;
        }

        existing.Title = vm.Title;
        existing.Content = vm.Content;
        existing.Order = requestedOrder;

        await _context.SaveChangesAsync();

        return RedirectToAction("Edit", "Chapters", new { id = vm.Id });
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

        // Check if all chapters in the course are now completed
        var chapter = await _context.Chapters
            .Include(c => c.Course)
                .ThenInclude(c => c.Chapters)
            .FirstOrDefaultAsync(c => c.Id == chapterId);

        if (chapter != null)
        {
            var allChapterIds = chapter.Course.Chapters.Select(c => c.Id).ToList();

            var completedChapterIds = await _context.UserChapterProgress
                .Where(p => p.UserId == user.Id && allChapterIds.Contains(p.ChapterId))
                .Select(p => p.ChapterId)
                .ToListAsync();

            bool allCompleted = allChapterIds.All(id => completedChapterIds.Contains(id));

            if (allCompleted)
            {
                var alreadyCourseCompleted = await _context.UserCourseProgress
                    .AnyAsync(p => p.UserId == user.Id && p.CourseId == chapter.CourseId);

                if (!alreadyCourseCompleted)
                {
                    _context.UserCourseProgress.Add(new UserCourseProgress
                    {
                        UserId = user.Id,
                        CourseId = chapter.CourseId,
                        CompletedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }
            }
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

            // Also remove course completion since not all chapters are done anymore
            var chapter = await _context.Chapters.FirstOrDefaultAsync(c => c.Id == chapterId);
            if (chapter != null)
            {
                var courseProgress = await _context.UserCourseProgress
                    .FirstOrDefaultAsync(p => p.UserId == user.Id && p.CourseId == chapter.CourseId);

                if (courseProgress != null)
                    _context.UserCourseProgress.Remove(courseProgress);
            }

            await _context.SaveChangesAsync();
        }

        return Ok();
    }
}
