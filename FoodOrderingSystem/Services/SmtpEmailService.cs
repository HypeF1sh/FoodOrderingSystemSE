// PB26 (Wallace) - email sending by SMTP or to a pickup folder
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;

namespace FoodOrderingSystem.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<EmailOptions> options,
        IWebHostEnvironment env,
        ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _env = env;
        _logger = logger;
    }

    // PB26: send one email; switched off or no address = do nothing
    public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(toEmail))
            return;

        try
        {
            // 1. Build the message (UTF-8, HTML)
            using var mail = new MailMessage
            {
                From = new MailAddress(_options.FromAddress, _options.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            mail.To.Add(new MailAddress(toEmail));

            using var client = new SmtpClient();

            // 2a. Demo mode: write the email as a .eml file in App_Data/MailPickup instead of sending
            if (_options.UsePickupDirectory)
            {
                var pickupPath = Path.IsPathRooted(_options.PickupDirectory)
                    ? _options.PickupDirectory
                    : Path.Combine(_env.ContentRootPath, _options.PickupDirectory);

                Directory.CreateDirectory(pickupPath);

                client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                client.PickupDirectoryLocation = pickupPath;
                client.EnableSsl = false;
            }
            else
            {
                // 2b. Real mode: send through the SMTP server in appsettings (password in user-secrets)
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.Host = _options.SmtpHost;
                client.Port = _options.SmtpPort;
                client.EnableSsl = _options.EnableSsl;

                if (!string.IsNullOrWhiteSpace(_options.Username))
                {
                    client.Credentials = new NetworkCredential(_options.Username, _options.Password);
                }
            }

            await client.SendMailAsync(mail);
            _logger.LogInformation("PB26: Confirmation email sent to {Email} with subject '{Subject}'.", toEmail, subject);
        }
        catch (Exception ex)
        {
            // Never throw: email failures must never break bookings, orders, or payments
            _logger.LogWarning(ex, "PB26: Failed to send email to {Email} (Subject: '{Subject}'). Carrying on.", toEmail, subject);
        }
    }
}