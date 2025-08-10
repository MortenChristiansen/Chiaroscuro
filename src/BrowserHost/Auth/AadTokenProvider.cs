using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Broker;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BrowserHost.Auth;

public class AadTokenProvider(string clientId, string[] scopes)
{
    private readonly IPublicClientApplication _pca =
        PublicClientApplicationBuilder
            .Create(clientId)
            .WithDefaultRedirectUri()
            .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
            .Build();

    private AuthenticationResult? _lastResult;

    public async Task<string> GetAccessTokenAsync()
    {
        // Try silent first
        try
        {
            var accounts = await _pca.GetAccountsAsync();
            var account = accounts.FirstOrDefault();
            _lastResult = await _pca.AcquireTokenSilent(scopes, account)
                                    .ExecuteAsync();
            return _lastResult.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            // Fall back to interactive using OS broker (WAM)
            _lastResult = await _pca.AcquireTokenInteractive(scopes)
                                    //.WithPrompt(Prompt.SelectAccount)
                                    .WithUseEmbeddedWebView(false) // ensure system broker / OS prompt
                                    .ExecuteAsync();
            return _lastResult.AccessToken;
        }
    }

    // Optionally expose expiration time and force refresh
    public DateTimeOffset? ExpiresOn => _lastResult?.ExpiresOn;
}
