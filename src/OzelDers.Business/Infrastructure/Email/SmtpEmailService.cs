using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using OzelDers.Business.Interfaces;

namespace OzelDers.Business.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config["Email:FromName"], _config["Email:FromAddress"]));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_config["Email:SmtpServer"], int.Parse(_config["Email:SmtpPort"]!), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_config["Email:SmtpUsername"], _config["Email:SmtpPassword"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("Email sent successfully to {ToEmail}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {ToEmail}", to);
            // Re-throw or handle depending on requirements
        }
    }

    public async Task SendTemplatedEmailAsync(string to, string templateName, Dictionary<string, string> replacements)
    {
        // Simple mock for templated emails — in real app, read from HTML files
        var htmlBody = $"<h1>{templateName}</h1><p>Welcome to OzelDers!</p>";
        foreach (var r in replacements)
        {
            htmlBody = htmlBody.Replace($"{{{{{r.Key}}}}}", r.Value);
        }
        await SendEmailAsync(to, templateName, htmlBody);
    }
}
