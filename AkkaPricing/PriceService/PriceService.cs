﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Query;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Configuration.Hocon;
using Akka.Routing;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Newtonsoft.Json.Linq;
using ConfigurationException = Akka.Configuration.ConfigurationException;
using StatelessService = Microsoft.ServiceFabric.Services.Runtime.StatelessService;

namespace PriceService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class PriceService : StatelessService
    {
        public PriceService(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {

            var port = Context.CodePackageActivationContext.GetEndpoint("AkkaNodeEnpoint").Port;
            var ipaddress = FabricRuntime.GetNodeContext().IPAddressOrFQDN;


            ActorSystem system = await CreateActorSystem(ipaddress, port, cancellationToken);
            //var router = ActorSystem.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), "tasker");
            //SystemActors.CommandProcessor = ActorSystem.ActorOf(Props.Create(() => new CommandProcessor(router)),
            //    "commands");
            //SystemActors.SignalRActor = ActorSystem.ActorOf(Props.Create(() => new SignalRActor()), "signalr");

            long iterations = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ServiceEventSource.Current.ServiceMessage(this.Context, "Working-{0}", ++iterations);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        private async Task<ActorSystem> CreateActorSystem(string ipAddress, int? specifiedPort,CancellationToken cancellationToken)
        {
            var systemName = "pricing";
            var sfAppName = "";
            var sfSvcName = "";
            var section = (AkkaConfigurationSection)ConfigurationManager.GetSection("akka");
            var clusterConfig = section.AkkaConfig;

            var lighthouseConfig = clusterConfig.GetConfig("lighthouse");
            if (lighthouseConfig != null)
            {
                systemName = lighthouseConfig.GetString("actorsystem", systemName);
                sfAppName = lighthouseConfig.GetString("sfappname","");
                sfSvcName = lighthouseConfig.GetString("sfsvcname", "");
            }

            var remoteConfig = clusterConfig.GetConfig("akka.remote");
            ipAddress = ipAddress ??
                        remoteConfig.GetString("helios.tcp.public-hostname") ??
                        "127.0.0.1"; //localhost as a final default
            int port = specifiedPort ?? remoteConfig.GetInt("helios.tcp.port");

            if (port == 0) throw new ConfigurationException("Need to specify an explicit port for Lighthouse. Found an undefined port or a port value of 0 in App.config.");


            var addresses = await GetSeedAddresses(sfAppName, sfSvcName, cancellationToken);

            var seeds = clusterConfig.GetStringList("akka.cluster.seed-nodes");
            foreach (string address in addresses)
            {

                if (!seeds.Contains(address))
                {
                    seeds.Add(address);
                }
            }
            

            var injectedClusterConfigString = seeds.Aggregate("akka.cluster.seed-nodes = [", (current, seed) => current + (@"""" + seed + @""", "));
            injectedClusterConfigString += "]";

            //var finalConfig = ConfigurationFactory.ParseString(injectedClusterConfigString).WithFallback(clusterConfig);

            var finalConfig = ConfigurationFactory.ParseString(
                    string.Format(@"akka.remote.helios.tcp.public-hostname = {0} 
akka.remote.helios.tcp.port = {1}", ipAddress, port))
                .WithFallback(ConfigurationFactory.ParseString(injectedClusterConfigString))
                .WithFallback(clusterConfig);

            return ActorSystem.Create(systemName, finalConfig);
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
