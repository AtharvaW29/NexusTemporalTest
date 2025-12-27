using NexusTemporal.Tests.Infrastructure;
using NUnit.Framework;

[SetUpFixture]
public class GlobalTestSetup
{
    private IDisposable? _telemetry;

    [OneTimeSetUp]
    public void Init()
    {
        _telemetry = TelemetrySetup.Initialize();
    }

    [OneTimeTearDown]
    public void Shutdown()
    {
        _telemetry?.Dispose();
        Thread.Sleep(1000);
    }
}
