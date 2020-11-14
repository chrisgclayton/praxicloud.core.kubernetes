// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kubernetes
{
    #region Using Clauses
    using Microsoft.Extensions.Logging;
    using praxicloud.core.metrics;
    #endregion

    /// <summary>
    /// The diagnostics details for metrics and logging
    /// </summary>
    public interface IDiagnosticsConfiguration
    {
        #region Properties
        /// <summary>
        /// The logger factory used to create loggers
        /// </summary>
        ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// The metric factory used to create metric objects
        /// </summary>
        IMetricFactory MetricFactory { get; }
        #endregion
    }
}
