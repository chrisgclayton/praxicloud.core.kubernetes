// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kubernetes
{
    #region Using Clauses
    using Microsoft.Extensions.Logging;
    using praxicloud.core.metrics;
    #endregion

    /// <summary>
    /// A custom diagnostics configuration 
    /// </summary>
    public class DiagnosticsConfiguration : IDiagnosticsConfiguration
    {
        #region Properties
        /// <inheritdoc />
        public ILoggerFactory LoggerFactory { get; set; }

        /// <inheritdoc />
        public IMetricFactory MetricFactory { get; set; }
        #endregion
    }
}
