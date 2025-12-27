using System.Diagnostics;

public static class GremlinTracing
{
    public static readonly ActivitySource activitySource = new("Gremlin.Temporal");
}
