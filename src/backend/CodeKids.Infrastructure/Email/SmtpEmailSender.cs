using System.Net;
using System.Net.Mail;
using CodeKids.Application.Abstractions;
using CodeKids.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CodeKids.Infrastructure.Email;

public sealed class SmtpEmailSender(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string plainTextBody, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.Host))
        {
            logger.LogWarning(
                "Email disabled or SMTP not configured. To={To} Subject={Subject} Body={Body}",
                toEmail,
                subject,
                plainTextBody);
            return;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, settings.FromDisplayName),
            Subject = subject,
            Body = plainTextBody,
            IsBodyHtml = false
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(settings.UserName))
        {
            client.Credentials = new NetworkCredential(settings.UserName, settings.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
        logger.LogInformation("Password recovery email sent to {To}", toEmail);
    }
}
