using Gremlin.Net.Driver;
using System.Diagnostics;

[TestFixture]
public class TemporalPerformancetests
{
    private GremlinClient _client;

    [OneTimeSetUp]
    public void Setup()
    {
        _client = GremlinClientFactory.Create();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
    }

    [Test]
    [Repeat(10)]
    public async Task TemporalQuery_P95_Latency()
    {
        var sw = Stopwatch.StartNew();

        await _client.SubmitAsync<dynamic>(@"
            g.E()
             .has('valid_from', lte('2025-02-01'))
             .has('valid_to', gt('2025-02-01'))
        ");

        sw.Stop();
        TestContext.WriteLine(sw.ElapsedMilliseconds);
    }
}
