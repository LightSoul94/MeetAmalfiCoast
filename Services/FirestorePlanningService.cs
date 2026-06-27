using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

public class FirestorePlanningService
{
    private readonly FirestoreDb _db;

    public FirestorePlanningService(IWebHostEnvironment environment)
    {
        string credentialPath = Path.Combine(
            environment.ContentRootPath,
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

    public async Task<List<PlanningAppointment>> GetPendingAppointmentsAsync()
    {
        Query query = _db.Collection("appointments")
            .WhereEqualTo("syncStatus", "pending");

        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        List<PlanningAppointment> appointments = new();

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            Dictionary<string, object> data = doc.ToDictionary();

            appointments.Add(new PlanningAppointment
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

        data.Add("customerName", "Google Calendar");
        data.Add("customerEmail", "");
        data.Add("customerPhone", "");
        data.Add("pickupAddress", "");
        data.Add("dropoffAddress", "");
        data.Add("notes", "");
        data.Add("status", "confirmed");
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

        { "createdAt", Google.Cloud.Firestore.Timestamp.GetCurrentTimestamp() },
        { "updatedAt", Google.Cloud.Firestore.Timestamp.GetCurrentTimestamp() },
        { "lastModifiedAt", Google.Cloud.Firestore.Timestamp.GetCurrentTimestamp() }
    };

        var doc = await _db.Collection("appointments").AddAsync(data);

        return doc.Id;
    }
}