
namespace NServiceBus.AcceptanceTests.DependencyInjection
{
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    [Explicit]
    public class When_a_known_interface_is_already_registered : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_not_overwrite_it()
        {
            var context = Scenario.Define<Context>()
                                  .WithEndpoint<Endpoint>(b => b.Given((bus, c) => bus.SendLocal(new Message())))
                                  .Done(c => c.Handled)
                                  .Run();
            Assert.IsTrue(context.IsServiceInitialized);
        }

        public class Context : ScenarioContext
        {
            public bool Handled { get; set; }
            public bool IsServiceInitialized { get; set; }
        }


        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class Initialization : INeedInitialization
            {
                public void Init()
                {
                    Configure
                        .Instance
                        .Configurer
                        .ConfigureComponent<Service>(DependencyLifecycle.SingleInstance);
                }
            }

            public class Service : IWantToRunWhenBusStartsAndStops
            {

                public bool Initialized;

                public void Start()
                {
                    Initialized = true;
                }

                public void Stop()
                {
                }
            }

            class Handler : IHandleMessages<Message>
            {
                public Context Context { get; set; }
                public Service Service { get; set; }

                public void Handle(Message message)
                {
                    Context.Handled = true;
                    Context.IsServiceInitialized = Service.Initialized;
                }
            }

        }

        public class Message : IMessage
        {
        }

    }

}