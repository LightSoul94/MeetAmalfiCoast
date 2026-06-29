using Google.Cloud.Firestore;
using MeetAmalfiCoasts.Models;

public class FirestorePlanningService
{
    private readonly FirestoreDb _db;

    public FirestorePlanningService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var projectId = configuration["Firestore:ProjectId"];

        if (string.IsNullOrWhiteSpace(projectId))
            throw new InvalidOperationException("Firestore:ProjectId non configurato in appsettings.json.");

        var credentialPath = Path.Combine(
            environment.ContentRootPath,
            "firebase-service-account.json"
        );

        if (!File.Exists(credentialPath))
            throw new FileNotFoundException("File firebase-service-account.json non trovato.", credentialPath);

        Environment.SetEnvironmentVariable(
            "GOOGLE_APPLICATION_CREDENTIALS",
            credentialPath
        );

        _db = FirestoreDb.Create(projectId);
    }

    public async Task<string> CreatePaidAppointmentAsync(
        PlanningAppointment appointment,
        string stripeSessionId,
        long depositAmount,
        string currency)
    {
        Dictionary<string, object?> data = new()
        {
            { "title", appointment.Title },
            { "customerName", appointment.Customer },
            { "customerEmail", appointment.CustomerEmail },
            { "customerPhone", "" },

            { "pickupAddress", "" },
            { "dropoffAddress", "" },

            { "isoDate", appointment.IsoDate },
            { "start", appointment.Start },
            { "end", appointment.End },

            { "notes", "" },
            { "status", "confirmed" },

            { "paymentStatus", "paid" },
            { "paymentType", "deposit" },
            { "depositAmount", depositAmount },
            { "currency", currency },
            { "stripeSessionId", stripeSessionId },

            { "googleEventId", null },
            { "googleCalendarId", null },

            { "syncStatus", "pending" },
            { "syncError", null },

            { "source", "website" },
            { "lastModifiedBy", "website" },

            { "createdAt", Timestamp.GetCurrentTimestamp() },
            { "updatedAt", Timestamp.GetCurrentTimestamp() },
            { "lastModifiedAt", Timestamp.GetCurrentTimestamp() }
        };

        DocumentReference doc = await _db
            .Collection("appointments")
            .AddAsync(data);

        return doc.Id;
    }
}