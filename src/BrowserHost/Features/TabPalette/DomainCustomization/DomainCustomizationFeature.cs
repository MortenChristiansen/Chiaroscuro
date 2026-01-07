using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Tab;
using BrowserHost.Utilities;
using System;
using System.Diagnostics;
using System.Windows;

namespace BrowserHost.Features.TabPalette.DomainCustomization;

public class DomainCustomizationFeature : Feature
{
    private readonly IBrowserContext _browserContext;
    private readonly DomainCustomizationBrowserApi _domainCustomizationApi;
    private readonly DomainCustomizationStateManager _stateManager;

    private string? _currentDomain;
    private ITabBrowser? _currentTab;
    private IDisposable? _cssWatcherSubscription;

    public DomainCustomizationFeature(
        PubSub pubSub,
        IBrowserContext browserContext,
        DomainCustomizationBrowserApi domainCustomizationApi,
        DomainCustomizationStateManager stateManager) : base(pubSub)
    {
        _browserContext = browserContext ?? throw new ArgumentNullException(nameof(browserContext));
        _domainCustomizationApi = domainCustomizationApi ?? throw new ArgumentNullException(nameof(domainCustomizationApi));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
    }

    public override void Configure()
    {
        PubSub.Handle<ChangeDomainCustomizationCommand>(cmd =>
        {
            var customization = _stateManager.GetCustomization(cmd.Domain);
            var updated = customization with { CssEnabled = cmd.CssEnabled };
            _stateManager.SaveCustomization(updated);

            if (cmd.Domain == _currentDomain)
            {
                ApplyCssToCurrentTab();
            }

            NotifyFrontendOfDomainUpdate(cmd.Domain);
            PubSub.Publish(new DomainCustomizationChangedEvent(cmd.Domain, cmd.CssEnabled));
        });
        PubSub.Handle<RemoveDomainCustomCssCommand>(cmd =>
        {
            // Note that this command can be triggered manually by the frontend or by deleting the file directly.
            // Clearing the watcher will prevent a second signal as we delete the CSS file below.
            if (cmd.Domain == _currentDomain)
                ClearCssWatcher();

            _stateManager.RemoveCustomCss(cmd.Domain);

            var tab = _browserContext.CurrentTab;
            if (tab != null && cmd.Domain == _currentDomain)
                RemoveCssFromTab(tab);

            var customization = _stateManager.GetCustomization(cmd.Domain);
            if (customization.CssEnabled)
                PubSub.Send(new ChangeDomainCustomizationCommand(cmd.Domain, CssEnabled: false));

            PubSub.Publish(new DomainCustomCssRemovedEvent(cmd.Domain));
        });
        PubSub.Handle<EditDomainCssCommand>(cmd =>
        {
            EditDomainCss(cmd.Domain);
            PubSub.Publish(new DomainCssEditRequestedEvent(cmd.Domain));
        });

        PubSub.Subscribe<TabPaletteRequestedEvent>((_) => InitializeDomainSettings());
        PubSub.Subscribe<TabActivatedEvent>((e) => OnTabChanged());
        PubSub.Subscribe<TabDeactivatedEvent>((e) => OnTabChanged());
    }

    public void InitializeDomainSettings()
    {
        var domain = _browserContext.CurrentTab?.CurrentDomain;
        if (domain != null)
        {
            var customization = _stateManager.GetCustomization(domain);
            _domainCustomizationApi.InitDomainSettings(domain, customization.CssEnabled, customization.HasCustomCss);
        }
    }

    private void OnTabChanged()
    {
        var newTab = _browserContext.CurrentTab;
        if (newTab != _currentTab)
        {
            if (_currentTab != null)
            {
                _currentTab.AddressChanged -= OnAddressChanged;
            }

            _currentTab = newTab;

            if (_currentTab != null)
            {
                _currentTab.AddressChanged += OnAddressChanged;
            }

            UpdateCurrentDomain();
        }
    }

    private void OnAddressChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateCurrentDomain();
    }

    private void UpdateCurrentDomain()
    {
        var newDomain = _browserContext.CurrentTab?.CurrentDomain;
        var domainChanged = newDomain != _currentDomain;

        _currentDomain = newDomain;

        ApplyCssToCurrentTab();

        if (domainChanged)
        {
            UpdateCssWatcher();
            NotifyFrontendOfDomainUpdate(newDomain);
        }
    }

    private void ApplyCssToCurrentTab()
    {
        var currentTab = _browserContext.CurrentTab;
        if (currentTab == null || _currentDomain == null) return;

        var customization = _stateManager.GetCustomization(_currentDomain);
        if (customization.CssEnabled && customization.HasCustomCss)
        {
            var css = _stateManager.GetCustomCss(_currentDomain);
            if (!string.IsNullOrEmpty(css))
            {
                InjectCssIntoTab(currentTab, css);
                return;
            }
        }

        RemoveCssFromTab(currentTab);
    }

    private static void InjectCssIntoTab(ITabBrowser tab, string css)
    {
        try
        {
            // Escape CSS for JavaScript injection
            var escapedCss = css.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n");

            var script = $@"
                (function() {{
                    const cssId = 'chiaroscuro-domain-css';
                    const expectedCss = '{escapedCss}';
                    let tries = 0;
                    const maxTries = 50; // ~5s with 100ms intervals

                    function applyCss() {{
                        const head = document.head || document.getElementsByTagName('head')[0];
                        if (!head) {{
                            if (tries++ < maxTries) {{
                                setTimeout(applyCss, 100);
                            }}
                            return;
                        }}

                        const existingStyle = document.getElementById(cssId);
                        if (existingStyle && existingStyle.textContent === expectedCss) {{
                            // Already applied
                            return;
                        }}

                        // Remove old if present
                        if (existingStyle) {{
                            existingStyle.remove();
                        }}

                        // Add new custom CSS
                        const style = document.createElement('style');
                        style.id = cssId;
                        style.textContent = expectedCss;
                        head.appendChild(style);
                    }}

                    if (document.readyState === 'loading') {{
                        // Ensure we try again after DOM is parsed
                        document.addEventListener('DOMContentLoaded', applyCss, {{ once: true }});
                    }}

                    // Initial attempt (and retries if needed)
                    applyCss();
                }})();
            ";

            _ = tab.ExecuteScriptAsync(script);
        }
        catch (Exception ex) when (!Debugger.IsAttached)
        {
            Debug.WriteLine($"Failed to inject CSS into tab: {ex.Message}");
        }
    }

    private static void RemoveCssFromTab(ITabBrowser tab)
    {
        try
        {
            var script = @"
                (function() {
                    const existingStyle = document.getElementById('chiaroscuro-domain-css');
                    if (existingStyle) {
                        existingStyle.remove();
                    }
                })();
            ";

            _ = tab.ExecuteScriptAsync(script);
        }
        catch (Exception ex) when (!Debugger.IsAttached)
        {
            Debug.WriteLine($"Failed to remove CSS from tab: {ex.Message}");
        }
    }

    private void EditDomainCss(string domain)
    {
        if (!_stateManager.EnsureCustomCssExistsAndOpenInEditor(domain))
            return;

        UpdateCssWatcher();
        NotifyFrontendOfDomainUpdate(domain);

        var customization = _stateManager.GetCustomization(domain);
        _stateManager.SaveCustomization(customization with { CssEnabled = true });
    }

    private void NotifyFrontendOfDomainUpdate(string? domain)
    {
        if (domain != null)
        {
            var customization = _stateManager.GetCustomization(domain);
            _domainCustomizationApi.UpdateDomainSettings(domain, customization.CssEnabled, customization.HasCustomCss);
        }
    }

    private void UpdateCssWatcher()
    {
        ClearCssWatcher();

        if (_currentDomain == null)
            return;

        _cssWatcherSubscription = _stateManager.WatchCustomCss(_currentDomain, OnCustomCssWatchEvent);
    }

    private void ClearCssWatcher()
    {
        _cssWatcherSubscription?.Dispose();
        _cssWatcherSubscription = null;
    }

    private void OnCustomCssWatchEvent(DomainCustomizationStateManager.CustomCssWatchEventKind kind)
    {
        if (_currentDomain == null)
            return;

        if (kind == DomainCustomizationStateManager.CustomCssWatchEventKind.Removed)
        {
            PubSub.Send(new RemoveDomainCustomCssCommand(_currentDomain));
            return;
        }

        _stateManager.RefreshCacheForDomain(_currentDomain);

        if (_browserContext.ActionRequiresDispatch)
        {
            _browserContext.Dispatch(() =>
            {
                ApplyCssToCurrentTab();
                NotifyFrontendOfDomainUpdate(_currentDomain);
            });

            return;
        }

        ApplyCssToCurrentTab();
        NotifyFrontendOfDomainUpdate(_currentDomain);
    }
}