using BrowserHost.Auth;
using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext;
using BrowserHost.Features.CustomWindowChrome;
using BrowserHost.Features.DragDrop;
using BrowserHost.Features.FileDownloads;
using BrowserHost.Features.TabPalette;
using BrowserHost.Utilities;
using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace BrowserHost.Features.Tabs;

public class TabBrowser : Browser
{
    private readonly ActionContextBrowser _actionContextBrowser;

    public string Id { get; }
    public string? Favicon { get; private set; }
    public string? ManualAddress { get; private set; }

    public TabBrowser(string id, string address, ActionContextBrowser actionContextBrowser, bool setManualAddress, string? favicon)
    {
        Id = id;
        Favicon = favicon;
        SetAddress(address, setManualAddress);

        TitleChanged += OnTitleChanged;
        LoadingStateChanged += OnLoadingStateChanged;

        DisplayHandler = new FaviconDisplayHandler(OnFaviconAddressesChanged);
        _actionContextBrowser = actionContextBrowser;

        var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        DownloadHandler = new DownloadHandler(downloadsPath);
        RequestHandler = new WinAuthHandler(Id);
        FindHandler = new FindHandler();

        BrowserSettings.BackgroundColor = Cef.ColorSetARGB(255, 255, 255, 255);

        JavascriptObjectRepository.Register("ssoBridge", new SsoJsBridge());

        FrameLoadEnd += (sender, args) =>
        {
            if (args.Frame.IsMain && args.Url.Contains("login.microsoftonline.com"))
            {
                args.Frame.ExecuteJavaScriptAsync(@"
            (function() {
  // helper: call native bridge getToken
  async function getNativeToken() {
    if (!window.ssoBridge || !window.ssoBridge.getToken) {
      console.warn('ssoBridge not present');
      return null;
    }
    try {
      // CefSharp async-binding exposes methods lower-cased typically
      const token = await window.ssoBridge.getToken();
      return token;
    } catch (e) {
      console.error('Failed to get native token', e);
      return null;
    }
  }

  function tryPatchMsalInstance(inst) {
    if (!inst || inst._patchedWithSsoBridge) return;
    inst._patchedWithSsoBridge = true;

    // keep original
    const origAcquireTokenSilent = inst.acquireTokenSilent && inst.acquireTokenSilent.bind(inst);

    inst.acquireTokenSilent = async function(request) {
      // Try native token first
      const native = await getNativeToken();
      if (native) {
        // Build a minimal AuthenticationResult expected by msal-browser
        const now = new Date();
        const expiresOn = new Date(now.getTime() + (60 * 60 * 1000)); // 1h
        const result = {
          uniqueId: "",
          tenantId: "", 
          scopes: Array.isArray(request.scopes) ? request.scopes : (request && request.scopes) || [],
          account: inst.getAllAccounts ? (inst.getAllAccounts()[0] || null) : null,
          idToken: null,
          idTokenClaims: null,
          accessToken: native,
          fromCache: false,
          expiresOn: expiresOn,
        };
        return result;
      }
      // fallback to original behavior
      if (origAcquireTokenSilent) {
        return origAcquireTokenSilent(request);
      }
      throw new Error('No acquireTokenSilent available');
    };

    // optionally patch ssoSilent too
    if (inst.ssoSilent) {
      const origSso = inst.ssoSilent.bind(inst);
      inst.ssoSilent = async function(ssoRequest) {
        const native = await getNativeToken();
        if (native) {
          return { accessToken: native, idToken: null, expiresOn: new Date(Date.now()+3600*1000) };
        }
        return origSso(ssoRequest);
      };
    }
    console.log('msal instance patched to use native token');
  }

  // try common global variables where msal instance might live
  try {
    // portal often creates instances; check common places
    if (window.msalInstance) tryPatchMsalInstance(window.msalInstance);
    if (window.msalPublicClient) tryPatchMsalInstance(window.msalPublicClient);
    // try window.msal if it's an object with instances
    if (window.msal) {
      // msal loaded: try to find instances
      try {
        if (typeof window.msal === 'object') {
          // msal-browser keeps apps as variables clients; try common names
          ['pca','app','msalClient','msalInstance'].forEach(name => {
            tryPatchMsalInstance(window[name]);
          });
        }
      } catch(e) {}
    }

    // monkey-patch constructor as last resort (msal-browser exports PublicClientApplication)
    if (window.msal && window.msal.PublicClientApplication) {
      const OrigCtor = window.msal.PublicClientApplication;
      window.msal.PublicClientApplication = function(cfg) {
        const inst = new OrigCtor(cfg);
        setTimeout(()=>tryPatchMsalInstance(inst), 10);
        return inst;
      };
    }
  } catch(e) {
    console.error('msal patch error', e);
  }

  // As another fallback, patch fetch/XHR to inject Authorization header
  (function() {
    const origFetch = window.fetch;
    window.fetch = async function(input, init) {
      const url = (typeof input === 'string') ? input : (input && input.url ? input.url : '');
      if (url && url.indexOf('login.microsoftonline.com')>=0 || url.indexOf('management.azure.com')>=0 || url.indexOf('graph.microsoft.com')>=0) {
        const token = await getNativeToken();
        if (token) {
          init = init || {};
          init.headers = init.headers || {};
          init.headers['Authorization'] = 'Bearer ' + token;
        }
      }
      return origFetch(input, init);
    };

    const xhrProto = XMLHttpRequest.prototype;
    const origOpen = xhrProto.open;
    xhrProto.open = function(method, url) {
      this._url = url;
      return origOpen.apply(this, arguments);
    };
    const origSend = xhrProto.send;
    xhrProto.send = function() {
      try {
        const url = this._url || '';
        if (url.indexOf('login.microsoftonline.com')>=0 || url.indexOf('management.azure.com')>=0) {
          const tokenPromise = getNativeToken();
          // synchronous setRequestHeader not possible if token async — we attempt to block
          // Not ideal: we should prefer fetch override for modern calls
          const self = this;
          tokenPromise.then(token => {
            if (token) self.setRequestHeader('Authorization', 'Bearer ' + token);
            origSend.apply(self, arguments);
          }).catch(()=>origSend.apply(this, arguments));
          return;
        }
      } catch(e) {}
      return origSend.apply(this, arguments);
    };
  })();
})();
        ");
            }
        };
    }

    private void OnTitleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _actionContextBrowser.UpdateTabTitle(Id, (string)e.NewValue);
    }

    private void OnFaviconAddressesChanged(IList<string> addresses)
    {
        Favicon = addresses.FirstOrDefault();
        PubSub.Publish(new TabFaviconUrlChangedEvent(Id, Favicon));
        Dispatcher.BeginInvoke(() => _actionContextBrowser.UpdateTabFavicon(Id, Favicon));
    }

    private void OnLoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
    {
        PubSub.Publish(new TabLoadingStateChangedEvent(Id, e.IsLoading));
    }

    public void SetAddress(string address, bool setManualAddress)
    {
        Address = address;
        if (setManualAddress)
            ManualAddress = address;
    }

    protected override void OnAddressChanged(string oldValue, string newValue)
    {
        if (DragDropFeature.IsDragging && oldValue != null && newValue.StartsWith("file://"))
        {
            // This is a workaround to prevent the current address from being set
            // when dragging and dropping files into the browser. Instead, we want
            // open a new tab with the file URL. This is not directly possible,
            // so we have to revert the change 
            GetBrowser().GoBack();
        }
        else
        {
            base.OnAddressChanged(oldValue, newValue);
        }
    }

    public void RegisterContentPageApi<TApi>(TApi api, string name) where TApi : BrowserApi
    {
        RegisterSecondaryApi(api, name);
    }
}
