using BrowserHost.Features.Terminal;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace BrowserHost.Features.TabPalette.LocalWebApp;

public class LocalWebAppProcessManager(PubSub pubSub) : IDisposable
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, Process> _processes = [];
    private readonly Dictionary<string, bool> _hasErrors = [];
    private readonly Dictionary<string, EventHandler> _exitHandlers = [];
    private bool _disposed;

    public virtual bool IsRunning(string tabId)
    {
        lock (_lock)
        {
            return _processes.TryGetValue(tabId, out var process) && !process.HasExited;
        }
    }

    public virtual bool HasErrors(string tabId)
    {
        lock (_lock)
        {
            return _hasErrors.TryGetValue(tabId, out var hasErrors) && hasErrors;
        }
    }

    public virtual void StartProcess(string tabId, LocalWebAppConfigV1 config)
    {
        lock (_lock)
        {
            if (_disposed) return;

            // Stop existing process if any
            StopProcessInternal(tabId);

            if (string.IsNullOrWhiteSpace(config.DirectoryPath) || !Directory.Exists(config.DirectoryPath))
            {
                _hasErrors[tabId] = true;
                pubSub.Publish(new LocalWebAppProcessErrorEvent(tabId));
                return;
            }

            var startCommand = string.IsNullOrWhiteSpace(config.StartCommand)
                ? "npm start"
                : config.StartCommand;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {startCommand}",
                    WorkingDirectory = config.DirectoryPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        pubSub.Send(new WriteTerminalOutputCommand(tabId, e.Data, IsError: false));
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        lock (_lock)
                        {
                            _hasErrors[tabId] = true;
                        }
                        pubSub.Send(new WriteTerminalOutputCommand(tabId, e.Data, IsError: true));
                        pubSub.Publish(new LocalWebAppProcessErrorEvent(tabId));
                    }
                };

                EventHandler exitedHandler = (_, _) => OnProcessExited(tabId);
                _exitHandlers[tabId] = exitedHandler;
                process.Exited += exitedHandler;

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                _processes[tabId] = process;
                _hasErrors[tabId] = false;

                pubSub.Publish(new LocalWebAppProcessStartedEvent(tabId));
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Debug.WriteLine($"Failed to start process for tab {tabId}: {ex.Message}");
                _hasErrors[tabId] = true;
                pubSub.Publish(new LocalWebAppProcessErrorEvent(tabId));
            }
        }
    }

    public virtual void StopProcess(string tabId)
    {
        lock (_lock)
        {
            StopProcessInternal(tabId);
        }
    }

    private void StopProcessInternal(string tabId)
    {
        if (_processes.TryGetValue(tabId, out var existingProcess))
        {
            if (_exitHandlers.TryGetValue(tabId, out var handler))
            {
                existingProcess.Exited -= handler;
                _exitHandlers.Remove(tabId);
            }

            _processes.Remove(tabId);
            _hasErrors.Remove(tabId);

            try
            {
                if (!existingProcess.HasExited)
                {
                    // Kill the process tree
                    KillProcessTree(existingProcess);
                }
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Debug.WriteLine($"Failed to stop process for tab {tabId}: {ex.Message}");
            }
            finally
            {
                existingProcess.Dispose();
                pubSub.Publish(new LocalWebAppProcessStoppedEvent(tabId));
            }
        }
    }

    private void OnProcessExited(string tabId)
    {
        lock (_lock)
        {
            if (!_processes.TryGetValue(tabId, out var process))
            {
                return;
            }

            _processes.Remove(tabId);
            _hasErrors.Remove(tabId);
            _exitHandlers.Remove(tabId);

            try
            {
                process.Dispose();
            }
            finally
            {
                pubSub.Publish(new LocalWebAppProcessStoppedEvent(tabId));
            }
        }
    }

    public virtual void StopAllProcesses()
    {
        lock (_lock)
        {
            foreach (var tabId in new List<string>(_processes.Keys))
            {
                StopProcessInternal(tabId);
            }
        }
    }

    private static void KillProcessTree(Process process)
    {
        try
        {
            // Use taskkill to kill the process tree on Windows
            using var killer = Process.Start(new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/T /F /PID {process.Id}",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            killer?.WaitForExit(5000);
        }
        catch
        {
            // Fallback: just kill the main process
            try { process.Kill(); } catch { }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopAllProcesses();
        GC.SuppressFinalize(this);
    }
}
