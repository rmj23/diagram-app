using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using diagram_app.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Client;

namespace diagram_app.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    private readonly IConfiguration _configuration;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> PrivacyAsync()
    {
        //string[] scopes = _configuration["AzureAd:Scopes"]?.Split(' ') ?? Array.Empty<string>();
        //string accessToken = await _tokenAcquisitionService.AcquireTokenForClientAsync(scopes);

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
