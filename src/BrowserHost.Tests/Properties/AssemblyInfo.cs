using BrowserHost.Tests.Infrastructure;
using Xunit.v3;

[assembly: TestPipelineStartup(typeof(TestPipelineStartup))]
[assembly: PerTestPubSubContextAttribute]