using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MeetAmalfiCoasts.Models;

namespace MeetAmalfiCoasts.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _environment;

    public HomeController(
        ILogger<HomeController> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
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
}