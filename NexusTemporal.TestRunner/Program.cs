using NexusTemporal.Tests.Infrastructure;
using NUnitLite;

TelemetrySetup.Initialize();

var engine = new AutoRun();
engine.Execute(new[] { "--worker=1", "--labels=All", "--trace=Info" });

Thread.Sleep(Timeout.Infinite);