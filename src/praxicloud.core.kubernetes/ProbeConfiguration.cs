// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kubernetes
{
    #region Using Clauses
    using System;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    #endregion

    /// <summary>
    /// Configures container probes
    /// </summary>
    public class ProbeConfiguration : IProbeConfiguration
    {
        /// <inheritdoc />
        public IPAddress AvailabilityIPAddress { get; set; }

        /// <inheritdoc />
        public ushort? AvailabilityPort { get; set; }

        /// <inheritdoc />
        public TimeSpan? AvailabilityProbeInterval { get; set; }

        /// <inheritdoc />
        public bool UseTcp { get; set; } = true;

        /// <inheritdoc />
        public X509Certificate2 Certificate { get; set; }

        /// <inheritdoc />
        public IPAddress HealthIPAddress { get; set; }

        /// <inheritdoc />
        public ushort? HealthPort { get; set; }

        /// <inheritdoc />
        public TimeSpan? HealthProbeInterval { get; set; }
    }
}
