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

    // Traversal depth (number of Gremlin steps)
    public static readonly Histogram<int> TraversalDepth =
        Meter.CreateHistogram<int>(
            name: "gremlin_traversal_depth",
            unit: "steps",
            description: "Depth of Gremlin traversal in number of steps");

    // Cardinality metrics
    public static readonly Histogram<long> VertexCardinality =
        Meter.CreateHistogram<long>(
            name: "gremlin_vertex_cardinality",
            unit: "count",
            description: "Number of vertices processed or emitted");

    public static readonly Histogram<long> EdgeCardinality =
        Meter.CreateHistogram<long>(
            name: "gremlin_edge_cardinality",
            unit: "count",
            description: "Number of edges processed or emitted");

    public static readonly Histogram<long> ResultCardinality =
        Meter.CreateHistogram<long>(
            name: "gremlin_result_cardinality",
            unit: "count",
            description: "Final traversal result size");
}
