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
    public interface IProbeConfiguration
    {
        /// <summary>
        /// The availability probes IP Address to listen on. 
        /// </summary>
        IPAddress AvailabilityIPAddress { get; }

        /// <summary>
        /// The port to listen on for the availability probe. If null it will not start an availability probe
        /// </summary>
        ushort? AvailabilityPort { get; }

        /// <summary>
        /// The interval that the availability probe validation method will be checked, if not provided it will default to 5 seconds
        /// </summary>
        TimeSpan? AvailabilityProbeInterval { get; }

        /// <summary>
        /// The health probes IP Address to listen on. 
        /// </summary>
        IPAddress HealthIPAddress { get; }

        /// <summary>
        /// The port to listen on for the health probe. If null it will not start an health probe
        /// </summary>
        ushort? HealthPort { get; }

        /// <summary>
        /// The interval that the health probe validation method will be checked, if not provided it will default to 5 seconds
        /// </summary>
        TimeSpan? HealthProbeInterval { get; }

        /// <summary>
        /// Use basic TCP probe over an HTTP based
        /// </summary>
        bool UseTcp { get; }

        /// <summary>
        /// The X509 Certificate to use if HTTPS is desired
        /// </summary>
        X509Certificate2 Certificate { get; }
    }
}
