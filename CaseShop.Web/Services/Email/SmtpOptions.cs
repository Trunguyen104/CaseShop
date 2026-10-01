using System.ComponentModel.DataAnnotations;

namespace CaseShop.Web.Services.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";
    [Required] public string Host { get; set; } = string.Empty;
    [Range(1, 65535)] public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    [Required, EmailAddress] public string FromEmail { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string FromName { get; set; } = "CaseShop";
    [Required, EmailAddress] public string SupportEmail { get; set; } = string.Empty;
}
