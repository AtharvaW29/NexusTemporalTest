using Gremlin.Net.Driver;

[TestFixture]
public class SnapshotQueryTests
{
    private GremlinClient _client;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _client = GremlinClientFactory.Create();
        await TestGraphSeeder.SeedAsync(_client);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client?.Dispose();
    }

    [Test]
    public async Task SnapShotQuery_ShouldReturnEdges()
    {
        var result = await _client.SubmitAsync<dynamic>(@"
            g.E()
            .has('valid_from', lte('2025-01-15'))
            .has('valid_to', gt('2025-02-15'))
        ");

        Assert.That(result.Count(), Is.GreaterThan(1));
    }
}