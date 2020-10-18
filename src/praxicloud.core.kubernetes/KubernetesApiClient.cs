// Copyright (c) Chris Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kubernetes
{
    #region Using Clauses
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using k8s;
    using k8s.Models;
    using Microsoft.Rest;
    using praxicloud.core.security;
    #endregion

    /// <summary>
    /// The base class that defines the cluster interop
    /// </summary>
    public class KubernetesApiClient : IDisposable
    {
        #region Delegates
        /// <summary>
        /// A method that is invoked when the number of replicas have been reported on by the Kubernetes API watch
        /// </summary>
        /// <param name="details">The details of the replica sets</param>
        /// <param name="userState">A user state object returned in when the handler is invoked</param>
        public delegate void ReplicaCountReporter(ReplicaDetails details, object userState);
        #endregion
        #region Constants
        /// <summary>
        /// The name of the variable that holds the Pod Name if configured based on recommendation
        /// </summary>
        public const string PodNameVariable = "POD_NAME";

        /// <summary>
        /// The name of the variable that holds the Namespace name if configured based on recommendations
        /// </summary>
        public const string NamspaceNameVariable = "POD_NAMESPACE";
        #endregion
        #region Variables
        /// <summary>
        /// The number of times the client has been disposed of
        /// </summary>
        private int _disposalCount;

        /// <summary>
        /// The Kuberentes configuration information that is in use
        /// </summary>
        private KubernetesClientConfiguration _configuration;

        /// <summary>
        /// The client being used to communicate with the cluster
        /// </summary>
        private readonly Kubernetes _client;
        #endregion
        #region Constructors
        /// <summary>
        /// Uses the best option to determine the cluster connection information, including in cluster.
        /// </summary>
        public KubernetesApiClient()
        {
            _configuration = KubernetesClientConfiguration.BuildDefaultConfig();
            _client = new Kubernetes(_configuration);
        }

        /// <summary>
        /// Uses the Kube Config file specified to identify the cluster to interact with
        /// </summary>
        /// <param name="fileName">The Kube Config file to use</param>
        public KubernetesApiClient(string fileName) : this()
        {
            Guard.FileExists(nameof(fileName), fileName);

            _configuration = KubernetesClientConfiguration.BuildConfigFromConfigFile(fileName);
            _client = new Kubernetes(_configuration);
        }
        #endregion
        #region Properties
        /// <summary>
        /// The current Kubernetes Context
        /// </summary>
        public string CurrentContext 
        {
            get => _configuration.CurrentContext;
        }

        /// <summary>
        /// The current Kubernetes Host
        /// </summary>
        public string Host
        {
            get => _configuration.Host;
        }

        /// <summary>
        /// The base URI 
        /// </summary>
        public Uri BaseUri
        {
            get => _client.BaseUri;
        }

        /// <summary>
        /// The name of the pod only available in pods configured as recommended with downward API
        /// </summary>
        public string PodName
        {
            get => Environment.GetEnvironmentVariable("POD_NAME");
        }

        /// <summary>
        /// The name of the pods namespace only available in pods configured as recommended with downward API
        /// </summary>
        public string NamespaceName
        {
            get => Environment.GetEnvironmentVariable("POD_NAMESPACE");
        }
        #endregion
        #region Methods
        /// <summary>
        /// Retrieves a list of the namespaces in the cluster
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for abort requests</param>
        /// <returns>A list of namespaces in the cluster</returns>
        public async Task<List<string>> GetNamespacesAsync(CancellationToken cancellationToken)
        {
            var results = new List<string>();
            var response = await _client.ListNamespaceAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach(var item in response.Items)
            {
                results.Add(item.Metadata.Name);
            }

            return results;
        }

        /// <summary>
        /// Retrieves the pods in the specified namespace
        /// </summary>
        /// <param name="namespaceName">The name of the namespace to retrieve pods in</param>
        /// <param name="cancellationToken">A token to monitor for abort requests</param>
        /// <returns>A list of pods in the namespace</returns>
        public async Task<List<string>> GetPodsInNamespaceAsync(string namespaceName, CancellationToken cancellationToken)
        {
            Guard.NotNullOrWhitespace(nameof(namespaceName), namespaceName);

            var results = new List<string>();
            var response = await _client.ListNamespacedPodAsync(namespaceName, cancellationToken: cancellationToken).ConfigureAwait(false);

            foreach (var item in response.Items)
            {
                results.Add(item.Metadata.Name);
            }

            return results;
        }

        /// <summary>
        /// Retrieves the controller information associated with a pod if found
        /// </summary>
        /// <param name="namespaceName">The name of the namespace to retrieve the controller for</param>
        /// <param name="podName">The name of the pod that the controller is associated with</param>
        /// <param name="cancellationToken">A token to monitor for abort requests</param>
        /// <returns>The controller details</returns>
        public async Task<ControllerDetails> GetPodControllerDetailsAsync(string namespaceName, string podName, CancellationToken cancellationToken)
        {
            Guard.NotNullOrWhitespace(nameof(namespaceName), namespaceName);
            Guard.NotNullOrWhitespace(nameof(podName), podName);

            var controllerFound = false;
            var results = new ControllerDetails(namespaceName, podName);
            var response = await _client.ReadNamespacedPodAsync(podName, namespaceName, cancellationToken: cancellationToken).ConfigureAwait(false);

            for(var index = 0; index < response.Metadata.OwnerReferences.Count && !controllerFound; index++)
            {
                var ownerItem = response.Metadata.OwnerReferences[index]; 

                if (ownerItem.Controller ?? false)
                {
                    controllerFound = true;

                    results.SetControllerDetails(ownerItem.Kind, ownerItem.Name);              
                }
            }

            return results;
        }

        /// <summary>
        /// Retrieve the replica information associated with the controller. This only supports StatefulSets, Replicaet, ReplicationControllers and DaemonSets
        /// </summary>
        /// <param name="namespaceName">The namespace that the controller is located in</param>
        /// <param name="controllerName">The name of the controller</param>
        /// <param name="controllerType">The type of the controller</param>
        /// <param name="cancellationToken">A token to monitor for abort requests</param>
        /// <returns></returns>
        public async Task<ReplicaDetails> GetReplicaInformationAsync(string namespaceName, string controllerName, KubernetesControllerType controllerType, CancellationToken cancellationToken)
        {
            Guard.NotNullOrWhitespace(nameof(namespaceName), namespaceName);
            Guard.NotNullOrWhitespace(nameof(controllerName), controllerName);
            if (controllerType != KubernetesControllerType.StatefulSet && controllerType != KubernetesControllerType.ReplicaSet && controllerType != KubernetesControllerType.ReplicationController && controllerType != KubernetesControllerType.DaemonSet) throw new GuardException("Only replica sets, stateful sets, replication controllers and daemon sets are supported", nameof(controllerType));

            var details = new ReplicaDetails(namespaceName, controllerName, controllerType, KubernetesWatchStatus.Unknown);

            switch(controllerType)
            {
                case KubernetesControllerType.DaemonSet:
                    var DaemonSetResults = await _client.ReadNamespacedDaemonSetWithHttpMessagesAsync(controllerName, namespaceName, cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (DaemonSetResults.Response.IsSuccessStatusCode)
                    {                        
                        details.SetReplicaDetails(DaemonSetResults.Body.Status.NumberAvailable, DaemonSetResults.Body.Status.NumberReady, DaemonSetResults.Body.Status.DesiredNumberScheduled);
                    }
                    break;

                case KubernetesControllerType.ReplicaSet:
                    var replicaSetResults = await _client.ReadNamespacedReplicaSetWithHttpMessagesAsync(controllerName, namespaceName, cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (replicaSetResults.Response.IsSuccessStatusCode)
                    {
                        details.SetReplicaDetails(replicaSetResults.Body.Status.Replicas, replicaSetResults.Body.Status.ReadyReplicas, replicaSetResults.Body.Spec.Replicas);
                    }
                    break;

                case KubernetesControllerType.ReplicationController:
                    var replicationControllerResults = await _client.ReadNamespacedReplicationControllerWithHttpMessagesAsync(controllerName, namespaceName, cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (replicationControllerResults.Response.IsSuccessStatusCode)
                    {
                        details.SetReplicaDetails(replicationControllerResults.Body.Status.Replicas, replicationControllerResults.Body.Status.ReadyReplicas, replicationControllerResults.Body.Spec.Replicas);
                    }
                    break;

                case KubernetesControllerType.StatefulSet:
                    var statefulSetResults = await _client.ReadNamespacedStatefulSetWithHttpMessagesAsync(controllerName, namespaceName, cancellationToken: cancellationToken).ConfigureAwait(false);

                    if(statefulSetResults.Response.IsSuccessStatusCode)
                    {
                        details.SetReplicaDetails(statefulSetResults.Body.Status.Replicas, statefulSetResults.Body.Status.ReadyReplicas, statefulSetResults.Body.Spec.Replicas);
                    }
                    break;
            }

            return details;
        }
               

        /// <summary>
        /// Retrieve the replica information associated with the controller and sets up a watch. This only supports StatefulSets, Replicaet, ReplicationControllers and DaemonSets
        /// </summary>
        /// <param name="namespaceName">The namespace that the controller is located in</param>
        /// <param name="controllerName">The name of the controller</param>
        /// <param name="controllerType">The type of the controller</param>
        /// <param name="userState">A state object that is passed in to the handler when the callback is invoked</param>
        /// <param name="cancellationToken">A token to monitor for abort requests</param>
        /// <param name="watchCallback">A callback that is notified if something changes with a controllers replica count (desired, current or ready)</param>
        /// <returns>A kubernetes watch that should not be disposed until it is no longer needed</returns>
        public async Task<KubernetesWatch> SubscribeToReplicaInformationAsync(string namespaceName, string controllerName, KubernetesControllerType controllerType, object userState, ReplicaCountReporter watchCallback, CancellationToken cancellationToken)
        {
            Guard.NotNullOrWhitespace(nameof(namespaceName), namespaceName);
            Guard.NotNullOrWhitespace(nameof(controllerName), controllerName);
            Guard.NotNull(nameof(watchCallback), watchCallback);
            if (controllerType != KubernetesControllerType.StatefulSet && controllerType != KubernetesControllerType.ReplicaSet && controllerType != KubernetesControllerType.ReplicationController && controllerType != KubernetesControllerType.DaemonSet) throw new GuardException("Only replica sets, stateful sets, replication controllers and daemon sets are supported", nameof(controllerType));

            IDisposable watcher = null;
            var watchStatus = KubernetesWatchStatus.FailedToInitiate;
            int? replicas = null;
            int? readyReplicas = null;
            int? desiredReplicas = null;

            switch (controllerType)
            {
                case KubernetesControllerType.DaemonSet:
                    var daemonSetResults = await _client.ListNamespacedDaemonSetWithHttpMessagesAsync(namespaceName, fieldSelector: $"metadata.name={controllerName}", watch: true, cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (daemonSetResults.Response.IsSuccessStatusCode)
                    {
                        if ((daemonSetResults.Body?.Items?.Count ?? 0) > 0)
                        {
                            var watchedSet = daemonSetResults.Body.Items[0];

                            watchStatus = KubernetesWatchStatus.Open;
                            replicas = watchedSet.Status.NumberAvailable;
                            readyReplicas = watchedSet.Status.NumberReady;
                            desiredReplicas = watchedSet.Status.DesiredNumberScheduled;
                        }

                        watcher = daemonSetResults.Watch<V1DaemonSet, V1DaemonSetList>((type, item) =>
                        {
                            var reportingDetails = new ReplicaDetails(namespaceName, controllerName, controllerType, KubernetesWatchStatus.Open);

                            reportingDetails.SetReplicaDetails(item.Status.NumberAvailable, item.Status.NumberReady, item.Status.DesiredNumberScheduled);
                            watchCallback?.Invoke(reportingDetails, userState);
                        },
                        (e) =>
                        {
                            var reportingDetails = new ReplicaDetails(namespaceName, controllerName, controllerType, KubernetesWatchStatus.Error);

                            reportingDetails.Exception = e;
                            watchCallback?.Invoke(reportingDetails, userState);
                        },
                        () =>
                        {
                            var reportingDetails = new ReplicaDetails(namespaceName, controllerName, controllerType, KubernetesWatchStatus.Closed);
                            watchCallback?.Invoke(reportingDetails, userState);
                        });

                    }
                    break;

                case KubernetesControllerType.ReplicaSet:
                    var replicaSetResults = await _client.ListNamespacedReplicaSetWithHttpMessagesAsync(namespaceName, fieldSelector: $"metadata.name={controllerName}", watch: true, cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (replicaSetResults.Response.IsSuccessStatusCode)
                    {
                        if ((replicaSetResults.Body?.Items?.Count ?? 0) > 0)
                        {
                            var watchedSet = replicaSetResults.Body.Items[0];

                            watchStatus = KubernetesWatchStatus.Open;
                            replicas = watchedSet.Status.Replicas;
                            readyReplicas = watchedSet.Status.ReadyReplicas;
                            desiredReplicas = watchedSet.Spec.Replicas;
                        }

                        watcher = replicaSetResults.Watch<V1ReplicaSet, V1ReplicaSetList>((type, item) =>
                        {
                            var reportingDetails = new ReplicaDetails(namespaceName, controllerName, controllerType, KubernetesWatchStatus.Open);

                            reportingDetails.SetReplicaDetails(item.Status.Replicas, item.Status.ReadyReplicas, item.Spec.Replicas);
                            watchCallback?.Invoke(reportingDetails, userState);
                        },
                        (e) =>
                        {
                            var reportingDetails = new ReplicaDetails(namespaceName, controllerName, controllerType, KubernetesWatchStatus.Error);

                            reportingDetails.Exception = e;
                            watchCallback?.Invoke(reportingDetails, userState);
                        },
                        () =>
                        {
                            var reportingDetails = new ReplicaDetails(namespaceName, controllerName, controllerType, KubernetesWatchStatus.Closed);
                            watchCallback?.Invoke(reportingDetails, userState);
                        });
                    }
                    break;

                case KubernetesControllerType.ReplicationController:
                    var replicationControllerResults = await _client.ListNamespacedReplicationControllerWithHttpMessagesAsync(namespaceName, fieldSelector: $"metadata.name={controllerName}", watch: true, cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (replicationControllerResults.Response.IsSuccessStatusCode)
                    {
                        if ((replicationControllerResults.Body?.Items?.Count ?? 0) > 0)
                        {
                            var watchedSet = replicationControllerResults.Body.Items[0];

                            watchStatus = KubernetesWatchStatus.Open;
                            replicas = watchedSet.Status.Replicas;
                            readyReplicas = watchedSet.Status.ReadyReplicas;
                            desiredReplicas = watchedSet.Spec.Replicas;
                        }

                        watcher = replicationControllerResults.Watch<V1ReplicationController, V1ReplicationControllerList>((type, item) =>
                        {
                            var reportingDetails = new ReplicaDetails(namespaceName, controllerName, controllerType, KubernetesWatchStatus.Open);

                            reportingDetails.SetReplicaDetails(item.Status.Replicas, item.Status.ReadyReplicas, item.Spec.Replicas);
                            watchCallback?.Invoke(reportingDetails, userState);
                        },
                        (e) =>
                        {
                            var reportingDetails = new ReplicaDetails(namespaceName, controllerName, controllerType, KubernetesWatchStatus.Error);

                            reportingDetails.Exception = e;
                            watchCallback?.Invoke(reportingDetails, userState);
                        },
                        () =>
                        {
                            var reportingDetails = new ReplicaDetails(namespaceName, controllerName, controllerType, KubernetesWatchStatus.Closed);
                            watchCallback?.Invoke(reportingDetails, userState);
                        });
                    }
                    break;

                case KubernetesControllerType.StatefulSet:
                    var statefulSetResults = await _client.ListNamespacedStatefulSetWithHttpMessagesAsync(namespaceName, fieldSelector: $"metadata.name={controllerName}", watch: true, cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (statefulSetResults.Response.IsSuccessStatusCode)
                    {
                        if ((statefulSetResults.Body?.Items?.Count ?? 0) > 0)
                        {
                            var watchedSet = statefulSetResults.Body.Items[0];

                            watchStatus = KubernetesWatchStatus.Open;
                            replicas = watchedSet.Status.Replicas;
                            readyReplicas = watchedSet.Status.ReadyReplicas;
                            desiredReplicas = watchedSet.Spec.Replicas;
                        }

                        watcher = statefulSetResults.Watch<V1StatefulSet, V1StatefulSetList>((type, item) =>
                        {
                            var reportingDetails = new ReplicaDetails(namespaceName, controllerName, controllerType, KubernetesWatchStatus.Open);

                            reportingDetails.SetReplicaDetails(item.Status.Replicas, item.Status.ReadyReplicas, item.Spec.Replicas);
                            watchCallback?.Invoke(reportingDetails, userState);
                        },
                        (e) =>
                        {
                            var reportingDetails = new ReplicaDetails(namespaceName, controllerName, controllerType, KubernetesWatchStatus.Error);

                            reportingDetails.Exception = e;
                            watchCallback?.Invoke(reportingDetails, userState);
                        },
                        () =>
                        {
                            var reportingDetails = new ReplicaDetails(namespaceName, controllerName, controllerType, KubernetesWatchStatus.Closed);
                            watchCallback?.Invoke(reportingDetails, userState);
                        });

                    }
                    break;
            }

            var details = new ReplicaDetails(namespaceName, controllerName, controllerType, watchStatus);
            details.SetReplicaDetails(replicas, readyReplicas, desiredReplicas);
            var watchSubscription = new KubernetesWatch(watcher, watchStatus, userState, details);

            return watchSubscription;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if(Interlocked.Increment(ref _disposalCount) == 1)
            {                
                GC.SuppressFinalize(this);
                _client.Dispose();                    
            }
        }
        #endregion
    }
}
