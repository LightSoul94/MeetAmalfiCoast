using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

/// <summary>
/// Servizio per la gestione degli appuntamenti di planning tramite Firestore
/// Gestisce le operazioni CRUD e la sincronizzazione con Google Calendar
/// </summary>
public class FirestorePlanningService
{
    // Istanza del database Firestore per le operazioni
    private readonly FirestoreDb _db;

    /// <summary>
    /// Costruttore che inizializza la connessione a Firestore
    /// Legge le credenziali da firebase-service-account.json
    /// </summary>
    public FirestorePlanningService(IWebHostEnvironment environment)
    {
        // Costruisce il percorso del file delle credenziali
        string credentialPath = Path.Combine(
            environment.ContentRootPath,
            "firebase-service-account.json"
        );

        // Imposta la variabile d'ambiente per le credenziali di Google
        // TODO: Sostituire con CredentialFactory quando Google stabilizzerà le API.
        #pragma warning disable CS0618
        GoogleCredential credential = GoogleCredential.FromFile(credentialPath);
        #pragma warning restore CS0618

        // Crea un'istanza di FirestoreDb
        _db = new FirestoreDbBuilder
        {
            ProjectId = "test-909e7",
            Credential = credential
        }.Build();
    }

    /// <summary>
    /// Recupera tutti gli appuntamenti in sospeso dalla raccolta Firestore
    /// Gli appuntamenti sono quelli con syncStatus = "pending"
    /// </summary>
    public async Task<List<PlanningAppointment>> GetPendingAppointmentsAsync()
    {
        // Crea una query per recuperare gli appuntamenti in sospeso
        Query query = _db.Collection("appointments")
            .WhereEqualTo("syncStatus", "pending");

        // Esegue la query e ottiene i risultati
        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        // Inizializza la lista degli appuntamenti
        List<PlanningAppointment> appointments = new();

        // Elabora ogni documento dal database
        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            // Converte il documento Firestore in un dizionario
            Dictionary<string, object> data = doc.ToDictionary();

            appointments.Add(new PlanningAppointment
            {
                Id = doc.Id,
                IsoDate = data.GetValueOrDefault("isoDate")?.ToString() ?? "",
                Start = data.GetValueOrDefault("start")?.ToString() ?? "",
                End = data.GetValueOrDefault("end")?.ToString() ?? "",
                Title = data.GetValueOrDefault("title")?.ToString() ?? "",
                Customer = data.GetValueOrDefault("customer")?.ToString() ?? "",
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

    /// <summary>
    /// Marca un appuntamento come sincronizzato con Google Calendar
    /// Aggiorna lo stato e salva i dati di sincronizzazione
    /// </summary>
    public async Task MarkAsSyncedAsync(string appointmentId, string googleEventId, string calendarId)
    {
        // Ottiene il riferimento al documento dell'appuntamento
        DocumentReference docRef = _db.Collection("appointments").Document(appointmentId);

        // Aggiorna i campi del documento per marcare la sincronizzazione completata
        await docRef.UpdateAsync(new Dictionary<string, object?>
        {
            { "googleEventId", googleEventId },      // ID dell'evento Google Calendar
            { "googleCalendarId", calendarId },      // ID del calendario Google
            { "syncStatus", "synced" },              // Stato: sincronizzato
            { "syncError", null },                   // Cancella gli errori precedenti
            { "source", "google" },                  // Sorgente: Google Calendar
            { "syncedAt", Timestamp.GetCurrentTimestamp() }  // Timestamp della sincronizzazione
        });
    }

    /// <summary>
    /// Marca un appuntamento con stato di errore durante la sincronizzazione
    /// Salva il messaggio di errore per il debug
    /// </summary>
    public async Task MarkAsErrorAsync(string appointmentId, string errorMessage)
    {
        // Ottiene il riferimento al documento dell'appuntamento
        DocumentReference docRef = _db.Collection("appointments").Document(appointmentId);

        // Aggiorna i campi per indicare un errore di sincronizzazione
        await docRef.UpdateAsync(new Dictionary<string, object>
        {
            { "syncStatus", "error" },           // Stato: errore
            { "syncError", errorMessage }        // Messaggio di errore per il debug
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
}