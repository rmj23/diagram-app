using diagram_app;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var instance = builder.Configuration["AzureAd:Instance"];
var tenantId = builder.Configuration["AzureAd:TenantId"];
var clientId = builder.Configuration["AzureAd:ClientId"];
var authority = $"{instance}{tenantId}/v2.0";

builder.Services.AddSingleton<IPublicClientApplication>(PublicClientApplicationBuilder
        .Create(clientId)
        .WithAuthority(authority)
        .WithRedirectUri("http://localhost:5134/Home")
        .Build());

builder.Services.AddSingleton<TokenAcquisitionService>();

var app = builder.Build();


app.UseAuthentication();

app.UseCookieAuthentication(new CookieAuthenticationOptions());
app.UseOpenIdConnectAuthentication(
    new OpenIdConnectAuthenticationOptions
    {
        // Sets the client ID, authority, and redirect URI as obtained from Web.config
        ClientId = clientId,
        Authority = authority,
        RedirectUri = redirectUri,
        // PostLogoutRedirectUri is the page that users will be redirected to after sign-out. In this case, it's using the home page
        PostLogoutRedirectUri = redirectUri,
        Scope = OpenIdConnectScope.OpenIdProfile,
        // ResponseType is set to request the code id_token, which contains basic information about the signed-in user
        ResponseType = OpenIdConnectResponseType.CodeIdToken,
        // ValidateIssuer set to false to allow personal and work accounts from any organization to sign in to your application
        // To only allow users from a single organization, set ValidateIssuer to true and the 'tenant' setting in Web.> config to the tenant name
        // To allow users from only a list of specific organizations, set ValidateIssuer to true and use the ValidIssuers parameter
        TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = false // Simplification (see note below)
        },
        // OpenIdConnectAuthenticationNotifications configures OWIN to send notification of failed authentications to > the OnAuthenticationFailed method
        Notifications = new OpenIdConnectAuthenticationNotifications
        {
            AuthenticationFailed = OnAuthenticationFailed
        }
    }
);

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
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
