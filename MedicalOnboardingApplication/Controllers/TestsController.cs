using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace MedicalOnboardingApplication.Controllers;

[Authorize(Roles = "Employee")]
public class TestsController : Controller
{
    private readonly MedicalOnboardingApplicationContext _context;
    private const int TotalQuestions = 15;
    private const int WindowSize = 3;
    private const double IncreaseThreshold = 0.8;
    private const double DecreaseThreshold = 0.5;
    private const int MinDifficulty = 1;
    private const int MaxDifficulty = 5;

    public TestsController(MedicalOnboardingApplicationContext context)
    {
        _context = context;
    }

    // GET: Tests/Index — show history and start button
    public async Task<IActionResult> Index()
    {
        var user = await _context.Users
            .Include(u => u.EmployeeType)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        var sessions = await _context.TestSessions
            .Where(s => s.UserId == user.Id)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();

        // Check if enough questions exist
        var questionCount = await _context.Questions
            .Where(q => q.Course.CourseEmployeeTypes
                .Any(cet => cet.EmployeeTypeId == user.EmployeeTypeId))
            .CountAsync();

        ViewBag.CanStartTest = questionCount >= TotalQuestions;
        ViewBag.QuestionCount = questionCount;

        return View(sessions);
    }

    // POST: Tests/Start
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start()
    {
        var user = await _context.Users
            .Include(u => u.EmployeeType)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        // Get all eligible questions
        var allQuestions = await _context.Questions
            .Include(q => q.Answers)
            .Where(q => q.Course.CourseEmployeeTypes
                .Any(cet => cet.EmployeeTypeId == user.EmployeeTypeId))
            .ToListAsync();

        if (allQuestions.Count < TotalQuestions)
        {
            TempData["Error"] = "Not enough questions available to start a test.";
            return RedirectToAction(nameof(Index));
        }

        // Pick 15 random questions
        var random = new Random();
        var selectedQuestions = allQuestions
            .OrderBy(_ => random.Next())
            .Take(TotalQuestions)
            .ToList();

        var session = new TestSession
        {
            UserId = user.Id,
            StartedAt = DateTime.UtcNow,
            TotalQuestions = TotalQuestions,
            FinalDifficulty = MinDifficulty
        };

        _context.TestSessions.Add(session);
        await _context.SaveChangesAsync();

        // Add questions to session in order
        for (int i = 0; i < selectedQuestions.Count; i++)
        {
            _context.TestSessionQuestions.Add(new TestSessionQuestion
            {
                TestSessionId = session.Id,
                QuestionId = selectedQuestions[i].Id,
                Order = i + 1,
                DifficultyAtTime = MinDifficulty
            });
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Question), new { sessionId = session.Id });
    }

    // GET: Tests/Question?sessionId=1
    public async Task<IActionResult> Question(int sessionId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        var session = await _context.TestSessions
            .Include(s => s.Questions)
                .ThenInclude(q => q.Question)
                    .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == user.Id);

        if (session == null || session.IsCompleted)
            return RedirectToAction(nameof(Index));

        // Find next unanswered question
        var nextQuestion = session.Questions
            .OrderBy(q => q.Order)
            .FirstOrDefault(q => q.IsCorrect == null);

        if (nextQuestion == null)
            return RedirectToAction(nameof(Complete), new { sessionId });

        var answeredCount = session.Questions.Count(q => q.IsCorrect != null);

        ViewBag.AnsweredCount = answeredCount;
        ViewBag.TotalQuestions = TotalQuestions;
        ViewBag.Progress = (int)Math.Round((double)answeredCount / TotalQuestions * 100);
        ViewBag.CurrentDifficulty = nextQuestion.DifficultyAtTime;

        return View(nextQuestion);
    }

    // POST: Tests/Answer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Answer(int sessionId, int sessionQuestionId, int answerId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        var session = await _context.TestSessions
            .Include(s => s.Questions)
                .ThenInclude(q => q.Question)
                    .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == user.Id);

        if (session == null || session.IsCompleted)
            return RedirectToAction(nameof(Index));

        var sessionQuestion = session.Questions
            .FirstOrDefault(q => q.Id == sessionQuestionId);

        if (sessionQuestion == null)
            return RedirectToAction(nameof(Index));

        // Record the answer
        var selectedAnswer = sessionQuestion.Question.Answers
            .FirstOrDefault(a => a.Id == answerId);

        sessionQuestion.SelectedAnswerId = answerId;
        sessionQuestion.IsCorrect = selectedAnswer?.IsCorrect ?? false;

        // Compute new difficulty based on last 3 answers
        var answeredSoFar = session.Questions
            .Where(q => q.IsCorrect != null)
            .OrderBy(q => q.Order)
            .ToList();

        var lastWindow = answeredSoFar
            .TakeLast(WindowSize)
            .Select(q => q.IsCorrect == true ? 1.0 : 0.0)
            .ToList();

        int currentDifficulty = sessionQuestion.DifficultyAtTime;
        int newDifficulty = currentDifficulty;

        if (lastWindow.Count == WindowSize)
        {
            double avg = lastWindow.Average();
            if (avg > IncreaseThreshold)
                newDifficulty = Math.Min(currentDifficulty + 1, MaxDifficulty);
            else if (avg < DecreaseThreshold)
                newDifficulty = Math.Max(currentDifficulty - 1, MinDifficulty);
        }

        // Set difficulty on next unanswered question
        var nextQuestion = session.Questions
            .OrderBy(q => q.Order)
            .FirstOrDefault(q => q.IsCorrect == null && q.Id != sessionQuestionId);

        if (nextQuestion != null)
            nextQuestion.DifficultyAtTime = newDifficulty;

        session.FinalDifficulty = newDifficulty;

        await _context.SaveChangesAsync();

        // Check if test is complete
        bool allAnswered = session.Questions.All(q => q.IsCorrect != null);
        if (allAnswered)
            return RedirectToAction(nameof(Complete), new { sessionId });

        return RedirectToAction(nameof(Question), new { sessionId });
    }

    // GET: Tests/Complete?sessionId=1
    public async Task<IActionResult> Complete(int sessionId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        var session = await _context.TestSessions
            .Include(s => s.Questions)
                .ThenInclude(q => q.Question)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == user.Id);

        if (session == null)
            return RedirectToAction(nameof(Index));

        if (!session.IsCompleted)
        {
            session.IsCompleted = true;
            session.CompletedAt = DateTime.UtcNow;
            session.CorrectAnswers = session.Questions.Count(q => q.IsCorrect == true);
            await _context.SaveChangesAsync();
        }

        return View(session);
    }
}