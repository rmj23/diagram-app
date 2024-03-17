using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using diagram_app.Models;
using System.Web;
using Newtonsoft.Json;
using System.Xml.Schema;

namespace diagram_app.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    private readonly IConfiguration _configuration;

    private readonly IHttpClientFactory _httpClientFactory;

    private readonly WebAppDbContext _context;

    public HomeController(ILogger<HomeController> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory, WebAppDbContext context)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> PrivacyAsync()
    {
        string[] scopes = _configuration["scope"]?.Split(' ') ?? Array.Empty<string>();
        
        string state = Guid.NewGuid().ToString();

        var token = new ExternalServiceToken()
        {
            IsPending = true,
            State = state
        };

        _context.ExternalServiceToken.Add(token);
        _context.SaveChanges();

        UriBuilder uriBuilder = new UriBuilder(_configuration.GetValue<string>("AuthUrl") ?? string.Empty);

        var queryParams = HttpUtility.ParseQueryString(uriBuilder.Query ?? String.Empty);

        queryParams["client_id"] = _configuration.GetValue<string>("ClientAppId") ?? string.Empty;
        queryParams["response_type"] = "Assertion";
        queryParams["state"] = state;
        queryParams["scope"] = _configuration.GetValue<string>("Scope") ?? string.Empty;
        queryParams["redirect_uri"] = _configuration.GetValue<string>("CallbackUrl") ?? string.Empty;

        uriBuilder.Query = queryParams.ToString();

        return Redirect(uriBuilder.ToString());
    }

    public async Task<IActionResult> CallbackAsync(string code, string state)
    {
        string error;

        if (ValidateCallbackValues(code, state.ToString(), out error))
        {
            // Exchange the auth code for an access token and refresh token
            HttpRequestMessage requestMessage =
                new HttpRequestMessage(HttpMethod.Post, _configuration.GetValue<string>("TokenUrl"));
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            Dictionary<string, string> form = new Dictionary<string, string>()
            {
                { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
                { "client_assertion", _configuration.GetValue<string>("ClientAppSecret") ?? string.Empty },
                { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                { "assertion", code },
                { "redirect_uri", _configuration.GetValue<string>("CallbackUrl") ?? string.Empty }
            };
            requestMessage.Content = new FormUrlEncodedContent(form);

            var httpClient = _httpClientFactory.CreateClient();

            HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);

            if (responseMessage.IsSuccessStatusCode)
            {
                string body = await responseMessage.Content.ReadAsStringAsync();

                var tokenModel = new TokenModel();
                JsonConvert.PopulateObject(body, tokenModel);

                var token = _context.ExternalServiceToken.FirstOrDefault(x => x.State == state);

                if (token is null)
                {
                    error = "No token found for state";
                }
                else 
                {
                    token.AccessToken = tokenModel.AccessToken;
                    token.TokenType = tokenModel.TokenType;
                    token.RefreshToken = tokenModel.RefreshToken;
                    token.ExpiresIn = tokenModel.ExpiresIn;
                    _context.SaveChanges();

                    ViewBag.Token = tokenModel;
                }
            }
            else
            {
                error = responseMessage.ReasonPhrase;
            }
        }

        if (!string.IsNullOrEmpty(error))
        {
            ViewBag.Error = error;
        }

        ViewBag.ProfileUrl = _configuration.GetValue<string>("ProfileUrl") ?? string.Empty;

        return View();
    }


    private bool ValidateCallbackValues(string code, string state, out string error)
    {
        error = null;

        if (string.IsNullOrEmpty(code))
        {
            error = "Invalid auth code";
        }
        else
        {
            Guid authorizationRequestKey;
            if (!Guid.TryParse(state, out authorizationRequestKey))
            {
                error = "Invalid authorization request key";
            }
            else
            {
                var token = _context.ExternalServiceToken.FirstOrDefault(x => x.State == authorizationRequestKey.ToString());

                if (token is null)
                {
                    error = "Unknown authorization request key";
                }
                else if (!token.IsPending)
                {
                    error = "Authorization request key already used";
                }
                else
                {
                    // mark the state value as used so it can't be reused
                    token.IsPending = false;
                    _context.SaveChanges();
                }
            }
        }

        return error == null;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
