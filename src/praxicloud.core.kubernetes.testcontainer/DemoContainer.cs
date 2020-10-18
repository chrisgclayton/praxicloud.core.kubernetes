using Microsoft.Extensions.Logging;
using praxicloud.core.containers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace praxicloud.core.kubernetes.testcontainer
{
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
    }
}
