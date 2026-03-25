using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using MedicalOnboardingApplication.Services.Interfaces;
using MedicalOnboardingApplication.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Text;
namespace MedicalOnboardingApplication.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MedicalOnboardingApplicationContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            MedicalOnboardingApplicationContext context,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return LocalRedirect("/");
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Încearcare de autentificare invalidă.");
                return View(vm);
            }

            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError("", "Trebuie să îți confirmi contul înainte de autentificare.");
                return View(vm);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName,
                vm.Password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var clinic = await _context.Clinics
                    .FirstOrDefaultAsync(c => c.Id == user.ClinicId);

                if (clinic != null && !string.IsNullOrEmpty(clinic.Subdomain))
                {
                    var baseDomain = _configuration["AppSettings:BaseDomain"];
                    var basePort = _configuration["AppSettings:BasePort"];

                    var redirectTo = $"http://{clinic.Subdomain}.{baseDomain}:{basePort}";
                    return Redirect(redirectTo);
                }

                // Fallback — no subdomain set yet
                return LocalRedirect("/");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Cont blocat. Încearcă din nou mai târziu.");
                return View(vm);
            }

            ModelState.AddModelError("", "Încearcare de autentificare invalidă.");
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult RegisterClinicAdmin(string returnUrl = null)
        {
            ViewBag.BaseDomain = _configuration["AppSettings:BaseDomain"];
            ViewBag.BasePort = _configuration["AppSettings:BasePort"];

            return View(new RegisterAdminViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterClinicAdmin(RegisterAdminViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var existingUser = await _userManager.FindByEmailAsync(vm.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Un cont cu această adresă de email există deja.");
                return View(vm);
            }

            // Check subdomain uniqueness
            var subdomainTaken = await _context.Clinics
                .AnyAsync(c => c.Subdomain == vm.Subdomain.Trim().ToLower());

            if (subdomainTaken)
            {
                ModelState.AddModelError("Subdomain", "Acest subdomeniu este deja folosit.");
                return View(vm);
            }

            // Create clinic first
            var clinic = new Clinic
            {
                Name = vm.ClinicName.Trim(),
                Subdomain = vm.Subdomain.Trim().ToLower()
            };

            _context.Clinics.Add(clinic);
            await _context.SaveChangesAsync();

            // Create user and link to clinic
            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                ClinicId = clinic.Id
            };

            var result = await _userManager.CreateAsync(user, vm.Password);
            if (!result.Succeeded)
            {
                // Rollback clinic creation
                _context.Clinics.Remove(clinic);
                await _context.SaveChangesAsync();

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(vm);
            }

            await _userManager.AddToRoleAsync(user, "Admin");

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action(
                "ConfirmEmail",
                "Account",
                new { userId = user.Id, token },
                Request.Scheme);

            await _emailService.SendEmailAsync(
                user.Email,
                "Bun venit în MedicalOnboarding!",
                $"""
                    <h2>Bun venit, {user.FirstName} {user.LastName}!</h2>
                    <p>Clinica <strong>{clinic.Name}</strong> a fost creată cu succes.</p>
                    <p>Subdomeniul tău este: <strong>{clinic.Subdomain}.{_configuration["AppSettings:BaseDomain"]}:{_configuration["AppSettings:BasePort"]}</strong></p>
                    <p>Te poți autentifica folosind adresa de email: <strong>{user.Email}</strong> după ce confirmi adresa de email.</p>
                    <p><a href="{confirmationLink}">Confirmă contul</a></p>
                    <br/>
                    <p>Îți dorim succes!</p>
                    """
            );

            return LocalRedirect(vm.ReturnUrl ?? Url.Action("Login", "Account")!);
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.Email);

            // Always show success to avoid email enumeration
            if (user == null)
                return View("ForgotPasswordConfirmation");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            var resetLink = Url.Action(
                "ResetPassword",
                "Account",
                new { token = encodedToken, email = vm.Email },
                Request.Scheme);

            await _emailService.SendEmailAsync(
                user.Email,
                "Resetare Parolă",
                $"""
                    <h2>Resetare Parolă</h2>
                    <p>Bună, {user.FirstName} {user.LastName}!</p>
                    <p>Ai solicitat resetarea parolei. Apasă pe butonul de mai jos pentru a continua.</p>
                    <br/>
                    <a href="{resetLink}"
                       style="background-color:#0d6efd;color:white;padding:10px 20px;text-decoration:none;border-radius:5px;">
                        Resetează Parola
                    </a>
                    <br/><br/>
                    <p class="text-muted">Dacă nu ai solicitat resetarea parolei, ignoră acest email.</p>
                    """
                        );

            return View("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
                return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
                return View("ConfirmEmailSuccess");

            return View("Error");
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
                return BadRequest();

            var vm = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.Email);

            // Do not reveal if user exists
            if (user == null)
                return View("ResetPasswordConfirmation");

            var decodedToken = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(vm.Token)
            );

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, vm.Password);

            if (result.Succeeded)
                return View("ResetPasswordConfirmation");

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(vm);
        }
    }
}
