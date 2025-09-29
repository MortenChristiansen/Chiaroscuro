using CefSharp;
using System;
using System.Collections.Generic;
using System.Windows;

namespace BrowserHost.Features.Permissions;

public class CefSharpPermissionHandler : CefSharp.Handler.PermissionHandler
{
    protected override bool OnShowPermissionPrompt(IWebBrowser chromiumWebBrowser, IBrowser browser, ulong promptId, string requestingOrigin, PermissionRequestType requestedPermissions, IPermissionPromptCallback callback)
    {
        return HandleGenericPermissionRequest(requestingOrigin, requestedPermissions, callback);
    }

    protected override bool OnRequestMediaAccessPermission(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string requestingOrigin, MediaAccessPermissionType requestedPermissions, IMediaAccessCallback callback)
    {
        return false; // default handling
    }

    private static bool HandleGenericPermissionRequest(string requestingOrigin, PermissionRequestType requestedPermissions, IPermissionPromptCallback callback)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var origin = Uri.TryCreate(requestingOrigin, UriKind.Absolute, out var parsed)
                ? parsed.GetLeftPart(UriPartial.Authority)
                : requestingOrigin;

            var permissionDisplay = BuildPermissionDisplay(requestedPermissions);
            var accepted = GenericPermissionDialog.ShowDialog(MainWindow.Instance, origin, permissionDisplay);
            callback.Continue(accepted ? PermissionRequestResult.Accept : PermissionRequestResult.Deny);
        });
        return true; // async
    }

    private static string BuildPermissionDisplay(PermissionRequestType requestedPermissions)
    {
        if (requestedPermissions == PermissionRequestType.None)
            return "None";

        var names = new List<string>();
        foreach (PermissionRequestType flag in Enum.GetValues(typeof(PermissionRequestType)))
        {
            if (flag == PermissionRequestType.None) continue;
            if (requestedPermissions.HasFlag(flag)) names.Add(ToFriendly(flag));
        }
        return string.Join(", ", names);
    }

    private static string ToFriendly(PermissionRequestType type) => type switch
    {
        PermissionRequestType.AccessibilityEvents => "Accessibility Events",
        PermissionRequestType.ArSession => "AR Session",
        PermissionRequestType.CameraPanTiltZoom => "Camera Pan/Tilt/Zoom",
        PermissionRequestType.CameraStream => "Camera Stream",
        PermissionRequestType.CapturedSurfaceControl => "Captured Surface Control",
        PermissionRequestType.Clipboard => "Clipboard",
        PermissionRequestType.TopLevelStorageAccess => "Top-Level Storage Access",
        PermissionRequestType.DiskQuota => "Disk Quota",
        PermissionRequestType.LocalFonts => "Local Fonts",
        PermissionRequestType.Geolocation => "Geolocation",
        PermissionRequestType.Identity_Provider => "Identity Provider",
        PermissionRequestType.IdleDetection => "Idle Detection",
        PermissionRequestType.MicStream => "Microphone Stream",
        PermissionRequestType.MidiSysex => "MIDI (Sysex)",
        PermissionRequestType.MultipleDownloads => "Multiple Downloads",
        PermissionRequestType.Notifications => "Notifications",
        PermissionRequestType.KeyboardLock => "Keyboard Lock",
        PermissionRequestType.PointerLock => "Pointer Lock",
        PermissionRequestType.ProtectedMediaIdentifier => "Protected Media Identifier",
        PermissionRequestType.RegisterProtocolHandler => "Register Protocol Handler",
        PermissionRequestType.StorageAccess => "Storage Access",
        PermissionRequestType.VrSession => "VR Session",
        PermissionRequestType.WindowManagement => "Window Management",
        PermissionRequestType.FileSystemAccess => "File System Access",
        _ => type.ToString()
    };
}
