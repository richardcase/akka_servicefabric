using System.Collections.Generic;
using System.Fabric;
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
                _listener.Initialize(initParams.CodePackageActivationContext, "AkkaSeedEnpoint");

                return _listener;
            });
        }
    }
}
