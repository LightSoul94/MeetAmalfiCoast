using Google.Cloud.Firestore;
using MeetAmalfiCoast.Models;

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
            "Configuration",
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
        PlanningAppointmentsModel appointment,
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

            { "reminderEmailSent", false },
            { "reminderEmailSentAt", null },

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
    #region Reminder Email Methods

    // Questa regione contiene metodi per gestire le email di promemoria per gli appuntamenti confermati.
    public async Task<List<PlanningAppointmentsModel>> GetAppointmentsForReminderAsync()
    {
        DateTime tomorrow = DateTime.Today.AddDays(1);

        string tomorrowIsoDate = tomorrow.ToString("yyyy-MM-dd");

        Query query = _db.Collection("appointments")
            .WhereEqualTo("isoDate", tomorrowIsoDate)
            .WhereEqualTo("status", "confirmed")
            .WhereEqualTo("reminderEmailSent", false);

        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        List<PlanningAppointmentsModel> appointments = new();

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            Dictionary<string, object> data = doc.ToDictionary();

            string customerEmail = data.GetValueOrDefault("customerEmail")?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(customerEmail))
            {
                continue;
            }

            appointments.Add(new PlanningAppointmentsModel
            {
                Id = doc.Id,
                IsoDate = data.GetValueOrDefault("isoDate")?.ToString() ?? "",
                Start = data.GetValueOrDefault("start")?.ToString() ?? "",
                End = data.GetValueOrDefault("end")?.ToString() ?? "",
                Title = data.GetValueOrDefault("title")?.ToString() ?? "",

                Customer = data.GetValueOrDefault("customerName")?.ToString()
                           ?? data.GetValueOrDefault("customer")?.ToString()
                           ?? "",

                CustomerEmail = customerEmail,
                SyncStatus = data.GetValueOrDefault("syncStatus")?.ToString() ?? "synced",
                SyncError = data.GetValueOrDefault("syncError")?.ToString(),
                Source = data.GetValueOrDefault("source")?.ToString() ?? "website"
            });
        }

        return appointments;
    }

    // Metodo per aggiornare lo stato dell'appuntamento dopo l'invio dell'email di promemoria
    public async Task MarkReminderEmailAsSentAsync(string appointmentId)
    {
        DocumentReference docRef = _db.Collection("appointments").Document(appointmentId);

        await docRef.UpdateAsync(new Dictionary<string, object?>
    {
        { "reminderEmailSent", true },
        { "reminderEmailSentAt", Timestamp.GetCurrentTimestamp() },
        { "updatedAt", Timestamp.GetCurrentTimestamp() }
    });
    }
    #endregion
}