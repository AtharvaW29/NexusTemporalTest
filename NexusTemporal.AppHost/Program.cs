var builder = DistributedApplication.CreateBuilder(args);

// Gremlin Server
builder.AddContainer("gremlin", "tinkerpop/gremlin-server")
    .WithEndpoint(
        port: 8182,
        targetPort: 8182,
        scheme: "tcp",
        name: "gremlin")
    .WithEnvironment(
        "GREMLIN_SERVER_YAML",
        "/opt/gremlin-server/conf/gremlin-server.yaml");

// OpenTelemetry Collector
builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib")
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

builder.Build().Run();
