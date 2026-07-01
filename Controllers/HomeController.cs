using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MeetAmalfiCoast.Models;

namespace MeetAmalfiCoast.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly SmtpSettings _smtp;
    private readonly IConfiguration _configuration;

    public HomeController(
        ILogger<HomeController> logger,
        IWebHostEnvironment environment,
        IOptions<SmtpSettings> smtp,
        IConfiguration configuration)
    {
        _logger = logger;
        _environment = environment;
        _smtp = smtp.Value;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        var heroPath = Path.Combine(
            _environment.WebRootPath,
            "images",
            "hero"
        );

        var allowedExtensions = new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".mp4"
        };

        var heroMedia = Directory.Exists(heroPath)
            ? Directory.GetFiles(heroPath)
                .Where(file => allowedExtensions.Contains(Path.GetExtension(file).ToLower()))
                .Select(Path.GetFileName)
                .Where(fileName => fileName != null)
                .Select(fileName => fileName!)
                .OrderBy(fileName => fileName)
                .ToList()
            : new List<string>();

        var model = new HomeViewModel
        {
            HeroMedia = heroMedia
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Planning()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SendContactRequest([FromBody] ContactRequestModel model)
    {
        if (model == null ||
            string.IsNullOrWhiteSpace(model.Name) ||
            string.IsNullOrWhiteSpace(model.Email) ||
            string.IsNullOrWhiteSpace(model.Service) ||
            string.IsNullOrWhiteSpace(model.Message))
        {
            return Json(new
            {
                success = false,
                message = "Please fill in all required fields."
            });
        }

        try
        {
            var body = $@"
            <h2>New Meet Amalfi Coast request</h2>

            <p><strong>Name:</strong> {WebUtility.HtmlEncode(model.Name)}</p>
            <p><strong>Email:</strong> {WebUtility.HtmlEncode(model.Email)}</p>
            <p><strong>Experience:</strong> {WebUtility.HtmlEncode(model.Service)}</p>

            <p><strong>Message:</strong></p>
            <p>{WebUtility.HtmlEncode(model.Message).Replace("\n", "<br>")}</p>
        ";

            using var message = new MailMessage();

            message.From = new MailAddress(_smtp.From);
            message.To.Add(_smtp.To);
            message.ReplyToList.Add(new MailAddress(model.Email));

            message.Subject = $"Meet Amalfi Coast - {model.Name}";
            message.Body = body;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                EnableSsl = _smtp.EnableSsl,
                Credentials = new NetworkCredential(
                    _smtp.Username,
                    _smtp.Password
                )
            };

            await client.SendMailAsync(message);

            return Json(new
            {
                success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore invio mail contatto");

            return Json(new
            {
                success = false,
                message = "Unable to send your request. Please try again later."
            });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetGoogleReviews()
    {
        var apiKey = _configuration["GooglePlaces:ApiKey"];
        var placeId = _configuration["GooglePlaces:PlaceId"];

        if (string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(placeId))
        {
            return Json(new GoogleReviewsViewModel());
        }

        using var client = new HttpClient();

        client.DefaultRequestHeaders.Add("X-Goog-Api-Key", apiKey);
        client.DefaultRequestHeaders.Add(
            "X-Goog-FieldMask",
            "displayName,rating,userRatingCount,reviews"
        );

        var response = await client.GetAsync(
            $"https://places.googleapis.com/v1/places/{placeId}"
        );

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return Json(new GoogleReviewsViewModel());
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var model = new GoogleReviewsViewModel
        {
            Name = root.TryGetProperty("displayName", out var displayName) &&
                   displayName.TryGetProperty("text", out var name)
                ? name.GetString() ?? ""
                : "",

            Rating = root.TryGetProperty("rating", out var rating)
                ? rating.GetDouble()
                : 0,

            UserRatingCount = root.TryGetProperty("userRatingCount", out var count)
                ? count.GetInt32()
                : 0
        };

        if (root.TryGetProperty("reviews", out var reviews))
        {
            foreach (var review in reviews.EnumerateArray())
            {
                model.Reviews.Add(new GoogleReviewViewModel
                {
                    Author = review.TryGetProperty("authorAttribution", out var author) &&
                             author.TryGetProperty("displayName", out var authorName)
                        ? authorName.GetString() ?? "Google user"
                        : "Google user",

                    Rating = review.TryGetProperty("rating", out var reviewRating)
                        ? reviewRating.GetDouble()
                        : 0,

                    Text = review.TryGetProperty("text", out var text) &&
                           text.TryGetProperty("text", out var reviewText)
                        ? reviewText.GetString() ?? ""
                        : ""
                });
            }
        }

        return Json(model);
    }
}