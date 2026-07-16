using System.ComponentModel.DataAnnotations;

public class NewsletterSubscriptionRequestModel
{
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public bool PrivacyConsent { get; set; }

    // Honeypot antispam
    public string Website { get; set; } = string.Empty;
}