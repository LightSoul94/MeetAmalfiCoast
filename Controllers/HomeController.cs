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
            <div style=""font-family: Arial, Helvetica, sans-serif; color:#333; line-height:1.6;"">

                <h2 style=""color:#1f2937; margin-bottom:10px;"">
                    📩 Nuova richiesta di contatto
                </h2>

                <p>
                    Hai ricevuto una nuova richiesta dal modulo contatti del sito
                    <strong>Meet Amalfi Coast</strong>.
                </p>

                <hr style=""border:none; border-top:1px solid #ddd; margin:25px 0;"">

                <table cellpadding=""6"" cellspacing=""0"">
                    <tr>
                        <td><strong>Nome</strong></td>
                        <td>{WebUtility.HtmlEncode(model.Name)}</td>
                    </tr>

                    <tr>
                        <td><strong>Email</strong></td>
                        <td>
                            <a href=""mailto:{WebUtility.HtmlEncode(model.Email)}"">
                                {WebUtility.HtmlEncode(model.Email)}
                            </a>
                        </td>
                    </tr>

                    <tr>
                        <td><strong>Esperienza richiesta</strong></td>
                        <td>{WebUtility.HtmlEncode(model.Service)}</td>
                    </tr>
                </table>

                <h3 style=""margin-top:30px; color:#1f2937;"">
                    Messaggio
                </h3>

                <div style=""
                    background:#f8f9fa;
                    border-left:4px solid #d6ad61;
                    padding:15px;
                    border-radius:4px;"">
                    {WebUtility.HtmlEncode(model.Message).Replace("\n", "<br>")}
                </div>

                <hr style=""border:none; border-top:1px solid #ddd; margin:30px 0 20px 0;"">

                <p style=""font-size:12px; color:#777;"">
                    Questa email è stata inviata automaticamente dal modulo contatti del sito
                    <strong>Meet Amalfi Coast</strong>.<br>
                    Per rispondere al cliente puoi utilizzare direttamente il pulsante
                    <strong>Rispondi</strong> del tuo client di posta oppure scrivere all'indirizzo
                    <a href=""mailto:{WebUtility.HtmlEncode(model.Email)}"">
                        {WebUtility.HtmlEncode(model.Email)}
                    </a>.
                </p>

            </div>";

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