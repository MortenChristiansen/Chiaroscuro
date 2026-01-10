using BrowserHost.E2E.Infrastructure;
using Xunit.v3;

[assembly: TestPipelineStartup(typeof(E2ETestPipelineStartup))]
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
[assembly: CaptureConsole]