namespace Rise.Shared.Emails;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlContent);
}
