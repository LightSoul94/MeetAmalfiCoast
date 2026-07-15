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
    private readonly FirestoreNewsletterService _newsletterService;
    private readonly NewsletterEmailTemplateService _newsletterTemplateService;
    private readonly EmailService _emailService;

    public HomeController(
        ILogger<HomeController> logger,
        IWebHostEnvironment environment,
        IOptions<SmtpSettings> smtp,
        IConfiguration configuration,
        FirestoreNewsletterService newsletterService,
        NewsletterEmailTemplateService newsletterTemplateService,
        EmailService emailService)
    {
        _logger = logger;
        _environment = environment;
        _smtp = smtp.Value;
        _configuration = configuration;
        _newsletterService = newsletterService;
        _newsletterTemplateService = newsletterTemplateService;
        _emailService = emailService;
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

    public IActionResult Gallery()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Terms()
    {
        return View();
    }

    public IActionResult Cookies()
    {
        return View();
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Services()
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

    [HttpPost]
    public async Task<IActionResult> SubscribeNewsletter([FromBody] NewsletterSubscriptionRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            return Ok(new
            {
                success = true,
                message = "Subscription completed successfully."
            });
        }

        if (!request.PrivacyConsent)
        {
            return BadRequest(new
            {
                success = false,
                message = "You must accept the Privacy Policy."
            });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                success = false,
                message = "Please enter a valid email address."
            });
        }

        NewsletterSubscriptionResult result =
            await _newsletterService.SubscribeAsync(request);

        if (!result.Success)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    success = false,
                    message = result.Message
                }
            );
        }

        if (!result.AlreadySubscribed)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(result.UnsubscribeToken))
                {
                    throw new InvalidOperationException(
                        "Token di disiscrizione non generato."
                    );
                }

                string unsubscribeUrl =
                    Url.Action(
                        action: nameof(UnsubscribeNewsletter),
                        controller: "Home",
                        values: new
                        {
                            token = result.UnsubscribeToken
                        },
                        protocol: Request.Scheme
                    )
                    ?? throw new InvalidOperationException(
                        "Impossibile generare il link di disiscrizione."
                    );

                string emailBody =
                    await _newsletterTemplateService
                        .BuildWelcomeEmailAsync(unsubscribeUrl);

                await _emailService.SendAsync(
                    request.Email.Trim(),
                    "Welcome to Meet Amalfi Coast",
                    emailBody
                );

                await _newsletterService
                    .MarkWelcomeEmailAsSentAsync(request.Email);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Errore durante l'invio della mail di benvenuto a {Email}",
                    request.Email
                );
            }
        }

        return Ok(new
        {
            success = true,
            alreadySubscribed = result.AlreadySubscribed,
            message = result.Message
        });
    }

    [HttpGet]
    public async Task<IActionResult> UnsubscribeNewsletter(string token)
    {
        bool deleted =
            await _newsletterService
                .DeleteByUnsubscribeTokenAsync(token);

        if (!deleted)
        {
            return Content(
                """
            The unsubscribe link is invalid or has already been used.
            """,
                "text/plain"
            );
        }

        return Content(
            """
        You have successfully unsubscribed from the Meet Amalfi Coast newsletter.
        """,
            "text/plain"
        );
    }
}