using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace CaseShop.Web.Services.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;

    public SmtpEmailSender(IOptions<SmtpOptions> options) => _options = options.Value;

    public async Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(new MailAddress(recipient));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = string.IsNullOrWhiteSpace(_options.Username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(_options.Username, NormalizePassword(_options.Host, _options.Password))
        };

        await client.SendMailAsync(message, cancellationToken);
    }

    private static string NormalizePassword(string host, string password) =>
        host.Equals("smtp.gmail.com", StringComparison.OrdinalIgnoreCase)
            ? string.Concat(password.Where(character => !char.IsWhiteSpace(character)))
            : password;
}
