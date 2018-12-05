using System;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Configuration;
using Shared;
using System.Collections.Generic;

namespace FrameWorkApp
{
    public class TestActor : ReceiveActor
    {
        public TestActor()
        {
            Receive<Dictionary<string, Bla>>(msg => Console.WriteLine($"Received {msg.Count}"));
        }
        protected override void PreStart()
        {
            base.PreStart();
            var mediator = DistributedPubSub.Get(Context.System).Mediator;
            var id = new Dictionary<string, Bla>
            {
                { "1", new Bla { Text = "test" } }
            };
            Context.System.Scheduler.ScheduleTellOnce(
                TimeSpan.FromSeconds(20), mediator,
                new Send("/user/framework-test-actor", id),
                Self);

        }
    }

    class Program
    {
        private static readonly string conf = @"
akka {
    actor {
		provider = ""Akka.Cluster.ClusterActorRefProvider, Akka.Cluster""
        serializers {
		    myserializer = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
        }
        serialization-bindings {
		    ""System.Object"" = myserializer
        }          
    }
    remote {
		log-remote-lifecycle-events = DEBUG
        dot-netty.tcp {
			hostname = ""localhost""
            port = 2556
		}
	}
    cluster{
        seed-nodes = [""akka.tcp://TestCluster@localhost:2555""]
    }
    extensions = [
        ""Akka.Cluster.Tools.Client.ClusterClientReceptionistExtensionProvider, Akka.Cluster.Tools"", 
        ""Akka.Cluster.Tools.PublishSubscribe.DistributedPubSubExtensionProvider,Akka.Cluster.Tools""
    ]
}
";

        static void Main(string[] args)
        {
            var actorSystem = ActorSystem.Create("TestCluster", ConfigurationFactory.ParseString(conf));
            var mediator = DistributedPubSub.Get(actorSystem).Mediator;
            var testActor = actorSystem.ActorOf(Props.Create(() => new TestActor()), "framework-test-actor");
            mediator.Tell(new Put(testActor));
            Console.ReadKey();
        }
    }
}
