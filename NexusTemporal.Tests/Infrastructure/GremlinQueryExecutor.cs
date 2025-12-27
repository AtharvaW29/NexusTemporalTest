using System;
using System.Diagnostics;
using Gremlin.Net.Driver;

public static class GremlinQueryExecutor
{
    public static async Task<dynamic> ExecuteAsync(
        GremlinClient client,
        string query,
        string queryName,
        string backend
        )
    {
        using var activity = GremlinTracing.activitySource
            .StartActivity(queryName);

        activity?.SetTag("gremlin.query", queryName);
        activity?.SetTag("backend", backend);
        activity?.SetTag("temporal", true);

        var sw = Stopwatch.StartNew();
        var res = await client.SubmitAsync<dynamic>(query);
        sw.Stop();

        TemporalMetrics.QueryLatency.Record(
            sw.ElapsedMilliseconds,
            new KeyValuePair<string, object?>("query", queryName),
            new KeyValuePair<string, object?>("backend", backend)
            );

        TemporalMetrics.QueryCount.Add(1);

        return res;
    }
}
