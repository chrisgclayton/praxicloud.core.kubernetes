// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kubernetes
{
    #region Using Clauses
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using praxicloud.core.containers;
    using praxicloud.core.exceptions;
    using praxicloud.core.kestrel;
    using praxicloud.core.metrics;
    using praxicloud.core.security;
    using praxicloud.core.kestrel.probes;
    #endregion

    /// <summary>
    /// A container base type to simplify many of the common aspects of building a micro service element
    /// </summary>
    public abstract class ContainerBase : IHealthCheck, IAvailabilityCheck
    {
        #region Variables
        /// <summary>
        /// A cancellation token source used to trigger a container shutdown
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource = null;

        /// <summary>
        /// A task 
        /// </summary>
        private Task _processingTask;

        /// <summary>
        /// A watch subscription
        /// </summary>
        private KubernetesWatch _watch;

        /// <summary>
        /// The base configuration for the probe;
        /// </summary>
        private readonly IProbeConfiguration _baseProbeConfiguration;
        #endregion
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the type
        /// </summary>
        /// <param name="containerName">The name of the container to be used for logging etc.</param>
        /// <param name="probeConfiguration">The probe configuration for health and availability</param>
        /// <param name="diagnosticsConfiguration">The diagnostics configuration for logging and metrics</param>
        protected ContainerBase(string containerName, IProbeConfiguration probeConfiguration, IDiagnosticsConfiguration diagnosticsConfiguration)
        {
            Guard.NotNull(nameof(probeConfiguration), probeConfiguration);
            Guard.NotNull(nameof(diagnosticsConfiguration), diagnosticsConfiguration);

            LoggerFactory = diagnosticsConfiguration.LoggerFactory ?? new LoggerFactory();
            Logger = LoggerFactory.CreateLogger(containerName);
            MetricFactory = diagnosticsConfiguration.MetricFactory ?? new MetricFactory();

            _baseProbeConfiguration = probeConfiguration;          
        }
        #endregion
        #region Properties
        /// <summary>
        /// A cancellation token that can be monitored for container shutdown
        /// </summary>
        protected CancellationToken ContainerCancellationToken => ContainerLifecycle.CancellationToken;

        /// <summary>
        /// A task that can be monitored for container shutdown
        /// </summary>
        protected Task ContainerTask => ContainerLifecycle.Task;

        /// <summary>
        /// True if running on Kubernetes
        /// </summary>
        protected bool IsKubernetes { get; private set; }

        /// <summary>
        /// The cancellation token that combines the customer cancellation token and the container cancellation token to indicate an event should trigger cancellation
        /// </summary>
        public CancellationToken CancellationToken => _cancellationTokenSource?.Token ?? ContainerCancellationToken;

        /// <summary>
        /// A task that completes when the container should shutdown
        /// </summary>
        public Task Task { get; private set; }

        /// <summary>
        /// The logger to write diagnostics messages to
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// The factory to create new loggers from
        /// </summary>
        public ILoggerFactory LoggerFactory { get; }

        /// <summary>
        /// The factory to create metrics from
        /// </summary>
        public IMetricFactory MetricFactory { get; }

        /// <summary>
        /// The namespace in Kubernetes the Pod is in
        /// </summary>
        public string KubernetesNamespace { get; private set; }

        /// <summary>
        /// The pod name in Kubernetes
        /// </summary>
        public string KubernetesPodName { get; private set; }

        /// <summary>
        /// The Kubernetes client if in Kubernetes
        /// </summary>
        public KubernetesApiClient KubernetesClient { get; private set; } = null;

        /// <summary>
        /// Is a Kubernetes controller found for the pod
        /// </summary>
        public bool KubernetesControllerFound { get; private set; } = false;

        /// <summary>
        /// The Kubernetes controller name
        /// </summary>
        public string KubernetesControllerName { get; private set; } = null;

        /// <summary>
        /// The name of the type of the controller
        /// </summary>
        public string KubernetesControllerTypeName { get; private set; } = null;

        /// <summary>
        /// The type of the controller
        /// </summary>
        public KubernetesControllerType KubernetesControllerType { get; private set; } = KubernetesControllerType.None;

        /// <summary>
        /// The index number of the stateful set
        /// </summary>
        public int? KubernetesStatefulSetIndex { get; private set; } = null;

        /// <summary>
        /// If the kubernetes controller supports replicas this will contain the desired replica count that is configured
        /// </summary>
        public int? KubernetesDesiredReplicaCount { get; private set; } = null;

        /// <summary>
        /// If the kubernetes controller supports replicas this will contain the replica count that is ready
        /// </summary>
        public int? KubernetesReadyReplicaCount { get; private set; } = null;

        /// <summary>
        /// If the Kubernetes controller supports replicas this will contain the replica count
        /// </summary>
        public int? KubernetesReplicaCount { get; private set; } = null;

        /// <summary>
        /// If true a Kubernetes watch will be configured to notify the derived type of controller scale events
        /// </summary>
        public virtual bool NotifyOfScaleEvents { get; } = false;
        #endregion
        #region Methods
        /// <summary>
        /// A method that the primary container must call to start processing
        /// </summary>
        public void StartContainer()
        {
            _processingTask = ProcessingLoopAsync(_baseProbeConfiguration);
        }

        /// <summary>
        /// Determines if it is Kubernetes and if so retrieves the information
        /// </summary>
        private async Task ConfigureKubernetesDataAsync()
        {
            IsKubernetes = false;

            foreach (var key in Environment.GetEnvironmentVariables().Keys)
            {
                if (key.ToString().Contains("KUBERNETES", StringComparison.OrdinalIgnoreCase))
                {
                    IsKubernetes = true;
                    continue;
                }
            }

            if(IsKubernetes)
            {
                try
                {
                    KubernetesClient = new KubernetesApiClient();

                    KubernetesNamespace = KubernetesClient.NamespaceName;
                    KubernetesPodName = KubernetesClient.PodName;

                    var controller = await KubernetesClient.GetPodControllerDetailsAsync(KubernetesNamespace, KubernetesPodName, ContainerLifecycle.CancellationToken).ConfigureAwait(false);

                    KubernetesControllerFound = controller.ControllerFound;
                    KubernetesControllerName = controller.ControllerName;
                    KubernetesControllerTypeName = controller.ControllerType;
                    KubernetesControllerType = controller.Type;

                    if (KubernetesControllerType == KubernetesControllerType.StatefulSet)
                    {
                        var elements = KubernetesPodName.Split("-", StringSplitOptions.RemoveEmptyEntries);
                        var indexText = elements[^1];

                        KubernetesStatefulSetIndex = int.Parse(indexText);
                    }

                    if (KubernetesControllerType == KubernetesControllerType.ReplicaSet || KubernetesControllerType == KubernetesControllerType.StatefulSet || KubernetesControllerType == KubernetesControllerType.DaemonSet || KubernetesControllerType == KubernetesControllerType.ReplicationController)
                    {
                        var replicas = await KubernetesClient.GetReplicaInformationAsync(KubernetesNamespace, KubernetesControllerName, KubernetesControllerType, ContainerLifecycle.CancellationToken).ConfigureAwait(false);

                        KubernetesDesiredReplicaCount = replicas.DesiredReplicaCount;
                        KubernetesReadyReplicaCount = replicas.ReadyReplicaCount;
                        KubernetesReplicaCount = replicas.ReplicaCount;

                        if (NotifyOfScaleEvents)
                        {
                            _watch = await KubernetesClient.SubscribeToReplicaInformationAsync(KubernetesNamespace, KubernetesControllerName, KubernetesControllerType, "base", ControllerWatchHandler, ContainerLifecycle.CancellationToken).ConfigureAwait(false);

                            if (_watch.InitialDetails != null)
                            {
                                if (_watch.InitialDetails.ReplicaCount.HasValue) KubernetesReplicaCount = _watch.InitialDetails.ReplicaCount.Value;
                                if (_watch.InitialDetails.ReadyReplicaCount.HasValue) KubernetesReadyReplicaCount = _watch.InitialDetails.ReadyReplicaCount.Value;
                                if (_watch.InitialDetails.DesiredReplicaCount.HasValue) KubernetesDesiredReplicaCount = _watch.InitialDetails.DesiredReplicaCount.Value;
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    Logger.LogError(e, "Failed attempting to leverage Kubernetes APIs, continuing without, controller data will not be accurate");
                }
            }
        }

        /// <summary>
        /// A handler that receives updates to the Kubernetes replica count
        /// </summary>
        /// <param name="details">The details of the new replica set</param>
        /// <param name="userState">The user state for the watch it is associated with</param>
        private void ControllerWatchHandler(ReplicaDetails details, object userState)
        {
            if (userState is string stateString && string.Equals(stateString, "base", StringComparison.Ordinal))
            {
                var previousDesiredReplicaCount = KubernetesDesiredReplicaCount;
                var previousReadyReplicaCount = KubernetesReadyReplicaCount;
                var previousReplicaCount = KubernetesReplicaCount;

                if (details.ReplicaCount.HasValue) KubernetesReplicaCount = details.ReplicaCount.Value;
                if (details.ReadyReplicaCount.HasValue) KubernetesReadyReplicaCount = details.ReadyReplicaCount.Value;
                if (details.DesiredReplicaCount.HasValue) KubernetesDesiredReplicaCount = details.DesiredReplicaCount.Value;

                KubernetesReplicaChange(previousReplicaCount, previousDesiredReplicaCount, previousReadyReplicaCount, KubernetesReplicaCount, KubernetesDesiredReplicaCount, KubernetesReadyReplicaCount);
            }
        }

        /// <summary>
        /// A method that is used to notify of replica count changes in the controller
        /// </summary>
        /// <param name="previousReplicaCount">The number of replicas that were previously active</param>
        /// <param name="previousDesiredReplicaCount">The number of replicas that were previously desired</param>
        /// <param name="previousReadyReplicaCount">The number of replicas that were previously ready</param>
        /// <param name="newReplicaCount">The number of replicas that are now present</param>
        /// <param name="newDesiredReplicaCount">The number of replcas that are now desired</param>
        /// <param name="newReadyReplicaCount">The number of replicas that are now ready</param>
        protected virtual void KubernetesReplicaChange(int? previousReplicaCount, int? previousDesiredReplicaCount, int? previousReadyReplicaCount, int? newReplicaCount, int? newDesiredReplicaCount, int? newReadyReplicaCount)
        {

        }

        /// <summary>
        /// Primary processing loop
        /// </summary>
        /// <param name="probeConfiguration">Health and availability probe configuration</param>
        private async Task ProcessingLoopAsync(IProbeConfiguration probeConfiguration)
        {
            Task = Task.WhenAny(ContainerLifecycle.Task, CustomTask);

            await ConfigureKubernetesDataAsync().ConfigureAwait(false);

            Logger.LogInformation($"IsKubernetes: { IsKubernetes }");
            Logger.LogInformation($"PodName: { KubernetesPodName }");
            Logger.LogInformation($"Namespace: {KubernetesNamespace}");
            Logger.LogInformation($"Controller Found: {KubernetesControllerFound}");

            if (KubernetesControllerFound)
            {
                Logger.LogInformation($"Controller Name: {KubernetesControllerName}");
                Logger.LogInformation($"Controller Type Name: {KubernetesControllerTypeName}");
                Logger.LogInformation($"Controller Type: {KubernetesControllerType}");
                if (KubernetesStatefulSetIndex.HasValue) Logger.LogInformation($"StatefulSet Index: {KubernetesStatefulSetIndex.Value}");
                if(KubernetesReplicaCount.HasValue) Logger.LogInformation($"Replica Count: {KubernetesReplicaCount}");
                if (KubernetesReadyReplicaCount.HasValue) Logger.LogInformation($"Ready Replica Count: {KubernetesReadyReplicaCount}");
                if (KubernetesDesiredReplicaCount.HasValue) Logger.LogInformation($"Desired Replica Count: {KubernetesDesiredReplicaCount}");                
            }

            IAvailabilityContainerProbe availabilityBroker = null;
            IHealthContainerProbe healthBroker = null;
            var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(CustomCancellationToken, ContainerCancellationToken);
            var unobserved = new UnobservedHandlers(UnhandledExceptionRaised);

            try
            {
                Logger.LogInformation("Creating probes");
                var probes = CreateProbes(probeConfiguration);
                availabilityBroker = probes.Item1;
                healthBroker = probes.Item2;
                Logger.LogInformation("Probes Created");

                try
                {
                    Logger.LogInformation("Starting probes");
                    if (availabilityBroker != null) await availabilityBroker.StartAsync(CancellationToken).ConfigureAwait(false);
                    if (healthBroker != null && availabilityBroker != healthBroker) await healthBroker.StartAsync(CancellationToken).ConfigureAwait(false);
                    Logger.LogInformation("Requested probes started");
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error starting brokers");
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error creating probes, continueing without probes");
            }

            var startupLogicSuccess = false;

            try
            {
                Logger.LogInformation("Executing startup logic");
                await StartupAsync(CancellationToken).ConfigureAwait(false);
                Logger.LogInformation("Completed startup logic");
                startupLogicSuccess = true;                                
            }
            catch(Exception e)
            {
                Logger.LogError(e, "Error processing container startup logic");
            }

            if (startupLogicSuccess)
            {
                try
                {
                    Logger.LogInformation("Starting container logic");
                    var executionTask = ExecuteAsync(CancellationToken);
                    Logger.LogInformation("Starting sleep loop");

                    while (!CancellationToken.IsCancellationRequested && !Task.IsCompleted)
                    {
                        await SleepInChunks(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
                    }

                    Logger.LogInformation("Completed sleep loop");
                    await executionTask.ConfigureAwait(false);
                    Logger.LogInformation("Completed container logic");
                }
                catch(Exception e)
                {
                    Logger.LogError(e, "Error executing container logic");
                }
            }
            
            await ShutdownAsync(startupLogicSuccess, CancellationToken).ConfigureAwait(false);            
            Logger.LogInformation("Stopping brokers");

            if (availabilityBroker != healthBroker)
            {
                try
                {
                    if (availabilityBroker != null)
                    {
                        Logger.LogInformation("Stopping availability broker");
                        await availabilityBroker.StopAsync(CancellationToken).ConfigureAwait(false);
                        (availabilityBroker as IDisposable)?.Dispose();
                        Logger.LogInformation("Stopped availability broker");
                    }
                }
                catch(Exception e)
                {
                    Logger.LogError(e, "Error stopping availability broker");
                }

                try
                {
                    if (healthBroker != null)
                    {
                        Logger.LogInformation("Stopping health broker");
                        await healthBroker.StopAsync(CancellationToken).ConfigureAwait(false);
                        (healthBroker as IDisposable)?.Dispose();
                        Logger.LogInformation("Stopped health broker");
                    }
                }
                catch(Exception e)
                {
                    Logger.LogError(e, "Error stopping health broker");
                }
            }
            else if(availabilityBroker != null)
            {
                try
                {
                    Logger.LogInformation("Stopping availability Broker");
                    await availabilityBroker.StopAsync(CancellationToken).ConfigureAwait(false);
                    (availabilityBroker as IDisposable)?.Dispose();
                    Logger.LogInformation("Stopped availability broker");
                }
                catch(Exception e)
                {
                    Logger.LogError(e, "Error stopping availability broker");
                }
            }

            Logger.LogInformation("Brokers stopped");
        }

        /// <summary>
        /// A method that can be overridden by the derived types to execute operations at startup
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for container shutdown</param>
        protected virtual Task StartupAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// A method that performs the primary operations of the container
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for container shutdown</param>
        protected abstract Task ExecuteAsync(CancellationToken cancellationToken);

        /// <summary>
        /// A method that can be overridden by the derived types to execute operations to shutdown
        /// </summary>
        /// <param name="startupSuccessful">True if startup was successful</param>
        /// <param name="cancellationToken">A token to monitor for container shutdown</param>
        protected virtual Task ShutdownAsync(bool startupSuccessful, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// A helper method to sleep in chunks until the time period expires, the cancellation token is triggered or the task completes or container is shutting down
        /// </summary>
        /// <param name="duration">The duration to sleep for</param>
        /// <param name="chunkDuration">The duration of each chunk when sleeping</param>
        /// <param name="task">A task to monitor for completion</param>
        /// <param name="cancellationToken">A cancellation token to monitor for abort requests</param>
        protected async Task SleepInChunks(TimeSpan duration, TimeSpan? chunkDuration = default, Task task = null, CancellationToken cancellationToken = default)
        {
            if (chunkDuration == null || chunkDuration.Value <= TimeSpan.Zero) chunkDuration = TimeSpan.FromMilliseconds(50);

            Guard.NotLessThan(nameof(duration), duration.TotalMilliseconds, 1);

            var sleepUntil = DateTime.UtcNow.Add(duration);
            var sleepCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CancellationToken);            
            var sleepTask = task == null ? Task : Task.WhenAny(Task, task);

            while(DateTime.UtcNow < sleepUntil && !sleepCancellationSource.Token.IsCancellationRequested && !(sleepTask?.IsCompleted ?? false))
            {
                var remaining = sleepUntil - DateTime.UtcNow;

                if (remaining > TimeSpan.Zero)
                {
                    var sleepTime = remaining > chunkDuration.Value ? chunkDuration.Value : remaining;

                    try
                    {
                        await Task.Delay(sleepTime, sleepCancellationSource.Token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        // The task was cancelled so continue as it is not an unexpected error
                    }
                }
            }
        }

        /// <summary>
        /// Can be overridden to provide a cancellation token to monitor for abort requests that will terminate the container if triggered
        /// </summary>
        protected virtual CancellationToken CustomCancellationToken => CancellationToken.None;

        /// <summary>
        /// A task that can be overridden to provide a task that when completed will trigger the container to end
        /// </summary>
        protected virtual Task CustomTask => ContainerTask;

        /// <summary>
        /// Invoked when application or task unhandled exceptions are raised
        /// </summary>
        /// <param name="sender">The source of the exception handler</param>
        /// <param name="exception">The exception object that was raised</param>
        /// <param name="isTerminating">True if the app domain is terminating</param>
        /// <param name="sourceType">The source of the invocation (app domain or task scheduler)</param>
        /// <returns>True if the exception should be concerned handled</returns>
        protected virtual bool UnhandledExceptionRaised(object sender, Exception exception, bool isTerminating, UnobservedType sourceType)
        {
            return false;
        }

        /// <summary>
        /// A factory that can be overridden to create non standard availability probe brokers
        /// </summary>
        /// <param name="probeConfiguration">The probe configuration details</param>
        protected virtual (IAvailabilityContainerProbe, IHealthContainerProbe) CreateProbes(IProbeConfiguration probeConfiguration)
        {
            IAvailabilityContainerProbe availabilityProbe = null;
            IHealthContainerProbe healthProbe = null;

            if (probeConfiguration.UseTcp)
            {
                if (probeConfiguration.AvailabilityPort.HasValue)
                {
                    availabilityProbe = new AvailabilityContainerProbe(probeConfiguration.AvailabilityIPAddress, probeConfiguration.AvailabilityPort.Value, probeConfiguration.AvailabilityProbeInterval ?? TimeSpan.FromSeconds(5), this);
                }

                if (probeConfiguration.HealthPort.HasValue)
                {
                    healthProbe = new HealthContainerProbe(probeConfiguration.HealthIPAddress, probeConfiguration.HealthPort.Value, probeConfiguration.HealthProbeInterval ?? TimeSpan.FromSeconds(5), this);
                }
            }
            else if(probeConfiguration.AvailabilityPort.HasValue || probeConfiguration.HealthPort.HasValue)
            {                
                if(probeConfiguration.AvailabilityPort.HasValue && probeConfiguration.AvailabilityPort == probeConfiguration.HealthPort)
                {
                    if (probeConfiguration.AvailabilityPort.HasValue)
                    {
                        var probe = new KestrelDualProbe(new KestrelHostConfiguration 
                        { 
                            Address = probeConfiguration.AvailabilityIPAddress, 
                            Port = probeConfiguration.AvailabilityPort.Value, 
                            Certificate = probeConfiguration.Certificate, 
                            KeepAlive = TimeSpan.FromSeconds(90), 
                            MaximumConcurrentConnections = 10, 
                            UseNagle = false
                        }, LoggerFactory, this, this);

                        availabilityProbe = probe;
                        healthProbe = probe;
                    }
                }
                else 
                {
                    if (probeConfiguration.AvailabilityPort.HasValue)
                    {
                        availabilityProbe = new KestrelAvailabilityProbe(new KestrelHostConfiguration
                        {
                            Address = probeConfiguration.AvailabilityIPAddress,
                            Port = probeConfiguration.AvailabilityPort.Value,
                            Certificate = probeConfiguration.Certificate,
                            KeepAlive = TimeSpan.FromSeconds(90),
                            MaximumConcurrentConnections = 10,
                            UseNagle = false
                        }, LoggerFactory, this);
                    }

                    if(probeConfiguration.HealthPort.HasValue)
                    {
                        healthProbe = new KestrelHealthProbe(new KestrelHostConfiguration
                        {
                            Address = probeConfiguration.HealthIPAddress,
                            Port = probeConfiguration.HealthPort.Value,
                            Certificate = probeConfiguration.Certificate,
                            KeepAlive = TimeSpan.FromSeconds(90),
                            MaximumConcurrentConnections = 10,
                            UseNagle = false
                        }, LoggerFactory, this);
                    }
                }                
            }

            return (availabilityProbe, healthProbe);
        }

        /// <summary>
        /// Polled at intervals to test if the service is healthy
        /// </summary>
        /// <returns>Returns true if the service is healthy</returns>
        public virtual Task<bool> IsHealthyAsync()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Determines if the service is available
        /// </summary>
        /// <returns>Returns true if available</returns>
        public virtual Task<bool> IsAvailableAsync()
        {
            return Task.FromResult(true);
        }
        #endregion
    }
}
