// Copyright (c) Chris Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kubernetes
{
    /// <summary>
    /// Types of controllers
    /// </summary>
    public enum KubernetesControllerType
    {
        /// <summary>
        /// None has been defined
        /// </summary>
        None,

        /// <summary>
        /// There is a defined controller but it was unrecognizable
        /// </summary>
        Unknown,

        /// <summary>
        /// A Kubernetes deployment
        /// </summary>
        Deployment,

        /// <summary>
        /// A Kubernetes Replica Set
        /// </summary>
        ReplicaSet,

        /// <summary>
        /// A Kubernetes Stateful Set
        /// </summary>
        StatefulSet,

        /// <summary>
        /// A Kubernetes Daemon Set
        /// </summary>
        DaemonSet,

        /// <summary>
        /// A Kubernetes Job
        /// </summary>
        Job,

        /// <summary>
        /// A Kubernetes Cron Job
        /// </summary>
        CronJob,

        /// <summary>
        /// A Kubernetes Replication Controller
        /// </summary>
        ReplicationController
    }
}
