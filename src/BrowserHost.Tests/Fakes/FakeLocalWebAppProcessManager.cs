using BrowserHost.Features.TabPalette.LocalWebApp;
using BrowserHost.Utilities;
using System.Linq;

namespace BrowserHost.Tests.Fakes;

public class FakeLocalWebAppProcessManager : LocalWebAppProcessManager
{
    private readonly PubSub _pubSub;
    private readonly HashSet<string> _runningProcesses = [];
    private readonly HashSet<string> _processesWithErrors = [];

    public FakeLocalWebAppProcessManager(PubSub pubSub) : base(pubSub)
    {
        _pubSub = pubSub;
    }

    public List<(string TabId, LocalWebAppConfigV1 Config)> StartProcessCalls { get; } = [];
    public List<string> StopProcessCalls { get; } = [];
    public int StopAllProcessesCallCount { get; private set; }

    public override bool IsRunning(string tabId) => _runningProcesses.Contains(tabId);

    public override bool HasErrors(string tabId) => _processesWithErrors.Contains(tabId);

    public override void StartProcess(string tabId, LocalWebAppConfigV1 config)
    {
        StartProcessCalls.Add((tabId, config));
        _runningProcesses.Add(tabId);
        _processesWithErrors.Remove(tabId);
        _pubSub.Publish(new LocalWebAppProcessStartedEvent(tabId));
    }

    public override void StopProcess(string tabId)
    {
        StopProcessCalls.Add(tabId);
        _runningProcesses.Remove(tabId);
        _processesWithErrors.Remove(tabId);
        _pubSub.Publish(new LocalWebAppProcessStoppedEvent(tabId));
    }

    public override void StopAllProcesses()
    {
        StopAllProcessesCallCount++;
        foreach (var tabId in _runningProcesses.ToList())
        {
            StopProcess(tabId);
        }
    }

    public void SimulateProcessRunning(string tabId) => _runningProcesses.Add(tabId);

    public void SimulateProcessError(string tabId) => _processesWithErrors.Add(tabId);
}
