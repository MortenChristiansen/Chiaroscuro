using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;
using System;
using System.Windows.Input;

namespace BrowserHost.Features.Terminal;

public class TerminalFeature : Feature
{
    private readonly IBrowserContext _browserContext;
    private readonly TerminalBrowserApi _browserApi;
    private readonly TerminalWindowOperations _windowOperations;

    public TerminalFeature(
        PubSub pubSub,
        IBrowserContext browserContext,
        TerminalBrowserApi browserApi,
        TerminalWindowOperations windowOperations) : base(pubSub)
    {
        _browserContext = browserContext ?? throw new ArgumentNullException(nameof(browserContext));
        _browserApi = browserApi ?? throw new ArgumentNullException(nameof(browserApi));
        _windowOperations = windowOperations ?? throw new ArgumentNullException(nameof(windowOperations));
    }

    public override void Configure()
    {
        // Toggle command
        PubSub.Handle<ToggleTerminalCommand>(_ => ToggleTerminal());
        PubSub.Handle<ClearTerminalCommand>(cmd => _browserApi.ClearTerminal(cmd.TabId));
        PubSub.Handle<WriteTerminalOutputCommand>(cmd => _browserApi.WriteOutput(cmd.TabId, cmd.Output, cmd.IsError));

        // Initialize terminal for new tabs
        PubSub.Subscribe<TabActivatedEvent>(e =>
        {
            _browserApi.InitTerminal(e.TabId);
        });
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (ShouldIgnoreKey(e))
        {
            return false;
        }

        // Handle the '½' key
        if (e.Key == Key.Oem5)
        {
            // Check for ½ key (no modifiers)
            if (_browserContext.CurrentKeyboardModifiers == ModifierKeys.None)
            {
                ToggleTerminal();
                return true;
            }
        }

        return false;
    }

    // Prevent toggling the terminal when focus is inside text inputs or embedded web content.
    private static bool ShouldIgnoreKey(KeyEventArgs e)
    {
        return e.OriginalSource is System.Windows.Controls.Primitives.TextBoxBase
            or System.Windows.Controls.PasswordBox
            or System.Windows.Controls.ComboBox
            or System.Windows.Interop.HwndHost;
    }

    private void ToggleTerminal()
    {
        if (_windowOperations.IsTerminalVisible)
        {
            _windowOperations.HideTerminal();
            _browserApi.SetVisibility(false);
        }
        else
        {
            _windowOperations.ShowTerminal();
            _browserApi.SetVisibility(true);

            // Initialize with current tab
            var currentTabId = _browserContext.CurrentTab?.Id;
            if (currentTabId != null)
            {
                _browserApi.InitTerminal(currentTabId);
            }
        }

        PubSub.Publish(new TerminalToggledEvent(_windowOperations.IsTerminalVisible));
    }
}
