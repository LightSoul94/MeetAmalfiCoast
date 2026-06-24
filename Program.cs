// Crea il builder per la configurazione dell'applicazione web
var builder = WebApplication.CreateBuilder(args);

// Registra i servizi MVC
builder.Services.AddControllersWithViews();

// Configura Google Calendar da appsettings / user-secrets / variabili d'ambiente
builder.Services.Configure<GoogleCalendarSettings>(
    builder.Configuration.GetSection("GoogleCalendar"));

builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("Smtp"));

// Registra i servizi applicativi
builder.Services.AddSingleton<GoogleCalendarService>();
builder.Services.AddSingleton<FirestorePlanningService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
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