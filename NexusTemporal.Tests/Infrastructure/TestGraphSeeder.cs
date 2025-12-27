using Gremlin.Net.Driver;

public static class TestGraphSeeder
{
    public static async Task SeedAsync(GremlinClient client)
    {
        // New Graph
        await client.SubmitAsync<dynamic>("g.V().drop()");

        // Creating 50 Sample Users
        for (int i = 0; i < 50; i++)
        {
            await client.SubmitAsync<dynamic>(
                $"g.addV('User').property('userId', 'U{i}')");
        }

        // Create IPs
        for (int i = 0; i < 20; i++)
        {
            await client.SubmitAsync<dynamic>(
                $"g.addV('IP').property('ipId', 'IP_{i}')");
        }

        // Temporal Edges
        for (int i = 0; i < 5000; i++)
        {
            await client.SubmitAsync<dynamic>($@"
                 g.V().has('User', 'userId', 'U{i % 50}')
                .as('u')
                .V().has('IP', 'ipId', 'IP_{i % 20}')
                .addE('USES_IP')
                .from('u')
                .property('valid_from', '2025-01-01')
                .property('valid_to', '2025-03-01')"
                );
        }
    }
}
