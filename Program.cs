using MeetAmalfiCoast.Services.Configuration;

// Crea il builder per la configurazione dell'applicazione web
var builder = WebApplication.CreateBuilder(args);

// Registra i servizi MVC
builder.Services.AddControllersWithViews();


// Configurazioni

builder.Services.Configure<ApplicationSettings>(
    builder.Configuration.GetSection("Application"));

builder.Services.Configure<GoogleCalendarSettings>(
    builder.Configuration.GetSection("GoogleCalendar"));

builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("Smtp"));

builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe"));

builder.Services.Configure<BookingSettings>(
    builder.Configuration.GetSection("Booking"));

// Servizi applicativi

builder.Services.AddSingleton<ApplicationConfigurationService>();

builder.Services.AddSingleton<GoogleCalendarService>();

builder.Services.AddSingleton<FirestorePlanningService>();

builder.Services.AddSingleton<FirestoreNewsletterService>();

builder.Services.AddSingleton<NewsletterEmailTemplateService>();

builder.Services.AddSingleton<EmailService>();

builder.Services.AddSingleton<StripeService>();

    
// Hosted services

builder.Services.AddHostedService<GoogleCalendarSyncWorkerService>();

builder.Services.AddHostedService<AppointmentReminderService>();

builder.Services.AddHostedService<NewsletterReminderService>();


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
        controller = "Planning",
        action = "Index"
    });

app.MapControllerRoute(
    name: "gallery",
    pattern: "gallery",
    defaults: new
    {
        controller = "Home",
        action = "Gallery"
    });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();