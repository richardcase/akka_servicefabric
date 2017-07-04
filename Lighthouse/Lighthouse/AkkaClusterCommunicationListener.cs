using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Newtonsoft.Json.Linq;

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

        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            try
            {
                List<string> seedAdresses =
                     GetSeedAddresses(_context.ApplicationName, _context.CodePackageName, cancellationToken).Result;

                LighthouseDetail lighthouse = LighthouseHostFactory.LaunchLighthouse(FabricRuntime.GetNodeContext().IPAddressOrFQDN, Port);

                _lighthouseSystem = lighthouse.ActorSystem;
                return Task.FromResult(lighthouse.Address);
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

        private async Task<List<string>> GetSeedAddresses(string applicationName, string serviceName, CancellationToken cancellationToken)
        {
            string fabricName = $"fabric:/{applicationName}/{serviceName}";

            var resolver = ServicePartitionResolver.GetDefault();
            var result = await resolver.ResolveAsync(new Uri(fabricName), new ServicePartitionKey(), cancellationToken);

            List<string> addresses = new List<string>();
            foreach (ResolvedServiceEndpoint resolvedServiceEndpoint in result.Endpoints)
            {
                if (!string.IsNullOrWhiteSpace(resolvedServiceEndpoint.Address))
                {
                    var address = JObject.Parse(resolvedServiceEndpoint.Address);
                    foreach (var endpoint in address["Endpoints"])
                    {
                        var endPointAddress = endpoint.First().Value<string>();
                        addresses.Add(endPointAddress);
                    }
                }
            }

            return addresses;
        }
    }
}
