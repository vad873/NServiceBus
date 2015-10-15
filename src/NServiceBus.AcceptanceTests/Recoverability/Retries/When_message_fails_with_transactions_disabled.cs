namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_message_fails_with_transactions_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_forward_message_to_error_queue()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<TransactionDisabledEndpoint>(b => b.When((bus, c) => bus.SendLocalAsync(new MessageWhichFails())))
                    .AllowSimulatedExceptions()
                    .Done(c => c.ForwardedToErrorQueue)
                    .Run();

            Assert.AreEqual(1, context.Logs.Count(l => l.Message
                .StartsWith($"Moving message '{context.PhysicalMessageId}' to the error queue because processing failed due to an exception:")));
        }

        public class TransactionDisabledEndpoint : EndpointConfigurationBuilder
        {
            public TransactionDisabledEndpoint()
            {
                EndpointSetup<DefaultServer>(configure =>
                {
                    configure.DisableFeature<FirstLevelRetries>();
                    configure.DisableFeature<SecondLevelRetries>();
                    configure.Transactions().Disable().WrapHandlersExecutionInATransactionScope();
                    configure.Transactions().DisableDistributedTransactions();
                });
            }

            public static byte Checksum(byte[] data)
            {
                var longSum = data.Sum(x => (long)x);
                return unchecked((byte)longSum);
            }

            class ErrorNotificationSpy : IWantToRunWhenBusStartsAndStops
            {
                public Context Context { get; set; }

                public BusNotifications BusNotifications { get; set; }

                public Task StartAsync()
                {
                    BusNotifications.Errors.MessageSentToErrorQueue.Subscribe(e =>
                    {
                        Context.ForwardedToErrorQueue = true;
                    });
                    return Task.FromResult(0);
                }

                public Task StopAsync()
                {
                    return Task.FromResult(0);
                }
            }

            class MessageHandler : IHandleMessages<MessageWhichFails>
            {
                public IBus Bus { get; set; }

                public Context Context { get; set; }

                public Task Handle(MessageWhichFails message)
                {
                    Context.PhysicalMessageId = Bus.CurrentMessageContext.Id;
                    throw new SimulatedException();
                }
            }
        }

        public class Context : ScenarioContext
        {
            public bool ForwardedToErrorQueue { get; set; }

            public string PhysicalMessageId { get; set; }
        }

        public class MessageWhichFails : IMessage
        {
        }
    }
}