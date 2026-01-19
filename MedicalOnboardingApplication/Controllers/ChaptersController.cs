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

    public async Task<IActionResult> Index(int courseId)
    {
        var chapters = await _context.Chapters
            .Where(c => c.CourseId == courseId)
            .OrderBy(c => c.Order)
            .ToListAsync();

        ViewBag.CourseId = courseId;
        return View(chapters);
    }

    // GET: Chapters/Create?courseId=1
    public IActionResult Create(int courseId)
    {
        var chapter = new Chapter
        {
            CourseId = courseId
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

        _context.Chapters.Add(chapter);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Courses", new { id = chapter.CourseId });
    }

    // GET: Chapters/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var chapter = await _context.Chapters.FindAsync(id);
        if (chapter == null) return NotFound();

        return View(chapter);
    }

    // POST: Chapters/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,Order,CourseId")] Chapter chapter)
    {
        if (id != chapter.Id) return NotFound();

        if (!ModelState.IsValid)
            return View(chapter);

        try
        {
            _context.Update(chapter);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ChapterExists(chapter.Id))
                return NotFound();
            else
                throw;
        }

        return RedirectToAction("Details", "Courses", new { id = chapter.CourseId });
    }

    // GET: Chapters/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var chapter = await _context.Chapters
            .FirstOrDefaultAsync(c => c.Id == id);

        if (chapter == null) return NotFound();

        return View(chapter);
    }

    // POST: Chapters/DeleteConfirmed/5
    [HttpPost, ActionName("DeleteConfirmed")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var chapter = await _context.Chapters.FindAsync(id);
        if (chapter != null)
        {
            _context.Chapters.Remove(chapter);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Courses", new { id = chapter.CourseId });
        }

        return NotFound();
    }

    private bool ChapterExists(int id)
    {
        return _context.Chapters.Any(c => c.Id == id);
    }
}