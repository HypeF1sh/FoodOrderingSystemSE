namespace FoodOrderingSystem.Services;

public class EmailOptions
{
    public bool Enabled { get; set; } = true;
    public bool UsePickupDirectory { get; set; } = true;
    public string PickupDirectory { get; set; } = "App_Data/MailPickup";
    public string FromAddress { get; set; } = "no-reply@rumahrasa.local";
    public string FromName { get; set; } = "Rumah Rasa Restaurant";
    public string SmtpHost { get; set; } = "localhost";
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public interface IEmailService
{
    /// <summary>
    /// PB26: Sends an email confirmation. Logs and swallows any SMTP/IO error so callers never fail.
    /// </summary>
    Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
}