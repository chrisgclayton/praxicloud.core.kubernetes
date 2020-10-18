

using Microsoft.Extensions.Logging;
using praxicloud.core.containers;
using System;
using System.Net;
using System.Threading.Tasks;

namespace praxicloud.core.kubernetes.testcontainer
{
    class Program
    {
        static void Main(string[] args)
        {
            var probes = new ProbeConfiguration
            {
                AvailabilityIPAddress = IPAddress.Any,
                AvailabilityPort = 10281,
                AvailabilityProbeInterval = TimeSpan.FromSeconds(1),
                HealthIPAddress = IPAddress.Any,
                HealthPort = 10281,
                HealthProbeInterval = TimeSpan.FromSeconds(1),
                UseTcp = false
            };

            var diagnostics = new DefaultDiagnosticsConfiguration("demo", new DefaultDiagnosticsConfiguration.DefaultLoggerConfiguration
            {
                IncludeColors = true,
                IncludeConsoleLogger = true,
                IncludeDebugLogger = false,
                IncludeScopes = true,
                Level = LogLevel.Trace
            },
            new DefaultDiagnosticsConfiguration.DefaultMetricConfiguration
            {
                IncludeApplicationInsights = false,
                IncludeConsole = true,
                IncludeDebug = false,
                IncludeLabels = true,
                IncludePrometheus = true,
                InstrumentationKey = null,
                PrometheusPort = 10282,
                ReportingInterval = 5
            });

            var container = new DemoContainer("democontainer", probes, diagnostics);

            container.StartContainer();

            Task.WhenAll(ContainerLifecycle.Task, container.Task).GetAwaiter().GetResult();

        }
    }
}
