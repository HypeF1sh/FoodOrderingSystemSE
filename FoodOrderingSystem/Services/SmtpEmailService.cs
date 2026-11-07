// PB26 (Wallace) - email sending by SMTP and/or to a pickup folder
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

        // 1. Always write the .eml copy when UsePickupDirectory is enabled (great for grading/offline demo)
        if (_options.UsePickupDirectory)
        {
            try
            {
                var pickupPath = Path.IsPathRooted(_options.PickupDirectory)
                    ? _options.PickupDirectory
                    : Path.Combine(_env.ContentRootPath, _options.PickupDirectory);

                Directory.CreateDirectory(pickupPath);

                using var pickupMail = CreateMailMessage(toEmail, subject, body, isHtml, _options.FromAddress);
                using var pickupClient = new SmtpClient
                {
                    DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                    PickupDirectoryLocation = pickupPath,
                    EnableSsl = false
                };

                await pickupClient.SendMailAsync(pickupMail);
                _logger.LogInformation("PB26: Saved .eml confirmation for {Email} in {Path}.", toEmail, pickupPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PB26: Failed to write .eml file for {Email}.", toEmail);
            }
        }

        // 2. Real Inbox Delivery: if SMTP credentials are configured (in user-secrets) and recipient isn't a fake @rroms.com address
        bool hasSmtpCredentials = !string.IsNullOrWhiteSpace(_options.SmtpHost)
                                  && _options.SmtpHost != "smtp.example.com"
                                  && _options.SmtpHost != "localhost"
                                  && !string.IsNullOrWhiteSpace(_options.Username)
                                  && !string.IsNullOrWhiteSpace(_options.Password);

        if (hasSmtpCredentials)
        {
            if (toEmail.EndsWith("@rroms.com", StringComparison.OrdinalIgnoreCase) ||
                toEmail.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("PB26: Skipping real SMTP send for seeded demo address {Email} (.eml file was saved).", toEmail);
                return;
            }

            try
            {
                // Use the authenticated SMTP username as the sender address so Gmail doesn't block it
                var senderAddress = _options.Username.Contains('@') ? _options.Username : _options.FromAddress;

                using var smtpMail = CreateMailMessage(toEmail, subject, body, isHtml, senderAddress);
                using var smtpClient = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
                {
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    EnableSsl = _options.EnableSsl,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_options.Username, _options.Password),
                    Timeout = 15000
                };

                await smtpClient.SendMailAsync(smtpMail);
                _logger.LogInformation("PB26: Live email delivered to inbox {Email} via {Host}.", toEmail, _options.SmtpHost);
            }
            catch (Exception ex)
            {
                // Never throw: email failures must never break bookings, orders, or payments
                _logger.LogWarning(ex, "PB26: Live SMTP send to {Email} failed. Carrying on.", toEmail);
            }
        }
    }

    private MailMessage CreateMailMessage(string toEmail, string subject, string body, bool isHtml, string fromAddress)
    {
        var mail = new MailMessage
        {
            From = new MailAddress(fromAddress, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };
        mail.To.Add(new MailAddress(toEmail));
        return mail;
    }
}