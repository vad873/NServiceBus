namespace NServiceBus.Features
{
    using ConsistencyGuarantees;
    using Transports;

    class StoreFaultsInErrorQueue : Feature
    {
        public StoreFaultsInErrorQueue()
        {
            EnableByDefault();
            Prerequisite(context =>
            {
                var b = !context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
                return b;
            }, "Send only endpoints can't be used to forward received messages to the error queue as the endpoint requires receive capabilities");
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            var errorQueue = ErrorQueueSettings.GetConfiguredErrorQueue(context.Settings);
            context.Settings.Get<QueueBindings>().BindSending(errorQueue);

            if (context.Settings.GetRequiredTransactionModeForReceives() == TransportTransactionMode.None)
            {
                context.Pipeline.Register(new MoveFaultsToErrorQueueNoTransactionBehavior.Registration(errorQueue, context.Settings.LocalAddress()));
            }
            else
            {
                var failureInfoStorage = new FailureInfoStorage();
                context.Container.RegisterSingleton(failureInfoStorage);

                context.Pipeline.Register(new MoveFaultsToErrorQueueBehavior.Registration(errorQueue, context.Settings.LocalAddress()));
            }

            context.Pipeline.Register("FaultToDispatchConnector", new FaultToDispatchConnector(), "Connector to dispatch faulted messages");

            RaiseLegacyNotifications(context);
        }

        //note: will soon be removed since we're deprecating Notifications in favor of the new notifications
        static void RaiseLegacyNotifications(FeatureConfigurationContext context)
        {
            var legacyNotifications = context.Settings.Get<Notifications>();
            var notifications = context.Settings.Get<NotificationSubscriptions>();

            notifications.Subscribe<MessageToBeRetried>(e =>
            {
                if (e.IsImmediateRetry)
                {
                    legacyNotifications.Errors.InvokeMessageHasFailedAFirstLevelRetryAttempt(e.Attempt, e.Message, e.Exception);
                }
                else
                {
                    legacyNotifications.Errors.InvokeMessageHasBeenSentToSecondLevelRetries(e.Attempt, e.Message, e.Exception);
                }

                return TaskEx.CompletedTask;
            });

            notifications.Subscribe<MessageFaulted>(e =>
            {
                legacyNotifications.Errors.InvokeMessageHasBeenSentToErrorQueue(e.Message, e.Exception);
                return TaskEx.CompletedTask;
            });
        }
    }
}