using MeetAmalfiCoast.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Google.Apis.Auth.OAuth2;
using MeetAmalfiCoast.Services.Configuration;
using Google.Cloud.Firestore;

public class FirestorePlanningService
{
    private readonly FirestoreDb _db;
    private readonly ApplicationConfigurationService _configuration;

    public FirestorePlanningService(IWebHostEnvironment environment, ApplicationConfigurationService configuration)
    {
        _configuration = configuration;

        string credentialPath = Path.Combine(
            environment.ContentRootPath,
            "Configuration",
            "firebase-service-account.json"
        );

#pragma warning disable CS0618
        GoogleCredential credential = GoogleCredential.FromFile(credentialPath);
#pragma warning restore CS0618

        _db = new FirestoreDbBuilder
        {
            ProjectId = "test-909e7",
            Credential = credential
        }.Build();
    }

    public async Task<List<MeetAmalfiCoast.Models.PlanningAppointmentModel>> GetPendingAppointmentsAsync()
    {
        Query query = _db.Collection("appointments")
            .WhereEqualTo("syncStatus", "pending");

        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        List<MeetAmalfiCoast.Models.PlanningAppointmentModel> appointments = new();

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            Dictionary<string, object> data = doc.ToDictionary();

            appointments.Add(new MeetAmalfiCoast.Models.PlanningAppointmentModel
            {
                Id = doc.Id,
                IsoDate = data.GetValueOrDefault("isoDate")?.ToString() ?? "",
                Start = data.GetValueOrDefault("start")?.ToString() ?? "",
                End = data.GetValueOrDefault("end")?.ToString() ?? "",
                Title = data.GetValueOrDefault("title")?.ToString() ?? "",

                Customer = data.GetValueOrDefault("customerName")?.ToString()
                           ?? data.GetValueOrDefault("customer")?.ToString()
                           ?? "",

                CustomerEmail = data.GetValueOrDefault("customerEmail")?.ToString() ?? "",
                GoogleEventId = data.GetValueOrDefault("googleEventId")?.ToString(),
                GoogleCalendarId = data.GetValueOrDefault("googleCalendarId")?.ToString(),
                SyncStatus = data.GetValueOrDefault("syncStatus")?.ToString() ?? "pending",
                SyncError = data.GetValueOrDefault("syncError")?.ToString(),
                Source = data.GetValueOrDefault("source")?.ToString() ?? "manual"
            });
        }

        return appointments;
    }

    public async Task MarkAsSyncedAsync(
        string appointmentId,
        string googleEventId,
        string calendarId)
    {
        DocumentReference docRef = _db.Collection("appointments").Document(appointmentId);

        await docRef.UpdateAsync(new Dictionary<string, object?>
        {
            { "googleEventId", googleEventId },
            { "googleCalendarId", calendarId },
            { "syncStatus", "synced" },
            { "syncError", null },
            { "source", "website" },
            { "syncedAt", Timestamp.GetCurrentTimestamp() },
            { "updatedAt", Timestamp.GetCurrentTimestamp() }
        });
    }

    public async Task MarkAsErrorAsync(string appointmentId, string errorMessage)
    {
        DocumentReference docRef = _db.Collection("appointments").Document(appointmentId);

        await docRef.UpdateAsync(new Dictionary<string, object>
        {
            { "syncStatus", "error" },
            { "syncError", errorMessage },
            { "updatedAt", Timestamp.GetCurrentTimestamp() }
        });
    }

    public async Task DeleteAppointmentByGoogleEventIdAsync(string googleEventId)
    {
        Query query = _db.Collection("appointments")
            .WhereEqualTo("googleEventId", googleEventId);

        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            await doc.Reference.DeleteAsync();
        }
    }

    public async Task UpdateAppointmentFromGoogleAsync(
        string googleEventId,
        string title,
        DateTime startDateTime,
        DateTime endDateTime)
    {
        Query query = _db.Collection("appointments")
            .WhereEqualTo("googleEventId", googleEventId)
            .Limit(1);

        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        if (snapshot.Documents.Count == 0)
        {
            return;
        }

        DocumentSnapshot doc = snapshot.Documents[0];

        string isoDate = startDateTime.ToString("yyyy-MM-dd");
        string start = startDateTime.ToString("HH:mm");
        string end = endDateTime.ToString("HH:mm");

        await doc.Reference.UpdateAsync(new Dictionary<string, object?>
        {
            { "title", title },
            { "isoDate", isoDate },
            { "start", start },
            { "end", end },
            { "syncStatus", "synced" },
            { "syncError", null },
            { "source", "google" },
            { "lastModifiedBy", "google" },
            { "updatedAt", Timestamp.GetCurrentTimestamp() }
        });
    }

    public async Task UpsertAppointmentFromGoogleAsync(
    string googleEventId,
    string title,
    string customerName,
    string customerEmail,
    string customerPhone,
    string notes,
    DateTime startDateTime,
    DateTime endDateTime)
    {
        Query query = _db.Collection("appointments")
            .WhereEqualTo("googleEventId", googleEventId)
            .Limit(1);

        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        string isoDate = startDateTime.ToString("yyyy-MM-dd");
        string start = startDateTime.ToString("HH:mm");
        string end = endDateTime.ToString("HH:mm");

        Dictionary<string, object?> data = new()
            {
                { "title", title },
                { "customerName", customerName },
                { "customerEmail", customerEmail },
                { "customerPhone", customerPhone },
                { "notes", notes },

                { "isoDate", isoDate },
                { "start", start },
                { "end", end },

                { "googleEventId", googleEventId },
                { "googleCalendarId", "primary" },
                { "syncStatus", "synced" },
                { "syncError", null },
                { "source", "google" },
                { "lastModifiedBy", "google" },
                { "updatedAt", Timestamp.GetCurrentTimestamp() }
            };

        if (snapshot.Documents.Count > 0)
        {
            await snapshot.Documents[0].Reference.UpdateAsync(data);
            return;
        }

        data.Add("pickupAddress", "");
        data.Add("dropoffAddress", "");
        data.Add("status", "confirmed");

        data.Add("reminderEmailSent", false);
        data.Add("reminderEmailSentAt", null);

        data.Add("createdAt", Timestamp.GetCurrentTimestamp());
        data.Add("lastModifiedAt", Timestamp.GetCurrentTimestamp());

        await _db.Collection("appointments").AddAsync(data);
    }

    public async Task ClearAllAppointmentsAsync()
    {
        CollectionReference collection = _db.Collection("appointments");

        QuerySnapshot snapshot = await collection.GetSnapshotAsync();

        WriteBatch batch = _db.StartBatch();

        int counter = 0;

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            batch.Delete(doc.Reference);
            counter++;

            if (counter == 450)
            {
                await batch.CommitAsync();
                batch = _db.StartBatch();
                counter = 0;
            }
        }

        if (counter > 0)
        {
            await batch.CommitAsync();
        }
    }

    public async Task<string> CreatePaidAppointmentAsync(
    MeetAmalfiCoast.Models.PlanningAppointmentModel appointment,
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

        { "createdAt", Google.Cloud.Firestore.Timestamp.GetCurrentTimestamp() },
        { "updatedAt", Google.Cloud.Firestore.Timestamp.GetCurrentTimestamp() },

        { "reminderEmailSent", false },
        { "reminderEmailSentAt", null },

        { "lastModifiedAt", Google.Cloud.Firestore.Timestamp.GetCurrentTimestamp() }
    };

        var doc = await _db.Collection("appointments").AddAsync(data);

        return doc.Id;
    }

    #region Reminder Email Methods

    // Questa regione contiene metodi per gestire le email di promemoria per gli appuntamenti confermati.
    public async Task<List<PlanningAppointmentModel>>
        GetAppointmentsForReminderAsync()
    {
        Query query = _db.Collection("appointments")
            .WhereEqualTo("status", "confirmed")
            .WhereEqualTo("reminderEmailSent", false);

        if (!_configuration.IsDebugMode)
        {
            string tomorrowIsoDate = DateTime.Today
                .AddDays(1)
                .ToString("yyyy-MM-dd");

            query = query.WhereEqualTo(
                "isoDate",
                tomorrowIsoDate
            );
        }

        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        List<PlanningAppointmentModel> appointments = new();

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            Dictionary<string, object> data = doc.ToDictionary();

            string customerEmail =
                data.GetValueOrDefault("customerEmail")?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(customerEmail))
            {
                continue;
            }

            appointments.Add(new PlanningAppointmentModel
            {
                Id = doc.Id,
                IsoDate = data.GetValueOrDefault("isoDate")?.ToString() ?? "",
                Start = data.GetValueOrDefault("start")?.ToString() ?? "",
                End = data.GetValueOrDefault("end")?.ToString() ?? "",
                Title = data.GetValueOrDefault("title")?.ToString() ?? "",

                Customer =
                    data.GetValueOrDefault("customerName")?.ToString()
                    ?? data.GetValueOrDefault("customer")?.ToString()
                    ?? "",

                CustomerEmail = customerEmail,
                GoogleEventId =
                    data.GetValueOrDefault("googleEventId")?.ToString(),
                GoogleCalendarId =
                    data.GetValueOrDefault("googleCalendarId")?.ToString(),
                SyncStatus =
                    data.GetValueOrDefault("syncStatus")?.ToString() ?? "synced",
                SyncError =
                    data.GetValueOrDefault("syncError")?.ToString(),
                Source =
                    data.GetValueOrDefault("source")?.ToString() ?? "website"
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

    public async Task<int> DeleteOldAppointmentsAsync()
    {
        DateTime limitDate = DateTime.Today.AddDays(-_configuration.FirestoreAppointmentRetentionDays);

        string limitIsoDate = limitDate.ToString("yyyy-MM-dd");

        QuerySnapshot snapshot = await _db.Collection("appointments")
            .WhereLessThanOrEqualTo("isoDate", limitIsoDate)
            .GetSnapshotAsync();

        WriteBatch batch = _db.StartBatch();

        int batchCounter = 0;
        int deletedCount = 0;

        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            batch.Delete(document.Reference);

            batchCounter++;
            deletedCount++;

            if (batchCounter >= 450)
            {
                await batch.CommitAsync();

                batch = _db.StartBatch();
                batchCounter = 0;
            }
        }

        if (batchCounter > 0)
        {
            await batch.CommitAsync();
        }

        return deletedCount;
    }

    // Metodo per ottenere un appuntamento in base all'ID dell'evento di Google Calendar
    public async Task<PlanningAppointmentModel?> GetAppointmentByGoogleEventIdAsync(
    string googleEventId)
    {
        Query query = _db.Collection("appointments")
            .WhereEqualTo("googleEventId", googleEventId)
            .Limit(1);

        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        if (snapshot.Documents.Count == 0)
        {
            return null;
        }

        DocumentSnapshot doc = snapshot.Documents[0];
        Dictionary<string, object> data = doc.ToDictionary();

        return new PlanningAppointmentModel
        {
            Id = doc.Id,
            Title = data.GetValueOrDefault("title")?.ToString() ?? "",

            Customer =
                data.GetValueOrDefault("customerName")?.ToString()
                ?? data.GetValueOrDefault("customer")?.ToString()
                ?? "",

            CustomerEmail =
                data.GetValueOrDefault("customerEmail")?.ToString() ?? "",

            CustomerPhone =
                data.GetValueOrDefault("customerPhone")?.ToString() ?? "",

            IsoDate =
                data.GetValueOrDefault("isoDate")?.ToString() ?? "",

            Start =
                data.GetValueOrDefault("start")?.ToString() ?? "",

            End =
                data.GetValueOrDefault("end")?.ToString() ?? "",

            GoogleEventId =
                data.GetValueOrDefault("googleEventId")?.ToString(),

            GoogleCalendarId =
                data.GetValueOrDefault("googleCalendarId")?.ToString(),

            SyncStatus =
                data.GetValueOrDefault("syncStatus")?.ToString() ?? "synced",

            SyncError =
                data.GetValueOrDefault("syncError")?.ToString(),

            Source =
                data.GetValueOrDefault("source")?.ToString() ?? "website"
        };
    }
}