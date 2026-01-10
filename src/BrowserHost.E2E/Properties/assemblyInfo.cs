using BrowserHost.E2E.Infrastructure;
using Xunit.v3;

[assembly: TestPipelineStartup(typeof(E2EContentServerFixture))]
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]