using Microsoft.AspNetCore.Mvc;

namespace MedicalOnboardingApplication.Controllers;

public class AdminController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
