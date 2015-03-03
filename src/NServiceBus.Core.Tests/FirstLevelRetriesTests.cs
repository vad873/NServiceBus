namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.FirstLevelRetries;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class FirstLevelRetriesTests
    {
        [Test]
        public void ShouldNotPerformFLROnMessagesThatCantBeDeserialized()
        {
            var behavior = new FirstLevelRetriesBehavior(new FirstLevelRetryPolicy(0), new BusNotifications());

            Assert.Throws<MessageDeserializationException>(() => behavior.Invoke(null, () =>
            {
                throw new MessageDeserializationException("test");
            }));
        }

        [Test]
        public void ShouldPerformFLRIfThereAreRetriesLeftToDo()
        {
            var behavior = new FirstLevelRetriesBehavior(new FirstLevelRetryPolicy(1), new BusNotifications());
            var context = CreateContext("someid");

            behavior.Invoke(context, () =>
            {
                throw new Exception("test"); 
            });

            Assert.False(context.MessageHandledSuccessfully);
        }

        [Test]
        public void ShouldBubbleTheExceptionUpIfThereAreNoMoreRetriesLeft()
        {
            var behavior = new FirstLevelRetriesBehavior(new FirstLevelRetryPolicy(0), new BusNotifications());
            var context = CreateContext("someid");

            Assert.Throws<Exception>(() => behavior.Invoke(context, () =>
            {
                throw new Exception("test");
            }));

            //should set the retries header to capture how many flr attempts where made
            Assert.AreEqual("0", context.PhysicalMessage.Headers[Headers.FLRetries]);
        }

        [Test]
        public void ShouldRememberRetryCountBetweenRetries()
        {
            var behavior = new FirstLevelRetriesBehavior(new FirstLevelRetryPolicy(1), new BusNotifications());

            behavior.Invoke(CreateContext("someid"), () =>
            {
                throw new Exception("test");
            });



            Assert.Throws<Exception>(()=> behavior.Invoke(CreateContext("someid"), () =>
            {
                throw new Exception("test");
            }));
        }

        [Test]
        public void ShouldClearStorageAfterGivingUp()
        {
            var behavior = new FirstLevelRetriesBehavior(new FirstLevelRetryPolicy(1), new BusNotifications());

            behavior.Invoke(CreateContext("someid"), () => { throw new Exception("test"); });
         
            //this should clear the storage since we gave up
            Assert.Throws<Exception>(() => behavior.Invoke(CreateContext("someid"), () =>{throw new Exception("test");}));
        
            //so this one should not blow
            behavior.Invoke(CreateContext("someid"), () => { throw new Exception("test"); });
        }

        [Test]
        public void ShouldRaiseBusNotificationsForFLR()
        {
            var notifications = new BusNotifications();
            var behavior = new FirstLevelRetriesBehavior(new FirstLevelRetryPolicy(1), notifications);

            var notificationFired = false;

            notifications.Errors.MessageHasFailedAFirstLevelRetryAttempt.Subscribe(flr =>
            {
                Assert.AreEqual(0, flr.RetryAttempt);
                Assert.AreEqual("test", flr.Exception.Message);
                Assert.AreEqual("someid", flr.Headers[Headers.MessageId]);

                notificationFired = true;
            })
                ;
            behavior.Invoke(CreateContext("someid"), () =>
            {
                throw new Exception("test");
            });


            Assert.True(notificationFired);
        }


        PhysicalMessageProcessingStageBehavior.Context CreateContext(string messageId)
        {
            var context = new PhysicalMessageProcessingStageBehavior.Context(new TransportReceiveContext(new ReceivedMessage(messageId, new Dictionary<string, string>(), new MemoryStream()), null));
            return context;
        }
    }
}