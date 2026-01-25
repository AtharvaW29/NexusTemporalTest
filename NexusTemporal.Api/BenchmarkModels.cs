namespace NexusTemporal.Api;

public record QueryResult(int Iteration, long ExecutionTimeMs, int ResultCount, bool Success, string? Error = null);
public record QueryBenchmarkResult(string QueryName, int Iterations, List<QueryResult> Results, QueryStatistics? Statistics);
public record QueryStatistics(long Min, long Max, long Avg, long P50, long P95, long P99);
