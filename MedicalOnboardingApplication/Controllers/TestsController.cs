using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Controllers;

[Authorize]
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

    // GET: Tests/Index
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Index", "Admin");
        }

        var user = await _context.Users
            .Include(u => u.EmployeeType)
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        var sessions = await _context.TestSessions
            .Where(s => s.UserId == user.Id)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync();

        var questionCount = await _context.Questions
            .Where(q => q.Course.CourseEmployeeTypes
                .Any(cet => cet.EmployeeTypeId == user.EmployeeTypeId))
            .CountAsync();

        var activeSession = sessions.FirstOrDefault(s => !s.IsCompleted);

        ViewBag.CanStartTest = questionCount >= TotalQuestions && activeSession == null;
        ViewBag.QuestionCount = questionCount;
        ViewBag.ActiveSessionId = activeSession?.Id;

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

        var activeSession = await _context.TestSessions
            .FirstOrDefaultAsync(s => s.UserId == user.Id && !s.IsCompleted);

        if (activeSession != null)
        {
            TempData["Error"] = "Ai un test nefinalizat. Te rugăm să îl completezi înainte de a începe unul nou.";
            return RedirectToAction(nameof(Index));
        }

        var allQuestions = await _context.Questions
            .Include(q => q.Answers)
            .Where(q => q.Course.CourseEmployeeTypes
                .Any(cet => cet.EmployeeTypeId == user.EmployeeTypeId))
            .ToListAsync();

        if (allQuestions.Count < TotalQuestions)
        {
            TempData["Error"] = "Nu sunt suficiente întrebări disponibile pentru a începe un test.";
            return RedirectToAction(nameof(Index));
        }

        // Get starting difficulty from last completed session
        var lastSession = await _context.TestSessions
            .Where(s => s.UserId == user.Id && s.IsCompleted)
            .OrderByDescending(s => s.CompletedAt)
            .FirstOrDefaultAsync();

        int startingDifficulty = MinDifficulty;
        if (lastSession != null)
        {
            startingDifficulty = Math.Clamp((int)Math.Round(lastSession.AverageDifficulty), MinDifficulty, MaxDifficulty);
        }

        var random = new Random();
        var selectedQuestions = allQuestions
            .OrderBy(_ => random.Next())
            .Take(TotalQuestions)
            .ToList();

        var session = new TestSession
        {
            UserId = user.Id,
            StartedAt = DateTime.UtcNow,
            TotalQuestions = TotalQuestions
        };

        _context.TestSessions.Add(session);
        await _context.SaveChangesAsync();

        for (int i = 0; i < selectedQuestions.Count; i++)
        {
            _context.TestSessionQuestions.Add(new TestSessionQuestion
            {
                TestSessionId = session.Id,
                QuestionId = selectedQuestions[i].Id,
                Order = i + 1,
                DifficultyAtTime = startingDifficulty
            });
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Question), new { sessionId = session.Id });
    }

    // GET: Tests/Question?sessionId=1
    public async Task<IActionResult> Question(int sessionId, int? answeredQuestionId = null)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        var session = await _context.TestSessions
            .Include(s => s.Questions)
                .ThenInclude(q => q.Question)
                    .ThenInclude(q => q.Answers)
            .Include(s => s.Questions)
                .ThenInclude(q => q.Question)
                    .ThenInclude(q => q.Course)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == user.Id);

        if (session == null) return RedirectToAction(nameof(Index));
        if (session.IsCompleted) return RedirectToAction(nameof(Results), new { sessionId });

        var answeredCount = session.Questions.Count(q => q.IsCorrect != null);

        // Show feedback for the just-answered question
        if (answeredQuestionId.HasValue)
        {
            var answered = session.Questions.FirstOrDefault(q => q.Id == answeredQuestionId.Value);
            if (answered != null)
            {
                // Restore saved answer order
                if (!string.IsNullOrEmpty(answered.AnswerOrder))
                {
                    var orderedAnswers = answered.AnswerOrder
                        .Split(',')
                        .Select(id => answered.Question.Answers.FirstOrDefault(a => a.Id == int.Parse(id)))
                        .Where(a => a != null)
                        .ToList();
                    answered.Question.Answers = orderedAnswers;
                }

                var answeredSoFar = session.Questions
                    .Where(q => q.IsCorrect != null)
                    .OrderBy(q => q.Order)
                    .ToList();

                var lastWindow = answeredSoFar
                    .TakeLast(WindowSize)
                    .Select(q => q.IsCorrect == true ? 1.0 : 0.0)
                    .ToList();

                double? windowAvg = lastWindow.Count == WindowSize ? lastWindow.Average() : null;

                int nextDiff = answered.DifficultyAtTime;
                if (windowAvg.HasValue)
                {
                    if (windowAvg.Value > IncreaseThreshold)
                        nextDiff = Math.Min(answered.DifficultyAtTime + 1, MaxDifficulty);
                    else if (windowAvg.Value < DecreaseThreshold)
                        nextDiff = Math.Max(answered.DifficultyAtTime - 1, MinDifficulty);
                }

                bool allAnswered = session.Questions.All(q => q.IsCorrect != null);

                ViewBag.ShowFeedback = true;
                ViewBag.IsCorrect = answered.IsCorrect;
                ViewBag.CorrectAnswerId = answered.Question.Answers.FirstOrDefault(a => a.IsCorrect)?.Id;
                ViewBag.SelectedAnswerId = answered.SelectedAnswerId;
                ViewBag.WindowAvg = windowAvg;
                ViewBag.LastWindow = lastWindow;
                ViewBag.WindowSize = WindowSize;
                ViewBag.IncreaseThreshold = IncreaseThreshold;
                ViewBag.DecreaseThreshold = DecreaseThreshold;
                ViewBag.NextDifficulty = nextDiff;
                ViewBag.IsLastQuestion = allAnswered;
                ViewBag.AnsweredCount = answeredCount;
                ViewBag.TotalQuestions = TotalQuestions;
                ViewBag.Progress = (int)Math.Round((double)answeredCount / TotalQuestions * 100);
                ViewBag.CurrentDifficulty = answered.DifficultyAtTime;
                ViewBag.SessionId = sessionId;

                return View(answered);
            }
        }

        // Show next unanswered question
        var nextQuestion = session.Questions
            .OrderBy(q => q.Order)
            .FirstOrDefault(q => q.IsCorrect == null);

        if (nextQuestion == null)
            return RedirectToAction(nameof(Complete), new { sessionId });

        // Shuffle and save order if not already saved
        if (string.IsNullOrEmpty(nextQuestion.AnswerOrder))
        {
            var shuffled = nextQuestion.Question.Answers
                .OrderBy(_ => Guid.NewGuid())
                .ToList();
            nextQuestion.AnswerOrder = string.Join(",", shuffled.Select(a => a.Id));
            await _context.SaveChangesAsync();
        }

        // Apply saved order
        nextQuestion.Question.Answers = nextQuestion.AnswerOrder
            .Split(',')
            .Select(id => nextQuestion.Question.Answers.FirstOrDefault(a => a.Id == int.Parse(id)))
            .Where(a => a != null)
            .ToList();

        ViewBag.ShowFeedback = false;
        ViewBag.AnsweredCount = answeredCount;
        ViewBag.TotalQuestions = TotalQuestions;
        ViewBag.Progress = (int)Math.Round((double)answeredCount / TotalQuestions * 100);
        ViewBag.CurrentDifficulty = nextQuestion.DifficultyAtTime;
        ViewBag.SessionId = sessionId;

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

        if (sessionQuestion == null || sessionQuestion.IsCorrect != null)
            return RedirectToAction(nameof(Question), new { sessionId });

        var selectedAnswer = sessionQuestion.Question.Answers
            .FirstOrDefault(a => a.Id == answerId);

        sessionQuestion.SelectedAnswerId = answerId;
        sessionQuestion.IsCorrect = selectedAnswer?.IsCorrect ?? false;

        await _context.SaveChangesAsync();

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

        var nextQuestion = session.Questions
            .OrderBy(q => q.Order)
            .FirstOrDefault(q => q.IsCorrect == null);

        if (nextQuestion != null)
            nextQuestion.DifficultyAtTime = newDifficulty;

        await _context.SaveChangesAsync();

        // Redirect back to Question with feedback
        return RedirectToAction(nameof(Question), new { sessionId, answeredQuestionId = sessionQuestionId });
    }

    // GET: Tests/Complete
    public async Task<IActionResult> Complete(int sessionId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        var session = await _context.TestSessions
            .Include(s => s.Questions)
                .ThenInclude(q => q.Question)
                    .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == user.Id);

        if (session == null)
            return RedirectToAction(nameof(Index));

        if (!session.IsCompleted)
        {
            session.IsCompleted = true;
            session.CompletedAt = DateTime.UtcNow;
            session.CorrectAnswers = session.Questions.Count(q => q.IsCorrect == true);
            session.AverageDifficulty = session.Questions.Average(q => q.DifficultyAtTime);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Results), new { sessionId });
    }

    // GET: Tests/Results
    public async Task<IActionResult> Results(int sessionId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        var session = await _context.TestSessions
            .Include(s => s.Questions.OrderBy(q => q.Order))
                .ThenInclude(q => q.Question)
                    .ThenInclude(q => q.Answers)
            .Include(s => s.Questions)
                .ThenInclude(q => q.SelectedAnswer)
            .Include(s => s.Questions)
                .ThenInclude(q => q.Question)
                    .ThenInclude(q => q.Course)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == user.Id);

        if (session == null || !session.IsCompleted)
            return RedirectToAction(nameof(Index));

        // Restore saved answer order for each question
        foreach (var sq in session.Questions)
        {
            if (!string.IsNullOrEmpty(sq.AnswerOrder))
            {
                sq.Question.Answers = sq.AnswerOrder
                    .Split(',')
                    .Select(id => sq.Question.Answers.FirstOrDefault(a => a.Id == int.Parse(id)))
                    .Where(a => a != null)
                    .ToList();
            }
        }

        return View(session);
    }

    public async Task<IActionResult> Feedback(int sessionId, int sessionQuestionId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

        var session = await _context.TestSessions
            .Include(s => s.Questions)
                .ThenInclude(q => q.Question)
                    .ThenInclude(q => q.Answers)
            .Include(s => s.Questions)
                .ThenInclude(q => q.Question)
                    .ThenInclude(q => q.Course)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == user.Id);

        if (session == null)
            return RedirectToAction(nameof(Index));

        var sessionQuestion = session.Questions
            .FirstOrDefault(q => q.Id == sessionQuestionId);

        if (sessionQuestion == null)
            return RedirectToAction(nameof(Index));

        var answeredSoFar = session.Questions
            .Where(q => q.IsCorrect != null)
            .OrderBy(q => q.Order)
            .ToList();

        var lastWindow = answeredSoFar
            .TakeLast(WindowSize)
            .Select(q => q.IsCorrect == true ? 1.0 : 0.0)
            .ToList();

        double? windowAvg = lastWindow.Count == WindowSize ? lastWindow.Average() : null;

        bool allAnswered = session.Questions.All(q => q.IsCorrect != null);

        ViewBag.SessionId = sessionId;
        ViewBag.WindowAvg = windowAvg;
        ViewBag.WindowSize = WindowSize;
        ViewBag.IncreaseThreshold = IncreaseThreshold;
        ViewBag.DecreaseThreshold = DecreaseThreshold;
        ViewBag.LastWindow = lastWindow;
        ViewBag.IsLastQuestion = allAnswered;
        ViewBag.AnsweredCount = answeredSoFar.Count;
        ViewBag.TotalQuestions = TotalQuestions;

        return View(sessionQuestion);
    }
}