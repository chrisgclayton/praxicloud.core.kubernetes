// Copyright (c) Chris Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kubernetes
{
    #region Using Clauses
    using System;
    using System.Runtime.CompilerServices;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using praxicloud.core.metrics;
    using praxicloud.core.metrics.applicationinsights;
    using praxicloud.core.metrics.consoleprovider;
    using praxicloud.core.metrics.debugprovider;
    using praxicloud.core.metrics.prometheus;
    using praxicloud.core.security;
    #endregion

    /// <summary>
    /// A diagnostics definition that outputs logging to console and debug with metrics being output to Application Insights, console, debug and prometheus as configured
    /// </summary>
    public class DefaultDiagnosticsConfiguration : DiagnosticsConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the type
        /// </summary>
        /// <param name="diagnosticsName">The name to use when writing diagnostics</param>
        /// <param name="loggerConfiguration">Basic logging configuration</param>
        /// <param name="metricConfiguration">Basic metrics configuration</param>
        public DefaultDiagnosticsConfiguration(string diagnosticsName, DefaultLoggerConfiguration loggerConfiguration, DefaultMetricConfiguration metricConfiguration)
        {
            Guard.NotNullOrWhitespace(nameof(diagnosticsName), diagnosticsName);
            Guard.NotNull(nameof(loggerConfiguration), loggerConfiguration);
            Guard.NotNull(nameof(metricConfiguration), metricConfiguration);

            var dependencyProvider = new ServiceCollection().AddLogging(configure =>
            {
                configure.ClearProviders();
                configure.SetMinimumLevel(loggerConfiguration.Level);

                if (loggerConfiguration.IncludeConsoleLogger)
                {
                    configure.AddConsole(new Action<Microsoft.Extensions.Logging.Console.ConsoleLoggerOptions>(options =>
                    {                        
                        options.DisableColors = !loggerConfiguration.IncludeColors;
                        options.IncludeScopes = loggerConfiguration.IncludeScopes;
                        options.LogToStandardErrorThreshold = LogLevel.Error;                        
                    }));
                }

                if (loggerConfiguration.IncludeDebugLogger) configure.AddDebug();
            }).BuildServiceProvider();

            LoggerFactory = dependencyProvider.GetRequiredService<ILoggerFactory>();

            MetricFactory = new MetricFactory();

            if(metricConfiguration.IncludeDebug) MetricFactory.AddDebug($"{diagnosticsName}-debug", metricConfiguration.ReportingInterval, metricConfiguration.IncludeLabels);
            if(metricConfiguration.IncludeConsole) MetricFactory.AddConsole($"{diagnosticsName}-console", metricConfiguration.ReportingInterval, metricConfiguration.IncludeLabels);
            if(metricConfiguration.IncludeApplicationInsights && !string.IsNullOrWhiteSpace(metricConfiguration.InstrumentationKey)) MetricFactory.AddApplicationInsights($"{diagnosticsName}-appinsights", metricConfiguration.InstrumentationKey);
            if (metricConfiguration.IncludePrometheus && metricConfiguration.PrometheusPort > 0) MetricFactory.AddPrometheus($"{diagnosticsName}-prometheus", metricConfiguration.PrometheusPort, Environment.MachineName);
        }

        /// <summary>
        /// Metric details
        /// </summary>
        public sealed class DefaultMetricConfiguration
        {
            /// <summary>
            /// True to enable the Application Insights Metrics Provider
            /// </summary>
            public bool IncludeApplicationInsights { get; set; } = false;

            /// <summary>
            /// The instrumentation key to use for writing to application insights
            /// </summary>
            public string InstrumentationKey { get; set; }

            /// <summary>
            /// True to provide a scraping Prometheus endpoint
            /// </summary>
            public bool IncludePrometheus { get; set; } = true;

            /// <summary>
            /// The port to listen for Prometheus scraping requests
            /// </summary>
            public ushort PrometheusPort { get; set; } = 9600;

            /// <summary>
            /// True to write to the console
            /// </summary>
            public bool IncludeConsole { get; set; } = false;

            /// <summary>
            /// True to write to the debug stream
            /// </summary>
            public bool IncludeDebug { get; set; } = false;

            /// <summary>
            /// True to include metric labels
            /// </summary>
            public bool IncludeLabels { get; set; } = true;

            /// <summary>
            /// The interval to report at
            /// </summary>
            public long ReportingInterval { get; set; }

        }

        /// <summary>
        /// Logging details 
        /// </summary>
        public sealed class DefaultLoggerConfiguration
        {
            /// <summary>
            /// The level to write to the logger at
            /// </summary>
            public LogLevel Level { get; set; } = LogLevel.Information;

            /// <summary>
            /// True to include scopes
            /// </summary>
            public bool IncludeScopes { get; set; } = false;

            /// <summary>
            /// True to include colors if possible
            /// </summary>
            public bool IncludeColors { get; set; } = false;

            /// <summary>
            /// True to write to the debug stream
            /// </summary>
            public bool IncludeDebugLogger { get; set; } = false;

            /// <summary>
            /// True to write to the console provider
            /// </summary>
            public bool IncludeConsoleLogger { get; set; } = true;
        }
    }
}
