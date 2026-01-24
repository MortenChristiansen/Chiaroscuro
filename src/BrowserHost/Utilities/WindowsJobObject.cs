using BrowserHost.Interop;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BrowserHost.Utilities;

/// <summary>
/// This class can be used for registering child processes that get killed along with the main process.
/// </summary>
public sealed class WindowsJobObject : IDisposable
{
    private IntPtr _handle;
    private bool _disposed;

    public WindowsJobObject(string? name = null)
    {
        _handle = WindowsJobObjectInterop.CreateJobObject(IntPtr.Zero, name);
        if (_handle == IntPtr.Zero)
        {
            return;
        }

        var info = new WindowsJobObjectInterop.JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new WindowsJobObjectInterop.JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = WindowsJobObjectInterop.JOBOBJECT_LIMIT_FLAGS.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            }
        };

        var infoLength = Marshal.SizeOf(info);
        var infoPtr = Marshal.AllocHGlobal(infoLength);
        try
        {
            Marshal.StructureToPtr(info, infoPtr, false);
            if (!WindowsJobObjectInterop.SetInformationJobObject(
                    _handle,
                    WindowsJobObjectInterop.JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation,
                    infoPtr,
                    (uint)infoLength))
            {
                WindowsJobObjectInterop.CloseHandle(_handle);
                _handle = IntPtr.Zero;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(infoPtr);
        }
    }

    public bool TryAddProcess(Process process)
    {
        if (_disposed || _handle == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            if (process.HasExited)
            {
                return false;
            }

            return WindowsJobObjectInterop.AssignProcessToJobObject(_handle, process.Handle);
        }
        catch (Exception ex) when (!Debugger.IsAttached)
        {
            Debug.WriteLine($"Failed to assign process {process.Id} to job object: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_handle != IntPtr.Zero)
        {
            WindowsJobObjectInterop.CloseHandle(_handle);
            _handle = IntPtr.Zero;
        }
    }
}
