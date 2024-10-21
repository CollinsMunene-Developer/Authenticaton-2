// EmailService.cs
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace AuthSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _sendGridKey;
        private readonly string _fromEmail;
        private readonly string _frontendUrl;

        public EmailService(IConfiguration configuration)
        {
            _sendGridKey = configuration["SendGrid:ApiKey"];
            _fromEmail = configuration["SendGrid:FromEmail"];
            _frontendUrl = configuration["FrontendUrl"];
        }

        public async Task SendVerificationEmailAsync(string email, string token)
        {
            var verificationLink = $"{_frontendUrl}/verify-email?token={token}&email={email}";
            var client = new SendGridClient(_sendGridKey);
            var from = new EmailAddress(_fromEmail, "Your App Name");
            var to = new EmailAddress(email);
            var subject = "Verify your email address";
            var plainTextContent = $"Please verify your email by clicking this link: {verificationLink}";
            var htmlContent = $@"
                <h1>Email Verification</h1>
                <p>Please verify your email by clicking the link below:</p>
                <a href='{verificationLink}'>Verify Email</a>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            await client.SendEmailAsync(msg);
        }

        // Implement other email sending methods...
    }
}
