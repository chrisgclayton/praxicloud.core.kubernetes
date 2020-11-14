// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kubernetes
{
    #region Using Clauses
    using System;
    using System.Threading;
    using praxicloud.core.security;
    #endregion

    /// <summary>
    /// A basic type that holds a Kubernetes watch open
    /// </summary>
    public class KubernetesWatch : IDisposable
    {
        #region Variables
        /// <summary>
        /// The number of times the instance was disposed of
        /// </summary>
        private int _disposalCount;

        /// <summary>
        /// A watch that when disposed of the subscription will be stopped
        /// </summary>
        private readonly IDisposable _watchHandle;
        #endregion
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the type
        /// </summary>
        /// <param name="watchHandle">The watch handle that will stop watching when closed</param>
        /// <param name="watchStatus">The current status of the watch</param>
        /// <param name="userState">A user state object passed to the callback when invoked</param>
        /// <param name="initialDetails">The initial details of the query that the watch is associated with</param>
        internal KubernetesWatch(IDisposable watchHandle, KubernetesWatchStatus watchStatus, object userState, ReplicaDetails initialDetails)
        {
            Guard.NotNull(nameof(watchHandle), watchHandle);
            Guard.NotNull(nameof(initialDetails), initialDetails);

            _watchHandle = watchHandle;
            UserState = userState;
            InitialDetails = initialDetails;
            WatchStatus = watchStatus;
        }

        /// <summary>
        /// Finalizes the instance
        /// </summary>
        ~KubernetesWatch()
        {
            if (Interlocked.Increment(ref _disposalCount) == 1) _watchHandle.Dispose();
        }
        #endregion
        #region Properties
        /// <summary>
        /// The current status of the watcher
        /// </summary>
        public KubernetesWatchStatus WatchStatus { get; internal set; }

        /// <summary>
        /// The user state passed into each callback
        /// </summary>
        public object UserState { get; }

        /// <summary>
        /// The initial details of the controller
        /// </summary>
        public ReplicaDetails InitialDetails { get; }
        #endregion
        #region Methods
        /// <inheritdoc />
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            if (Interlocked.Increment(ref _disposalCount) == 1) _watchHandle.Dispose();
        }
        #endregion
    }    
}
