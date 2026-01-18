using BrowserHost.E2E.Infrastructure;
using System.Runtime.CompilerServices;
using Xunit.v3;

[assembly: TestPipelineStartup(typeof(E2ETestPipelineStartup))]
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
[assembly: CaptureConsole]
[assembly: InternalsVisibleTo("BrowserHost.E2E.Manual")]