using System.Diagnostics.Metrics;


public static class TemporalMetrics
{
    public static readonly Meter Meter = new("Nexus.Temporal");

    public static readonly Histogram<double> QueryLatency =
        Meter.CreateHistogram<double>(
            "temporal.gremlin.latency",
            unit: "ms",
            description: "Latency of temporal Gremlin queries"
            );

    public static readonly Counter<long> QueryCount =
            Meter.CreateCounter<long>(
                "temporal.gremlin.count",
                description: "Number of temporal Gremlinqueries executed"
            );
}
