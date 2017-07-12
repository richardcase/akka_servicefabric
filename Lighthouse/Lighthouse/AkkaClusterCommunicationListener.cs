using System;
using System.Fabric;
using System.Fabric.Description;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Lighthouse.Actors;
using Microsoft.ServiceFabric.Services.Communication.Runtime;

namespace Lighthouse
{
    public class AkkaClusterCommunicationListener : ICommunicationListener, IDisposable
    {

        private ICodePackageActivationContext _context;
        private ActorSystem _lighthouseSystem;

        public int Port { get; set; }


        public void Initialize(ICodePackageActivationContext context, string endpointName)
        {
            _context = context;
            EndpointResourceDescription serviceEndpoint = _context.GetEndpoint(endpointName);
            this.Port = serviceEndpoint.Port;
        }

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            try
            {

                LighthouseDetail lighthouse = await LighthouseHostFactory.LaunchLighthouse(FabricRuntime.GetNodeContext().IPAddressOrFQDN, Port, cancellationToken);

                _lighthouseSystem = lighthouse.ActorSystem;
                _lighthouseSystem.ActorOf(Props.Create(typeof(ClusterListener)), "clusterlistener");
                

                return lighthouse.Address;
            }
            catch (Exception ex)
            {
                //TODO:
                throw;
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            StopActorSystem();
            return Task.FromResult(true);
        }

        public void Abort()
        {
            StopActorSystem();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_lighthouseSystem != null)
                {
                    _lighthouseSystem.Terminate();
                    _lighthouseSystem.Dispose();
                }
            }
        }

        private void StopActorSystem()
        {
            _lighthouseSystem.Terminate();
            _lighthouseSystem.Dispose();
        }


    }
}
