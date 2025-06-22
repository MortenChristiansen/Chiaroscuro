using System.Diagnostics;
using System.Windows.Input;

namespace BrowserHost.Features;

public class ActionDialogFeature(MainWindow window, BrowserApi api)
{
    public void Register()
    {
        window.ActionDialog.Address = ContentServer.GetUiAddress("/action-dialog");
        window.ActionDialog.ConsoleMessage += (sender, e) =>
        {
            Debug.WriteLine($"ActionDialog: {e.Message}");
        };
        window.ActionDialog.JavascriptObjectRepository.Register("api", api);
    }

    public bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.T && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            ShowDialog();
            e.Handled = true;
            return true;
        }
        return false;
    }

    private void ShowDialog()
    {
        api.ShowActionDialog();
    }
}
