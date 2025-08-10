using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace BrowserHost.Auth;

public class SsoJsBridge
{
    private static readonly AadTokenProvider _azureSsoHelper = new AadTokenProvider(AuthClients.AzurePortalClientId, AuthClients.AzurePortalScopes);

    public async Task<string> GetAzureAccessToken()
    {
        return await _azureSsoHelper.GetAccessTokenAsync();
    }

    // called from JS: returns a string token
    public async Task<string> GetToken()
    {
        // your MSAL-based method; keep short timeout and cache locally
        return await _azureSsoHelper.GetAccessTokenAsync();
    }

    // optional: return token metadata as JSON
    public async Task<string> GetTokenJson()
    {
        var token = await _azureSsoHelper.GetAccessTokenAsync();
        var expires = _azureSsoHelper.ExpiresOn?.ToUnixTimeMilliseconds() ?? (DateTimeOffset.UtcNow + TimeSpan.FromMinutes(55)).ToUnixTimeMilliseconds();
        return JsonSerializer.Serialize(new { accessToken = token, expiresOn = expires });
    }
}
