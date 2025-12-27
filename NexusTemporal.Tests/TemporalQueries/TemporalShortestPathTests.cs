using Gremlin.Net.Driver;
using System.Diagnostics;

[TestFixture]
public class TemporalShortestPathTests
{
    private GremlinClient _client;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _client = GremlinClientFactory.Create();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client?.Dispose();
    }

    // This is the hypothesis test
    [Test]
    public async Task TemporalShortestPath_ShouldCompleteFast()
    {
        var sw = Stopwatch.StartNew();
        string query = @"
        g.V().has('User', 'userId', 'U1')
         .repeat(
            outE('USES_IP')
            .has('valid_from', lte('2025-01-15'))
            .has('valid_to', gt('2025-01-15'))
            .inV()
            .simplePath()
            )
         .until(has('User', 'userId', 'U7'))
         .path()
         .limit(1)";

        var result = await GremlinQueryExecutor.ExecuteAsync(
            _client,
            query,
            queryName: "TemporalShortestPath",
            backend: "baseline"
            );

        sw.Stop();

        TestContext.WriteLine($"Execution time: {sw.ElapsedMilliseconds} ms");

        TemporalMetrics.QueryLatency.Record(sw.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("query", "temporal_shortest_path"),
            new KeyValuePair<string, object?>("backend", "baseline"));

        TemporalMetrics.QueryCount.Add(1);

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(200));
    }
}
