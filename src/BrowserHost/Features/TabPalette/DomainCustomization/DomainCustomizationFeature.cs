using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Tab;
using BrowserHost.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace BrowserHost.Features.TabPalette.DomainCustomization;

public class DomainCustomizationFeature(MainWindow window) : Feature(window)
{
    private string? _currentDomain;
    private TabBrowser? _currentTab;

    public override void Configure()
    {
        PubSub.Subscribe<TabPaletteRequestedEvent>((_) => InitializeDomainSettings());
        PubSub.Subscribe<DomainCustomizationChangedEvent>((e) =>
        {
            var customization = DomainCustomizationStateManager.GetCustomization(e.Domain);
            var updated = customization with { CssEnabled = e.CssEnabled };
            DomainCustomizationStateManager.SaveCustomization(updated);

            // Apply CSS changes to current tab if it's for the current domain
            if (e.Domain == _currentDomain)
            {
                ApplyCssToCurrentTab();
            }

            // Notify frontend about the change
            NotifyFrontendOfDomainUpdate(e.Domain);
        });

        PubSub.Subscribe<DomainCssEditRequestedEvent>((e) => EditDomainCss(e.Domain));

        // Monitor tab changes
        PubSub.Subscribe<TabActivatedEvent>((e) => OnTabChanged());
        PubSub.Subscribe<TabDeactivatedEvent>((e) => OnTabChanged());

        InitializeCurrentTab();
    }

    public void InitializeDomainSettings()
    {
        var domain = GetCurrentDomain();
        if (domain != null)
        {
            var customization = DomainCustomizationStateManager.GetCustomization(domain);
            Window.TabPaletteBrowserControl.InitDomainSettings(domain, customization.CssEnabled, customization.HasCustomCss);
        }
    }

    private void OnTabChanged()
    {
        var newTab = Window.CurrentTab;
        if (newTab != _currentTab)
        {
            // Unsubscribe from old tab's address changes
            if (_currentTab != null)
            {
                _currentTab.AddressChanged -= OnAddressChanged;
            }

            _currentTab = newTab;

            // Subscribe to new tab's address changes
            if (_currentTab != null)
            {
                _currentTab.AddressChanged += OnAddressChanged;
            }

            // Update domain and apply CSS
            UpdateCurrentDomain();
        }
    }

    private void OnAddressChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        UpdateCurrentDomain();
    }

    private void UpdateCurrentDomain()
    {
        var newDomain = GetCurrentDomain();
        if (newDomain != _currentDomain)
        {
            _currentDomain = newDomain;
            ApplyCssToCurrentTab();

            // If tab palette is open, update the domain settings
            NotifyFrontendOfDomainUpdate(newDomain);
        }
    }

    private void InitializeCurrentTab()
    {
        _currentTab = Window.CurrentTab;
        if (_currentTab != null)
        {
            _currentTab.AddressChanged += OnAddressChanged;
        }
        _currentDomain = GetCurrentDomain();
        ApplyCssToCurrentTab();
    }

    private void ApplyCssToCurrentTab()
    {
        var currentTab = Window.CurrentTab;
        if (currentTab == null || _currentDomain == null) return;

        try
        {
            var customization = DomainCustomizationStateManager.GetCustomization(_currentDomain);
            if (customization.CssEnabled && customization.HasCustomCss)
            {
                var css = DomainCustomizationStateManager.GetCustomCss(_currentDomain);
                if (!string.IsNullOrEmpty(css))
                {
                    InjectCssIntoTab(currentTab, css);
                }
            }
            else
            {
                RemoveCssFromTab(currentTab);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to apply CSS to tab for domain {_currentDomain}: {ex.Message}");
        }
    }

    private static void InjectCssIntoTab(TabBrowser tab, string css)
    {
        try
        {
            // Escape CSS for JavaScript injection
            var escapedCss = css.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\n");

            var script = $@"
                (function() {{
                    // Remove existing custom CSS if any
                    const existingStyle = document.getElementById('chiaroscuro-domain-css');
                    if (existingStyle) {{
                        existingStyle.remove();
                    }}
                    
                    // Add new custom CSS
                    const style = document.createElement('style');
                    style.id = 'chiaroscuro-domain-css';
                    style.textContent = '{escapedCss}';
                    document.head.appendChild(style);
                }})();
            ";

            tab.ExecuteScriptAsync(script);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to inject CSS into tab: {ex.Message}");
        }
    }

    private static void RemoveCssFromTab(TabBrowser tab)
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

            tab.ExecuteScriptAsync(script);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to remove CSS from tab: {ex.Message}");
        }
    }

    private void EditDomainCss(string domain)
    {
        try
        {
            var cssPath = DomainCustomizationStateManager.GetCustomCssPath(domain);
            var domainFolder = Path.GetDirectoryName(cssPath)!;

            // Ensure domain folder exists
            Directory.CreateDirectory(domainFolder);

            // Create empty CSS file if it doesn't exist
            if (!File.Exists(cssPath))
            {
                File.WriteAllText(cssPath, $"/* Custom CSS for {domain} */\n\n");
            }

            // Open the CSS file with the default editor
            var processStartInfo = new ProcessStartInfo(cssPath)
            {
                UseShellExecute = true
            };
            Process.Start(processStartInfo);

            // Refresh cache after potential edit
            DomainCustomizationStateManager.RefreshCacheForDomain(domain);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open CSS editor for domain {domain}: {ex.Message}");
        }
    }

    private void NotifyFrontendOfDomainUpdate(string? domain)
    {
        if (domain != null)
        {
            var customization = DomainCustomizationStateManager.GetCustomization(domain);
            Window.TabPaletteBrowserControl.UpdateDomainSettings(domain, customization.CssEnabled, customization.HasCustomCss);
        }
    }

    private string? GetCurrentDomain()
    {
        var currentTab = Window.CurrentTab;
        if (currentTab?.Address == null) return null;

        try
        {
            var uri = new Uri(currentTab.Address);
            return uri.Host;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to extract domain from address {currentTab.Address}: {ex.Message}");
            return null;
        }
    }
}