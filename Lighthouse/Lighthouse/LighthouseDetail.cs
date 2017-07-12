using Akka.Actor;

namespace Lighthouse
{
    public class LighthouseDetail
    {
        public string Address { get; }

        public ActorSystem ActorSystem { get; }

        public LighthouseDetail(string address, ActorSystem actorSystem)
        {
            Address = address;
            ActorSystem = actorSystem;
        }
    }
}
