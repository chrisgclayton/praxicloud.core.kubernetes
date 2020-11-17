# PraxiCloud Core Kubernetes
PraxiCloud Libraries are a set of common utilities and tools for general software development that simplify common development efforts for software development. The core kestrel library contains easy to use tools such as middleware components, availability and health probes.



# Installing via NuGet

Install-Package PraxiCloud-Core-Kubernetes



# API Interaction



## Key Types and Interfaces

|Class| Description | Notes |
| ------------- | ------------- | ------------- |
|**KubernetesApiClient**|A client for interacting with a clusters resources through the Kubernetes API. Instantiating without a file will attempt to connect to the cluster the client is residing in.<br />***PodName*** Is the name of the pod, assuming the POD_NAME environment variable is set with the downward API.<br />***NamespaceName*** is the name of the namespace, assuming the POD_NAMESPACE environment variable is set with the downward API.<br />***GetPodsInNamespaceAsync*** retrieves a list of the pods that reside in the specified namespace.<br />***GetPodControllerDetailsAsync*** retrieves information about the pod and tries to identify the controller that owns it based on the metadata.<br />***GetReplicaInformationAsync*** retrieves the replica information associated with the specified controller. Currently this supports StatefulSets, Replicasets, ReplicationControllers and DaemonSets.<br />***SubscribeToReplicaInformationAsync*** sets up a watch for changes in the replica information. This may be resulting from scale events etc. This returns a KubernetesWatch.| To interact with the cluster resources it may require additional permissions, depending on the installation.<br /><br />There are many more methods available but the most interesting ones are listed here. |

## Sample Usage

### Query the Replica Details for the Current Pods Controller

```csharp
var client = new KubernetesApiClient();

var controllerDetails = await client.GetPodControllerDetailsAsync(client.NamespaceName, client.PodName, CancellationToken.None).ConfigureAwait(false);
var replicaDetails = await client.GetReplicaInformationAsync(client.NamespaceName, controllerDetails.ControllerName, controllerDetails.ControllerType, CancellationToken.None).ConfigureAwait(false);

Console.WriteLine($"There are { replicaDetails.ReplicaCount.Value } replicas");
```

## Additional Information

For additional information the Visual Studio generated documentation found [here](./documents/praxicloud.core.kubernetes/praxicloud.core.kubernetes.xml), can be viewed using your favorite documentation viewer.

# Containers



## Key Types and Interfaces

|Class| Description | Notes |
| ------------- | ------------- | ------------- |
|**ContainerBase**|A base container that exposes easy overrides to handle probe checks, handles unhandled exceptions and notifies of scale events, if associated with a compatible controller.| This is a base container type that can be used with Kubernetes or other orchestrators. |

## Sample Usage

### Creating a Simple Container with Health and Availability Probes

```csharp
static void Main(string[] args)
{
    var probes = new ProbeConfiguration
    {
	AvailabilityIPAddress = IPAddress.Any,
	AvailabilityPort = 10281,
	AvailabilityProbeInterval = TimeSpan.FromSeconds(1),
	HealthIPAddress = IPAddress.Any,
	HealthPort = 10281,
	HealthProbeInterval = TimeSpan.FromSeconds(1),
	UseTcp = false
    };

    var diagnostics = new DefaultDiagnosticsConfiguration("demo", new DefaultDiagnosticsConfiguration.DefaultLoggerConfiguration
    {
	IncludeColors = true,
	IncludeConsoleLogger = true,
	IncludeDebugLogger = false,
	IncludeScopes = true,
	Level = LogLevel.Trace
    },
    new DefaultDiagnosticsConfiguration.DefaultMetricConfiguration
    {
	IncludeApplicationInsights = false,
	IncludeConsole = true,
	IncludeDebug = false,
	IncludeLabels = true,
	IncludePrometheus = true,
	InstrumentationKey = null,
	PrometheusPort = 10282,
	ReportingInterval = 5
    });

    var container = new DemoContainer("democontainer", probes, diagnostics);

    container.StartContainer();

    Task.WhenAll(ContainerLifecycle.Task, container.Task).GetAwaiter().GetResult();

}


public class DemoContainer : ContainerBase
{
    public DemoContainer(string containerName, IProbeConfiguration probeConfiguration, IDiagnosticsConfiguration diagnosticsConfiguration) : base(containerName, probeConfiguration, diagnosticsConfiguration)
    {
    
    }
    
    protected override Task StartupAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("In the derived startup async");
    
        return Task.CompletedTask;
    }
    
    protected override Task ShutdownAsync(bool startupSuccessful, CancellationToken cancellationToken)
    {
        Logger.LogInformation("In the derived shutdown async");
    
        return Task.CompletedTask;
    }
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var variableList = Environment.GetEnvironmentVariables();
    
        Logger.LogInformation("Environment Variables Start");
    
        foreach (var variable in variableList.Keys)
        {
    	    Logger.LogInformation($"    { variable }: {variableList[variable]}");    
        }
    
        Logger.LogInformation("Environment Variables Done");
    
        
        await ContainerLifecycle.Task.ConfigureAwait(false);
    }
    
    protected override void KubernetesReplicaChange(int? previousReplicaCount, int? previousDesiredReplicaCount, int? previousReadyReplicaCount, int? newReplicaCount, int? newDesiredReplicaCount, int? newReadyReplicaCount)
    {
        Logger.LogInformation("Kubernetes Replica Change Invoked");
        Logger.LogInformation($"Previous Replica Count {previousReplicaCount}, Previous Desired Replica Count {previousDesiredReplicaCount}, Previous Ready Replica Count {previousReadyReplicaCount}, New Replica Count {newReplicaCount}, New Desired Replica Count {newDesiredReplicaCount}, New Ready Replica Count {newReadyReplicaCount}");
    }

    protected override bool UnhandledExceptionRaised(object sender, Exception exception, bool isTerminating, UnobservedType sourceType)
    {
       Console.WriteLine("An unhandled exception occurred");
    
       return false;
    }
    
    public override Task<bool> IsHealthyAsync()
    {
        return Task.FromResult(true);
    }
    
    public override Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(true);
    }
}
```

## Additional Information

For additional information the Visual Studio generated documentation found [here](./documents/praxicloud.core.kubernetes/praxicloud.core.kubernetes.xml), can be viewed using your favorite documentation viewer.

