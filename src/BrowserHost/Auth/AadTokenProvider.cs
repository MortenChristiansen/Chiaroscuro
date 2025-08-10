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
            .WithBroker(new BrokerOptions(BrokerOptions.OperatingSystems.Windows))
            .Build();
    private AuthenticationResult? _lastResult;

    public async Task<string> GetAccessTokenAsync()
    {
        // Try silent first
        try
        {
            var accounts = await _pca.GetAccountsAsync().ConfigureAwait(false);
            var account = accounts.FirstOrDefault();
            _lastResult = await _pca.AcquireTokenSilent(scopes, account)
                                    .ExecuteAsync().ConfigureAwait(false);
            return _lastResult.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            // Fall back to interactive using OS broker (WAM)
            _lastResult = await _pca.AcquireTokenInteractive(scopes)
                                    .WithUseEmbeddedWebView(false) // ensure system broker / OS prompt
                                    .ExecuteAsync().ConfigureAwait(false);
            return _lastResult.AccessToken;
        }
    }

    // Optionally expose expiration time and force refresh
    public DateTimeOffset? ExpiresOn => _lastResult?.ExpiresOn;
}
