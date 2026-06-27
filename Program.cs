using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Registra i servizi di configurazione
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
    // Registra il servizio di Firestore Planning
builder.Services.AddSingleton<FirestorePlanningService>();
    // Registra il servizio di Stripe
builder.Services.AddSingleton<StripeService>();

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
        controller = "Home",
        action = "Planning"
    });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();
