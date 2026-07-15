using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

// Crea il builder per la configurazione dell'applicazione web
var builder = WebApplication.CreateBuilder(args);

// Registra i servizi MVC
builder.Services.AddControllersWithViews();


// Configurazioni

builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("Smtp"));

builder.Services.Configure<StripeSettings>(
    builder.Configuration.GetSection("Stripe"));

builder.Services.Configure<BookingSettings>(
    builder.Configuration.GetSection("Booking"));

// Registra i servizi applicativi

builder.Services.AddSingleton<FirestorePlanningService>();

builder.Services.AddSingleton<FirestoreNewsletterService>();

builder.Services.AddSingleton<NewsletterEmailTemplateService>();

builder.Services.AddSingleton<StripeService>();

builder.Services.AddSingleton<EmailService>();


// Hosted services

builder.Services.AddHostedService<AppointmentReminderService>();

builder.Services.AddHostedService<NewsletterReminderService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
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
