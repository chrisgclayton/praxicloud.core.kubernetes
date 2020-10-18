// Copyright (c) Chris Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kubernetes
{
    /// <summary>
    /// Indicates the status of a kubernetes watch connection
    /// </summary>
    public enum KubernetesWatchStatus
    {
        /// <summary>
        /// The status of the watch is not known
        /// </summary>
        Unknown,

        /// <summary>
        /// The status of the watch is open
        /// </summary>
        Open,

        /// <summary>
        /// The status of the watch is closed
        /// </summary>
        Closed,

        /// <summary>
        /// There was an error raised with the watch
        /// </summary>
        Error,

        /// <summary>
        /// The watch never initiated
        /// </summary>
        FailedToInitiate
    }
}
