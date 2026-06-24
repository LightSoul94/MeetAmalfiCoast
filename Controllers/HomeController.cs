using Microsoft.Extensions.Options;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MeetAmalfiCoasts.Models;
using System.Net;
using System.Net.Mail;

namespace MeetAmalfiCoasts.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly SmtpSettings _smtp;

    public HomeController(
        ILogger<HomeController> logger,
        IWebHostEnvironment environment,
        IOptions<SmtpSettings> smtp)
    {
        _logger = logger;
        _environment = environment;
        _smtp = smtp.Value;
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
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
			message = ex.Message,
			detail = ex.InnerException?.Message
		});
	}
}
}