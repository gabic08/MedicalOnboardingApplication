using MedicalOnboardingApplication.Data;
using MedicalOnboardingApplication.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MedicalOnboardingApplication.Middleware;

public class SubdomainMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _baseDomain;
    private readonly IConfiguration _configuration;

    public SubdomainMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
        _baseDomain = configuration["AppSettings:BaseDomain"]!;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host.ToLower();
        var path = context.Request.Path.Value?.ToLower();
        var port = _configuration["AppSettings:BasePort"];

        string subdomain = null;

        if (host.EndsWith($".{_baseDomain}"))
        {
            subdomain = host[..^(_baseDomain.Length + 1)];
        }

        context.Items["Subdomain"] = subdomain;

        // If on a subdomain, verify it exists in the database
        if (!string.IsNullOrEmpty(subdomain))
        {
            using var scope = context.RequestServices.CreateScope();
            var dbContext = scope.ServiceProvider
                .GetRequiredService<MedicalOnboardingApplicationContext>();

            var clinic = await dbContext.Clinics
                .FirstOrDefaultAsync(c => c.Subdomain == subdomain);

            if (clinic == null)
            {
                context.Response.Redirect($"http://{_baseDomain}:{port}");
                return;
            }

            // If on a subdomain and trying to access auth routes, redirect to main domain
            var authPaths = new[]
            {
                "/account/login",
                "/account/register",
                "/account/registerclinicadmin",
                "/account/forgotpassword",
                "/account/resetpassword",
                "/account/confirmemail"
            };

            if (authPaths.Any(p => path?.StartsWith(p) == true))
            {
                context.Response.Redirect($"http://{_baseDomain}:{port}{context.Request.Path}{context.Request.QueryString}");
                return;
            }

            // If authenticated, verify user belongs to this subdomain's clinic
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userName = context.User.Identity.Name;
                var user = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.UserName == userName);

                if (user == null || user.ClinicId != clinic.Id)
                {
                    var signInManager = context.RequestServices
                        .GetRequiredService<SignInManager<ApplicationUser>>();
                    await signInManager.SignOutAsync();

                    context.Response.Redirect($"http://{_baseDomain}:{port}/Account/Login");
                    return;
                }
            }
        }

        // If on main domain and trying to access anything except account routes
        if (string.IsNullOrEmpty(subdomain))
        {
            var accountPaths = new[]
            {
                "/account",
                "/home"
            };

            bool isAccountPath = accountPaths.Any(p => path?.StartsWith(p) == true);
            bool isRootPath = path == "/" || string.IsNullOrEmpty(path);
            bool isStaticFile = path?.StartsWith("/_framework") == true ||
                    path?.StartsWith("/css") == true ||
                    path?.StartsWith("/js") == true ||
                    path?.StartsWith("/lib") == true ||
                    path?.StartsWith("/images") == true ||
                    path?.StartsWith("/uploads") == true ||
                    path?.StartsWith("/required") == true ||
                    path?.Contains('.') == true; // catches .css, .js, .png, .ico etc.


            if (!isAccountPath && !isRootPath && !isStaticFile)
            {
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    using var scope = context.RequestServices.CreateScope();
                    var dbContext = scope.ServiceProvider
                        .GetRequiredService<MedicalOnboardingApplicationContext>();

                    var userName = context.User.Identity.Name;
                    var user = await dbContext.Users
                        .FirstOrDefaultAsync(u => u.UserName == userName);

                    if (user?.ClinicId != null)
                    {
                        var clinic = await dbContext.Clinics
                            .FirstOrDefaultAsync(c => c.Id == user.ClinicId);

                        if (clinic != null && !string.IsNullOrEmpty(clinic.Subdomain))
                        {
                            context.Response.Redirect($"http://{clinic.Subdomain}.{_baseDomain}:{port}");
                            return;
                        }
                    }
                }
                else
                {
                    context.Response.Redirect($"http://{_baseDomain}:{port}/Account/Login");
                    return;
                }
            }
        }

        await _next(context);
    }
}