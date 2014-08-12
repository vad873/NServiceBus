using NUnit.Framework;

namespace NServiceBus.AcceptanceTests.PubSub
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;

    public class When_publishing_an_event_with_multiple_inheritance
    {
        [Test]
        public void All_message_handlers_should_be_invoked()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<Publisher>(b =>
                    b.Given(bus => bus.Send(new Publisher.MyEvent())))
                .WithEndpoint<Subscriber>()
                .Done(c => c.FirstEventHandled && c.SecondEventHandled)
                .Run(TimeSpan.FromSeconds(15));

            Assert.True(context.FirstEventHandled, "The first message failed.");
            Assert.True(context.SecondEventHandled, "The second message failed.");
        }

        public class Context : ScenarioContext
        {
            public bool FirstEventHandled { get; set; }

            public bool SecondEventHandled { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public class MyEvent : IMyFirstEvent, IMySecondEvent
            {
            }

            public Publisher()
            {
                EndpointSetup<DefaultServer>()
                    .AddMapping<MyEvent>(typeof(Subscriber));
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyFirstHandler : IHandleMessages<IMyFirstEvent>
            {
                public Context Context { get; set; }

                public void Handle(IMyFirstEvent message)
                {
                    Context.FirstEventHandled = true;
                }
            }

            public class MySecondHandler : IHandleMessages<IMySecondEvent>
            {
                public Context Context { get; set; }

                public void Handle(IMySecondEvent message)
                {
                    Context.SecondEventHandled = true;
                }
            }
        }

        public interface IMyFirstEvent : IMessage // Using message to avoid checks for sending events
        {
        }

        public interface IMySecondEvent : IMessage
        {
        }
    }
}