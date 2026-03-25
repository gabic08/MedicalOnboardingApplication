using MailKit.Net.Smtp;
using MedicalOnboardingApplication.Services.Interfaces;
using MimeKit;

namespace MedicalOnboardingApplication.Services;

public class EmailService : IEmailService
{
    private ILogger<EmailService> _logger;
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {

            var section = _config.GetSection("EmailConfiguration");
            var from = section["From"]!;
            var password = section["Password"]!;
            var host = section["Host"]!;
            var port = int.Parse(section["Port"]!);
            var sourceName = section["SourceName"]!;

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(sourceName, from));
            emailMessage.To.Add(new MailboxAddress(string.Empty, to));
            emailMessage.Subject = subject;

            // Set the email body to HTML format
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
            {
                Text = body
            };

            using SmtpClient client = new();

            await client.ConnectAsync(host, port, true);
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            await client.AuthenticateAsync(from, password);
            await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }
}