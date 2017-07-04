using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace Lighthouse
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class Lighthouse : StatelessService
    {
        private AkkaClusterCommunicationListener _listener;

        public Lighthouse(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            yield return new ServiceInstanceListener(initParams =>
            {
                _listener = new AkkaClusterCommunicationListener();
                _listener.Initialize(initParams.CodePackageActivationContext, "SeedEnpoint");

                return _listener;
            });
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        //protected override async Task RunAsync(CancellationToken cancellationToken)
        //{

        //    var endpoint = Context.CodePackageActivationContext.GetEndpoint("SeedEnpoint");

        //    cancellationToken.ThrowIfCancellationRequested();
        //    _lighthouseSystem = LighthouseHostFactory.LaunchLighthouse(FabricRuntime.GetNodeContext().IPAddressOrFQDN, endpoint.Port);
        //    //_lighthouseSystem = LighthouseHostFactory.LaunchLighthouse(null, endpoint.Port);

        //    await Task.Delay(-1, cancellationToken);
        //}

    }
}
