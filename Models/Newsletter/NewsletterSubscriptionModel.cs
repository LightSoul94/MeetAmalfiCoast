using Google.Cloud.Firestore;

[FirestoreData]
public class NewsletterSubscriptionModel
{
    [FirestoreDocumentId]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Email { get; set; } = string.Empty;

    [FirestoreProperty]
    public string NormalizedEmail { get; set; } = string.Empty;

    [FirestoreProperty]
    public string Status { get; set; } = "active";

    [FirestoreProperty]
    public bool PrivacyConsent { get; set; }

    [FirestoreProperty]
    public Timestamp SubscribedAt { get; set; }

    [FirestoreProperty]
    public Timestamp? UnsubscribedAt { get; set; }

    [FirestoreProperty]
    public string Source { get; set; } = "website";

    [FirestoreProperty]
    public string Language { get; set; } = "en";

    [FirestoreProperty]
    public bool WelcomeEmailSent { get; set; }

    [FirestoreProperty]
    public Timestamp? WelcomeEmailSentAt { get; set; }

    [FirestoreProperty]
    public Timestamp? LastReminderEmailSentAt { get; set; }

    [FirestoreProperty]
    public string UnsubscribeToken { get; set; } = string.Empty;
}