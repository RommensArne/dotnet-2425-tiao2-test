using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Rise.Shared.Emails;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Rise.Services.Emails
{
    public class EmailService : IEmailService
    {
        private readonly ISendGridClient _client;
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
            var apiKey = _config["SendGrid:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException("SendGrid API key is not configured.");
            }

            _client = new SendGridClient(apiKey);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            Console.WriteLine("Sending email to: " + toEmail);

            var from = new EmailAddress("simon.zachee@student.hogent.be", "buut"); // This is a verified sender in SendGrid, replace with the actual sender or use a dns verified domain
            var to = new EmailAddress(toEmail);
            var plainTextContent = "This is the plain text version of the email."; // Optional fallback
            var msg = MailHelper.CreateSingleEmail(
                from,
                to,
                subject,
                plainTextContent,
                htmlContent
            );

            try
            {
                var response = await _client.SendEmailAsync(msg);

                if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
                {
                    Console.WriteLine("Email sent successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to send email. Status: {response.StatusCode}");

                    var responseBody = await response.Body.ReadAsStringAsync();
                    Console.WriteLine($"Response body: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                throw;
            }
        }
    }
}
