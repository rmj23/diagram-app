using Microsoft.Identity.Client;
using System.Threading.Tasks;

public class TokenAcquisitionService
{
    private readonly IPublicClientApplication _publicClientApplication;

    public TokenAcquisitionService(IPublicClientApplication publicClientApplication)
    {
        _publicClientApplication = publicClientApplication;
    }

    public async Task<string> AcquireTokenForClientAsync(string[] scopes)
    {
        AuthenticationResult result;
        try
        {
            var accounts = await _publicClientApplication.GetAccountsAsync();
            result = await _publicClientApplication.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                    .ExecuteAsync();
        }
        catch (MsalUiRequiredException ex)
        {
            // If the token has expired, prompt the user with a login prompt
            result = await _publicClientApplication.AcquireTokenInteractive(scopes)
                    .WithClaims(ex.Claims)
                    .ExecuteAsync();
        }

        return result.AccessToken;
    }
}
