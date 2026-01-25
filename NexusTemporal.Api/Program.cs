using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphBinary;
using NexusTemporal.Api;
using NexusTemporal.Tests.Infrastructure;
using System.Diagnostics;

// Use application base directory as content root so wwwroot is found when run by Aspire from output dir
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ContentRootPath = AppContext.BaseDirectory
});

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Initialize telemetry
var telemetry = TelemetrySetup.Initialize();
builder.Services.AddSingleton(telemetry);

// Register Gremlin client - use environment variables if available (from Aspire)
var gremlinHost = builder.Configuration["GREMLIN_HOST"] ?? "localhost";
var gremlinPort = int.Parse(builder.Configuration["GREMLIN_PORT"] ?? "8182");

builder.Services.AddSingleton<GremlinClient>(sp =>
{
    var server = new GremlinServer(
        hostname: gremlinHost,
        port: gremlinPort,
        enableSsl: false
    );
    var serializer = new GraphBinaryMessageSerializer();
    return new GremlinClient(server, serializer);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Serve dashboard (index.html) at root — path is relative to web root (wwwroot)
app.MapGet("/", () => Results.File("index.html", "text/html"));

// Define benchmark queries
var benchmarkQueries = new Dictionary<string, string>
{
    ["SnapshotQuery"] = @"
        g.E()
        .has('valid_from', lte('2025-01-15'))
        .has('valid_to', gt('2025-01-15'))",
    
    ["TemporalShortestPath"] = @"
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
         .limit(1)",
    
    ["TemporalQuery_P95"] = @"
        g.E()
         .has('valid_from', lte('2025-02-01'))
         .has('valid_to', gt('2025-02-01'))"
};

// Endpoint to get available queries
app.MapGet("/api/queries", () =>
{
    return Results.Ok(benchmarkQueries.Keys);
})
.WithName("GetQueries")
.WithOpenApi();

// Endpoint to run a single query
app.MapPost("/api/query/{queryName}", async (string queryName, GremlinClient client) =>
{
    if (!benchmarkQueries.TryGetValue(queryName, out var query))
    {
        return Results.NotFound(new { error = $"Query '{queryName}' not found" });
    }

    var sw = Stopwatch.StartNew();
    try
    {
        var result = await GremlinQueryExecutor.ExecuteAsync(
            client,
            query,
            queryName,
            "api"
        );
        
        sw.Stop();
        var resultList = result.ToList();
        
        return Results.Ok(new
        {
            queryName,
            executionTimeMs = sw.ElapsedMilliseconds,
            resultCount = resultList.Count,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        sw.Stop();
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Query execution failed"
        );
    }
})
.WithName("RunQuery")
.WithOpenApi();

// Endpoint to run all benchmark queries
app.MapPost("/api/benchmark/run", async (GremlinClient client, int? iterations = 1) =>
{
    var results = new List<QueryBenchmarkResult>();
    var totalSw = Stopwatch.StartNew();

    foreach (var kvp in benchmarkQueries)
    {
        var queryName = kvp.Key;
        var query = kvp.Value;
        var queryResults = new List<QueryResult>();

        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var result = await GremlinQueryExecutor.ExecuteAsync(
                    client,
                    query,
                    queryName,
                    "api"
                );
                
                sw.Stop();
                var resultList = result.ToList();
                
                queryResults.Add(new QueryResult(
                    Iteration: i + 1,
                    ExecutionTimeMs: sw.ElapsedMilliseconds,
                    ResultCount: resultList.Count,
                    Success: true
                ));
            }
            catch (Exception ex)
            {
                sw.Stop();
                queryResults.Add(new QueryResult(
                    Iteration: i + 1,
                    ExecutionTimeMs: sw.ElapsedMilliseconds,
                    ResultCount: 0,
                    Success: false,
                    Error: ex.Message
                ));
            }
        }

        var executionTimes = queryResults
            .Where(r => r.Success)
            .Select(r => r.ExecutionTimeMs)
            .ToList();

        QueryStatistics? statistics = null;
        if (executionTimes.Any())
        {
            var sorted = executionTimes.OrderBy(x => x).ToList();
            statistics = new QueryStatistics(
                Min: sorted.Min(),
                Max: sorted.Max(),
                Avg: (long)sorted.Average(),
                P50: sorted.Skip(sorted.Count / 2).FirstOrDefault(),
                P95: sorted.Skip((int)(sorted.Count * 0.95)).FirstOrDefault(),
                P99: sorted.Skip((int)(sorted.Count * 0.99)).FirstOrDefault()
            );
        }

        results.Add(new QueryBenchmarkResult(
            QueryName: queryName,
            Iterations: queryResults.Count,
            Results: queryResults,
            Statistics: statistics
        ));
    }

    totalSw.Stop();

    return Results.Ok(new
    {
        TotalExecutionTimeMs = totalSw.ElapsedMilliseconds,
        Timestamp = DateTime.UtcNow,
        Queries = results
    });
})
.WithName("RunBenchmark")
.WithOpenApi();

// Endpoint to seed the graph
app.MapPost("/api/graph/seed", async (GremlinClient client) =>
{
    try
    {
        await TestGraphSeeder.SeedAsync(client);
        return Results.Ok(new { message = "Graph seeded successfully" });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Graph seeding failed"
        );
    }
})
.WithName("SeedGraph")
.WithOpenApi();

// Endpoint to get graph statistics
app.MapGet("/api/graph/stats", async (GremlinClient client) =>
{
    try
    {
        var vertexCount = await client.SubmitAsync<long>("g.V().count()");
        var edgeCount = await client.SubmitAsync<long>("g.E().count()");
        
        return Results.Ok(new
        {
            vertexCount = vertexCount.FirstOrDefault(),
            edgeCount = edgeCount.FirstOrDefault(),
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Failed to get graph statistics"
        );
    }
})
.WithName("GetGraphStats")
.WithOpenApi();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();

