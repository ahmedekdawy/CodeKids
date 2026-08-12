using System;
using System.Collections.Generic;
using System.Text;

namespace CodeKids.Application.Abstractions
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
