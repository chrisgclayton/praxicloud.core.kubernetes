// Copyright (c) Chris Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kubernetes
{
    #region Using Clauses
    using System;
    #endregion

    /// <summary>
    /// The details of the contoller associated with a namespace and pod
    /// </summary>
    public sealed class ControllerDetails
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the type
        /// </summary>
        /// <param name="namespaceName">The namespace names</param>
        /// <param name="podName">The pod name</param>
        public ControllerDetails(string namespaceName, string podName)
        {
            NamespaceName = namespaceName;
            PodName = podName;
        }
        #endregion
        #region Properties
        /// <summary>
        /// The name of the namespace the pod is associated with
        /// </summary>
        public string NamespaceName { get; }

        /// <summary>
        /// The name of the pod 
        /// </summary>
        public string PodName { get; }

        /// <summary>
        /// True if a controller is found that the pod is associated with
        /// </summary>
        public bool ControllerFound { get; private set; } = false;

        /// <summary>
        /// The controller type if found, or null if not
        /// </summary>
        public string ControllerType { get; private set; } = null;

        /// <summary>
        /// The name of the controller if found, or null if not
        /// </summary>
        public string ControllerName { get; private set; } = null;

        /// <summary>
        /// The type of controller
        /// </summary>
        public KubernetesControllerType Type { get; private set; } = KubernetesControllerType.None;
        #endregion
        #region Methods
        /// <summary>
        /// Sets the controller details
        /// </summary>
        /// <param name="controllerType">The controller type</param>
        /// <param name="controllerName">The controller name</param>
        internal void SetControllerDetails(string controllerType, string controllerName)
        {            
            ControllerFound = true;
            ControllerType = controllerType;
            ControllerName = controllerName;

            if(Enum.TryParse(typeof(KubernetesControllerType), ControllerType, out var converted))
            {
                Type = (KubernetesControllerType)converted;
            }
        }
        #endregion
    }
}
