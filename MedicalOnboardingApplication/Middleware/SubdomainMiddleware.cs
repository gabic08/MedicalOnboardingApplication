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

        string subdomain = null;

        if (host.EndsWith($".{_baseDomain}"))
        {
            subdomain = host[..^(_baseDomain.Length + 1)];
        }

        context.Items["Subdomain"] = subdomain;

        // If on a subdomain and trying to access auth routes, redirect to main domain
        if (!string.IsNullOrEmpty(subdomain))
        {
            var path = context.Request.Path.Value?.ToLower();
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
                var port = _configuration["AppSettings:BasePort"];
                var redirectUrl = $"http://{_baseDomain}:{port}{context.Request.Path}{context.Request.QueryString}";
                context.Response.Redirect(redirectUrl);
                return;
            }
        }

        await _next(context);
    }
}