using EbayClone.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace EbayClone.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var email = new MimeMessage();
            string senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "";
            string senderName = _configuration["EmailSettings:SenderName"] ?? "Ebay Clone";

            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(new MailboxAddress("", to));
            email.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(
                    _configuration["EmailSettings:SmtpServer"], 
                    int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587"), 
                    SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    _configuration["EmailSettings:SenderEmail"], 
                    _configuration["EmailSettings:Password"]);

                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Backup log nếu SMTP tèo
                Console.WriteLine($"[CRITICAL] SMTP Error: {ex.Message}");
                // Trong thực tế sẽ dùng Logger chuyên nghiệp hơn
            }
        }

        public async Task SendVerificationEmailAsync(string to, string token)
        {
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px; border-radius: 8px;'>
                    <img src='https://upload.wikimedia.org/wikipedia/commons/1/1b/EBay_logo.svg' width='100' alt='eBay Logo' />
                    <h2 style='color: #333;'>Confirm your email address</h2>
                    <p>To finish setting up your account, please enter this code on the verification page:</p>
                    <div style='background: #f4f4f4; padding: 15px; font-size: 24px; font-weight: bold; text-align: center; letter-spacing: 5px; color: #0654ba;'>
                        {token}
                    </div>
                    <p>This code will expire in 15 minutes.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #888;'>If you didn't request this email, you can safely ignore it.</p>
                </div>";

            await SendEmailAsync(to, "Confirm your registration - eBay", body);
        }
    }
}
