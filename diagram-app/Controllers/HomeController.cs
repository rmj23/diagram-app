using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using diagram_app.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using System.Web;
using Newtonsoft.Json;

namespace diagram_app.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    private readonly IConfiguration _configuration;

    private readonly ITokenAcquisition _tokenAcquisition;

    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly Dictionary<string, TokenModel> s_authorizationRequests = new Dictionary<string, TokenModel>();


    public HomeController(ILogger<HomeController> logger, IConfiguration configuration, ITokenAcquisition tokenAcquisition, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _tokenAcquisition = tokenAcquisition;
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> PrivacyAsync()
    {
        string[] scopes = _configuration["scope"]?.Split(' ') ?? Array.Empty<string>();
        //string accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);
        
        string state = Guid.NewGuid().ToString();

        s_authorizationRequests[state] = new TokenModel() { IsPending = true };

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
                String body = await responseMessage.Content.ReadAsStringAsync();

                TokenModel tokenModel = s_authorizationRequests[state];
                JsonConvert.PopulateObject(body, tokenModel);

                ViewBag.Token = tokenModel;
            }
            else
            {
                error = responseMessage.ReasonPhrase;
            }
        }

        if (!String.IsNullOrEmpty(error))
        {
            ViewBag.Error = error;
        }

        ViewBag.ProfileUrl = _configuration.GetValue<string>("ProfileUrl") ?? string.Empty;

        return View();
    }


    private static bool ValidateCallbackValues(String code, String state, out String error)
    {
        error = null;

        if (String.IsNullOrEmpty(code))
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
                TokenModel tokenModel;
                if (!s_authorizationRequests.TryGetValue(authorizationRequestKey.ToString(), out tokenModel))
                {
                    error = "Unknown authorization request key";
                }
                else if (!tokenModel.IsPending)
                {
                    error = "Authorization request key already used";
                }
                else
                {
                    s_authorizationRequests[authorizationRequestKey.ToString()].IsPending = false; // mark the state value as used so it can't be reused
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
