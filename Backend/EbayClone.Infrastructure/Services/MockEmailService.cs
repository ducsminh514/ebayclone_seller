using EbayClone.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace EbayClone.Infrastructure.Services
{
    public class MockEmailService : IEmailService
    {
        public Task SendEmailAsync(string to, string subject, string body)
        {
            // Trong thực tế, đây là nơi gọi SMTP hoặc SendGrid API
            Console.WriteLine("=================================================");
            Console.WriteLine($"[EMAIL SENT TO: {to}]");
            Console.WriteLine($"[SUBJECT: {subject}]");
            Console.WriteLine($"[BODY: {body}]");
            Console.WriteLine("=================================================");
            return Task.CompletedTask;
        }

        public async Task SendVerificationEmailAsync(string to, string token)
        {
            string subject = "Ebay Clone - Verify your email";
            string body = $"Your verification token is: {token}. It will expire in 15 minutes.";
            await SendEmailAsync(to, subject, body);
        }
    }
}
