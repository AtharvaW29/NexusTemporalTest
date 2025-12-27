var builder = DistributedApplication.CreateBuilder(args);

var gremlin = builder.AddContainer("gremlin", "tinkerpop/gremlin-server")
    .WithEndpoint(port: 8182, targetPort: 8182)
    .WithEnvironment("GREMLIN_SERVER_YAML", "/opt/gremlin-server/conf/gremlin-server.yaml");


builder.Build().Run();
