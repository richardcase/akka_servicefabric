using System;
using System.Collections.Generic;
using System.Configuration;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Configuration;
using Akka.Configuration.Hocon;
using Microsoft.ServiceFabric.Services.Client;
using Newtonsoft.Json.Linq;
using ConfigurationException = Akka.Configuration.ConfigurationException;

namespace Lighthouse
{
    public static class LighthouseHostFactory
    {
        public static async Task<LighthouseDetail> LaunchLighthouse(string ipAddress, int? specifiedPort, CancellationToken cancellationToken)
        {
            var systemName = "lighthouse";
            var sfAppName = "";
            var sfSvcName = "";
            var section = (AkkaConfigurationSection)ConfigurationManager.GetSection("akka");
            var clusterConfig = section.AkkaConfig;

            var lighthouseConfig = clusterConfig.GetConfig("lighthouse");
            if (lighthouseConfig != null)
            {
                systemName = lighthouseConfig.GetString("actorsystem", systemName);
                sfAppName = lighthouseConfig.GetString("sfappname", "");
                sfSvcName = lighthouseConfig.GetString("sfsvcname", "");
            }

            var remoteConfig = clusterConfig.GetConfig("akka.remote");
            ipAddress = ipAddress ??
                        remoteConfig.GetString("helios.tcp.public-hostname") ??
                        "127.0.0.1"; //localhost as a final default
            int port = specifiedPort ?? remoteConfig.GetInt("helios.tcp.port");

            if (port == 0) throw new ConfigurationException("Need to specify an explicit port for Lighthouse. Found an undefined port or a port value of 0 in App.config.");

            var selfAddress = string.Format("akka.tcp://{0}@{1}:{2}", systemName, ipAddress, port);
           
            var seeds = clusterConfig.GetStringList("akka.cluster.seed-nodes");
            if (!seeds.Contains(selfAddress))
            {
                seeds.Add(selfAddress);
            }

            //try
            //{
            //    var otherSeedAddresses = await GetSeedAddresses(sfAppName, sfSvcName, cancellationToken);
            //    foreach (string address in otherSeedAddresses)
            //    {
            //        Console.WriteLine("Seed address: {0}", address);
            //        //    if (!seeds.Contains(address))
            //        //    {
            //        //        seeds.Add(address);
            //        //    }
            //    }
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("Ignoring seed error");
            //    Console.WriteLine(e);
                
            //}


            var injectedClusterConfigString = seeds.Aggregate("akka.cluster.seed-nodes = [", (current, seed) => current + (@"""" + seed + @""", "));
            injectedClusterConfigString += "]";

            var finalConfig = ConfigurationFactory.ParseString(
                    string.Format(@"akka.remote.helios.tcp.public-hostname = {0} 
akka.remote.helios.tcp.port = {1}", ipAddress, port))
                .WithFallback(ConfigurationFactory.ParseString(injectedClusterConfigString))
                .WithFallback(clusterConfig);

            return new LighthouseDetail(selfAddress, ActorSystem.Create(systemName, finalConfig));
        }

        private static async Task<List<string>> GetSeedAddresses(string applicationName, string serviceName, CancellationToken cancellationToken)
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
