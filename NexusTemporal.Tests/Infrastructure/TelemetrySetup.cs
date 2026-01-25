using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using System;

namespace NexusTemporal.Tests.Infrastructure;

public static class TelemetrySetup
{
    private static bool _initialized;

    public static IDisposable Initialize()
    {
        if (_initialized)
            return new CompositeDisposable();

        _initialized = true;

        var resource = ResourceBuilder.CreateDefault()
            .AddService("NexusTemporal.Tests");

        // Allow overriding OTLP endpoint/protocol via environment variables for running Aspire locally
        var otlpEndpointEnv = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        var otlpProtocolEnv = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL");

        var defaultEndpoint = otlpEndpointEnv ?? "http://localhost:4317";
        var protocol = (otlpProtocolEnv ?? string.Empty).ToLower() switch
        {
            "http/protobuf" or "http" or "http/protobuf" => OtlpExportProtocol.HttpProtobuf,
            _ => OtlpExportProtocol.Grpc,
        };

        // Traces
        var tracerProviderBuilder = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resource)
            .AddSource(GremlinTracing.activitySource.Name)
            .SetSampler(new AlwaysOnSampler())
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(defaultEndpoint);
                options.Protocol = protocol;
            })
            // Add console exporter so tests show traces locally for debugging
            .AddConsoleExporter();

        var tracerProvider = tracerProviderBuilder.Build();

        // Metrics
        var meterProviderBuilder = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resource)
            .AddMeter(TemporalMetrics.Meter.Name);
        
        // Check if running in Aspire - send directly to dashboard if available
        var dashboardEndpoint = Environment.GetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL");
        if (!string.IsNullOrEmpty(dashboardEndpoint))
        {
            // Send to Aspire dashboard directly
            meterProviderBuilder.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(dashboardEndpoint);
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
        }
        
        // Also send to OTEL collector (or use as fallback)
        meterProviderBuilder.AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri(defaultEndpoint);
            options.Protocol = protocol;
        });
        
        // Add console exporter for metrics
        meterProviderBuilder.AddConsoleExporter();

        var meterProvider = meterProviderBuilder.Build();

        return new CompositeDisposable(tracerProvider, meterProvider);
    }

    private sealed class CompositeDisposable : IDisposable
    {
        private readonly IDisposable[] _items;
        public CompositeDisposable(params IDisposable[] items) => _items = items ?? Array.Empty<IDisposable>();
        public void Dispose()
        {
            foreach (var item in _items)
                item.Dispose();
        }
    }
}