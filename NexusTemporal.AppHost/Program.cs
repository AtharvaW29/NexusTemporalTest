var builder = DistributedApplication.CreateBuilder(args);

// Gremlin Server
var gremlin = builder.AddContainer("gremlin", "tinkerpop/gremlin-server")
    .WithEndpoint(
        port: 8182,
        targetPort: 8182,
        scheme: "tcp",
        name: "gremlin")
    .WithEnvironment(
        "GREMLIN_SERVER_YAML",
        "/opt/gremlin-server/conf/gremlin-server.yaml");

// OpenTelemetry Collector
var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib")
    .WithBindMount(
        "./otel/otel-collector.yaml",
        "/etc/otelcol/config.yaml")
    .WithArgs("--config=/etc/otelcol/config.yaml")
    .WithEndpoint(
        port: 4317,
        targetPort: 4317,
        scheme: "tcp",
        name: "otlp-grpc")
    .WithEndpoint(
        port: 4318,
        targetPort: 4318,
        scheme: "http",
        name: "otlp-http");

// The dashboard endpoint is available via the DOTNET_DASHBOARD_OTLP_ENDPOINT_URL env var
var dashboardEndpoint = Environment.GetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL")
    ?? "http://localhost:19228";

var dockerEndpoint = dashboardEndpoint.Replace("localhost", "host.docker.internal")
                                      .Replace("127.0.0.1", "host.docker.internal");
otelCollector.WithEnvironment("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL", dockerEndpoint);

// API Service for running benchmarks and visualization
var gremlinEndpoint = gremlin.GetEndpoint("gremlin");
var api = builder.AddProject<Projects.NexusTemporal_Api>("api", launchProfileName: "NexusTemporal.Api")
    .WithReference(gremlinEndpoint)
    .WithEnvironment("GREMLIN_HOST", () => gremlinEndpoint.Host)
    .WithEnvironment("GREMLIN_PORT", () => gremlinEndpoint.Port.ToString());

builder.Build().Run();