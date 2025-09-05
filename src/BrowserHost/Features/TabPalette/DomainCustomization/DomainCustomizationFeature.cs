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
    private FileSystemWatcher? _cssFileWatcher;
    private string? _watchedCssPath;

    public override void Configure()
    {
        PubSub.Subscribe<TabPaletteRequestedEvent>((_) => InitializeDomainSettings());
        PubSub.Subscribe<DomainCustomizationChangedEvent>((e) =>
        {
            var customization = DomainCustomizationStateManager.GetCustomization(e.Domain);
            var updated = customization with { CssEnabled = e.CssEnabled };
            DomainCustomizationStateManager.SaveCustomization(updated);

            if (e.Domain == _currentDomain)
            {
                ApplyCssToCurrentTab();
            }

            NotifyFrontendOfDomainUpdate(e.Domain);
        });

        PubSub.Subscribe<DomainCssEditRequestedEvent>((e) => EditDomainCss(e.Domain));

        PubSub.Subscribe<TabActivatedEvent>((e) => OnTabChanged());
        PubSub.Subscribe<TabDeactivatedEvent>((e) => OnTabChanged());
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
        var newDomain = GetCurrentDomain();
        var domainChanged = newDomain != _currentDomain;

        _currentDomain = newDomain;

        ApplyCssToCurrentTab();

        if (domainChanged)
        {
            UpdateCssFileWatcher();
            NotifyFrontendOfDomainUpdate(newDomain);
        }
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
                    const cssId = 'chiaroscuro-domain-css';
                    const expectedCss = '{escapedCss}';
                    
                    // Check if the CSS is already correctly applied
                    const existingStyle = document.getElementById(cssId);
                    if (existingStyle && existingStyle.textContent === expectedCss) {{
                        // CSS is already correctly applied, no need to update
                        return;
                    }}
                    
                    // Remove existing custom CSS if any
                    if (existingStyle) {{
                        existingStyle.remove();
                    }}
                    
                    // Add new custom CSS
                    const style = document.createElement('style');
                    style.id = cssId;
                    style.textContent = expectedCss;
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

            Directory.CreateDirectory(domainFolder);
            var isNewCssFile = !File.Exists(cssPath);

            if (isNewCssFile)
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

            UpdateCssFileWatcher();
            NotifyFrontendOfDomainUpdate(domain);
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

    private void UpdateCssFileWatcher()
    {
        _cssFileWatcher?.Dispose();
        _cssFileWatcher = null;
        _watchedCssPath = null;

        if (_currentDomain == null) return;

        try
        {
            var cssPath = DomainCustomizationStateManager.GetCustomCssPath(_currentDomain);
            var directory = Path.GetDirectoryName(cssPath);
            var fileName = Path.GetFileName(cssPath);

            if (directory != null && Directory.Exists(directory))
            {
                _cssFileWatcher = new FileSystemWatcher(directory, fileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };

                _cssFileWatcher.Changed += OnCssFileChanged;
                _watchedCssPath = cssPath;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to setup CSS file watcher for domain {_currentDomain}: {ex.Message}");
        }
    }

    private void OnCssFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (_currentDomain == null || e.FullPath != _watchedCssPath) return;

            // Small delay to ensure file write is complete
            System.Threading.Thread.Sleep(100);

            // Refresh cache and reapply CSS
            DomainCustomizationStateManager.RefreshCacheForDomain(_currentDomain);

            // Apply CSS to current tab on UI thread
            Window.Dispatcher.Invoke(() =>
            {
                ApplyCssToCurrentTab();
                NotifyFrontendOfDomainUpdate(_currentDomain);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to handle CSS file change: {ex.Message}");
        }
    }
}