// Crea il builder per la configurazione dell'applicazione web
var builder = WebApplication.CreateBuilder(args);

// Registra i servizi MVC
builder.Services.AddControllersWithViews();

// Registra i servizi di configurazione
    // Registra i servizi di configurazione per Google Calendar
builder.Services.Configure<GoogleCalendarSettings>(
    builder.Configuration.GetSection("GoogleCalendar"));
    // Registra i servizi di configurazione per SMTP
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("Smtp"));
    // Registra i servizi di configurazione per Stripe
builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe"));
// Registra i servizi di configurazione per Booking
builder.Services.Configure<BookingSettings>(
    builder.Configuration.GetSection("Booking"));

// Registra i servizi applicativi
    // Registra il servizio di Google Calendar
builder.Services.AddSingleton<GoogleCalendarService>();
    // Registra il servizio di Firestore Planning
builder.Services.AddSingleton<FirestorePlanningService>();
    // Registra il servizio di Stripe
builder.Services.AddSingleton<StripeService>();
    // Registra il servizio di sincronizzazione come hosted service
builder.Services.AddHostedService<GoogleCalendarSyncWorker>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}


app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "planning",
    pattern: "planning",
    defaults: new
    {
        controller = "Home",
        action = "Planning"
    });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();