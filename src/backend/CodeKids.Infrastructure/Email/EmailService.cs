using CodeKids.Application.Abstractions;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.Net.Mail;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

public class EmailService: IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        // 1. Fetch values from configuration
        var smtpServer = _configuration["Email:Host"];
        var port = int.Parse(_configuration["Email:Port"]);
        var senderName = _configuration["Email:FromDisplayName"];
        var senderEmail = _configuration["Email:UserName"];
        var appPassword = _configuration["Email:AppPassword"];

        // 2. Build the email message
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(new MailboxAddress("", toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = body };

        // 3. Connect and send via Gmail SMTP
        using var client = new SmtpClient();
        try
        {
            // Use SecureSocketOptions.StartTls for Port 587
            await client.ConnectAsync(smtpServer, port, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, appPassword);
            await client.SendAsync(message);
        }
        catch (Exception ex)
        {

        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}