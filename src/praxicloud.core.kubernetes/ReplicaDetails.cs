// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kubernetes
{
    #region Using Clauses
    using System;
    #endregion

    /// <summary>
    /// The count details of a controller that supports replicas
    /// </summary>
    public sealed class ReplicaDetails
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the type
        /// </summary>
        /// <param name="namespaceName">The namespace names</param>
        /// <param name="controllerName">The controller name</param>
        /// <param name="type">The type of the kubernetes controller</param>
        /// <param name="watchStatus">The status of the kubernetes watch</param>
        public ReplicaDetails(string namespaceName, string controllerName, KubernetesControllerType type, KubernetesWatchStatus watchStatus)
        {
            NamespaceName = namespaceName;
            ControllerName = controllerName;
            Type = type;
            WatchStatus = watchStatus;
        }
        #endregion
        #region Properties
        /// <summary>
        /// The name of the namespace the pod is associated with
        /// </summary>
        public string NamespaceName { get; }

        /// <summary>
        /// The name of the controller 
        /// </summary>
        public string ControllerName { get; }
        
        /// <summary>
        /// The type of controller
        /// </summary>
        public KubernetesControllerType Type { get; }

        /// <summary>
        /// The number of replicas
        /// </summary>
        public int? ReplicaCount { get; private set; }

        /// <summary>
        /// The number of replicas that are ready
        /// </summary>
        public int? ReadyReplicaCount { get; private set; }

        /// <summary>
        /// The desired replica count
        /// </summary>
        public int? DesiredReplicaCount { get; private set; }

        /// <summary>
        /// The status of the watch if associated with one
        /// </summary>
        public KubernetesWatchStatus WatchStatus { get; }

        /// <summary>
        /// Used to communicate exceptions raised by the controller watch
        /// </summary>
        public Exception Exception { get; internal set; }
        #endregion
        #region Methods
        /// <summary>
        /// Sets the replica details
        /// </summary>
        /// <param name="replicaCount">The number of replicas that are scheduled</param>
        /// <param name="readyReplicaCount">The number of replcas that are ready</param>
        /// <param name="desiredReplicaCount">The number of replicas that are desired</param>
        internal void SetReplicaDetails(int? replicaCount, int? readyReplicaCount, int? desiredReplicaCount)
        {
            ReplicaCount = replicaCount;
            DesiredReplicaCount = desiredReplicaCount;
            ReadyReplicaCount = readyReplicaCount;
        }
        #endregion
    }
}
