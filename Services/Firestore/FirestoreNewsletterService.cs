using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

public class FirestoreNewsletterService
{
    private const string CollectionName = "newsletterSubscribers";

    private readonly FirestoreDb _firestoreDb;

    public FirestoreNewsletterService(IWebHostEnvironment environment)
    {
        string credentialPath = Path.Combine(
            environment.ContentRootPath,
            "Configuration",
            "firebase-service-account.json"
        );

#pragma warning disable CS0618
        GoogleCredential credential =
            GoogleCredential.FromFile(credentialPath);
#pragma warning restore CS0618

        _firestoreDb = new FirestoreDbBuilder
        {
            ProjectId = "test-909e7",
            Credential = credential
        }.Build();
    }

    public async Task<NewsletterSubscriptionResult> SubscribeAsync(
        NewsletterSubscriptionRequest request)
    {
        string normalizedEmail = NormalizeEmail(request.Email);
        string documentId = CreateEmailHash(normalizedEmail);
        string unsubscribeToken = Guid.NewGuid().ToString("N");

        DocumentReference documentReference = _firestoreDb
            .Collection(CollectionName)
            .Document(documentId);

        return await _firestoreDb.RunTransactionAsync(
            async transaction =>
            {
                DocumentSnapshot snapshot =
                    await transaction.GetSnapshotAsync(documentReference);

                if (snapshot.Exists)
                {
                    return NewsletterSubscriptionResult.Existing();
                }

                NewsletterSubscriptionModel subscription = new()
                {
                    Email = request.Email.Trim(),
                    NormalizedEmail = normalizedEmail,
                    Status = "active",
                    PrivacyConsent = true,
                    SubscribedAt = Timestamp.GetCurrentTimestamp(),
                    Source = "website",
                    Language = "en",

                    WelcomeEmailSent = false,
                    WelcomeEmailSentAt = null,
                    LastReminderEmailSentAt = null,

                    UnsubscribeToken = unsubscribeToken
                };

                transaction.Create(
                    documentReference,
                    subscription
                );

                return NewsletterSubscriptionResult.Created(
                    unsubscribeToken
                );
            });
    }

    public async Task MarkWelcomeEmailAsSentAsync(
        string email)
    {
        string normalizedEmail = NormalizeEmail(email);
        string documentId = CreateEmailHash(normalizedEmail);

        DocumentReference documentReference = _firestoreDb
            .Collection(CollectionName)
            .Document(documentId);

        await documentReference.UpdateAsync(
            new Dictionary<string, object>
            {
                ["WelcomeEmailSent"] = true,
                ["WelcomeEmailSentAt"] =
                    Timestamp.GetCurrentTimestamp()
            }
        );
    }

    // Invia promemoria newsletter mensile
    public async Task<List<NewsletterSubscriptionModel>>
        GetSubscribersForMonthlyReminderAsync()
    {
        Query query = _firestoreDb
            .Collection(CollectionName)
            .WhereEqualTo("Status", "active");

        QuerySnapshot snapshot =
            await query.GetSnapshotAsync();

        List<NewsletterSubscriptionModel> subscribers = new();

        DateTime utcNow = DateTime.UtcNow;

        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            NewsletterSubscriptionModel subscriber =
                document.ConvertTo<NewsletterSubscriptionModel>();

            subscriber.Id = document.Id;

            DateTime referenceDate =
                subscriber.LastReminderEmailSentAt?.ToDateTime()
                ?? subscriber.SubscribedAt.ToDateTime();

            if (referenceDate.AddMonths(1) <= utcNow)
            {
                subscribers.Add(subscriber);
            }
        }

        return subscribers;
    }

    public async Task MarkReminderEmailAsSentAsync(
        string documentId)
    {
        DocumentReference documentReference = _firestoreDb
            .Collection(CollectionName)
            .Document(documentId);

        await documentReference.UpdateAsync(
            new Dictionary<string, object>
            {
                ["LastReminderEmailSentAt"] =
                    Timestamp.GetCurrentTimestamp()
            }
        );
    }

    public async Task<bool> DeleteByUnsubscribeTokenAsync(
        string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        Query query = _firestoreDb
            .Collection(CollectionName)
            .WhereEqualTo("UnsubscribeToken", token)
            .Limit(1);

        QuerySnapshot snapshot =
            await query.GetSnapshotAsync();

        DocumentSnapshot? document =
            snapshot.Documents.FirstOrDefault();

        if (document is null)
        {
            return false;
        }

        await document.Reference.DeleteAsync();

        return true;
    }

    private static string NormalizeEmail(string email)
    {
        return email
            .Trim()
            .ToLowerInvariant();
    }

    private static string CreateEmailHash(
        string normalizedEmail)
    {
        byte[] bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(normalizedEmail)
        );

        return Convert
            .ToHexString(bytes)
            .ToLowerInvariant();
    }
}