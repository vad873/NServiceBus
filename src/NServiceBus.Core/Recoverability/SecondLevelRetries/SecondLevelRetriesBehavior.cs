namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using DelayedDelivery;
    using DeliveryConstraints;
    using Logging;
    using Pipeline;
    using Transports;

    class SecondLevelRetriesBehavior : ForkConnector<ITransportReceiveContext, IRoutingContext>
    {
        public SecondLevelRetriesBehavior(SecondLevelRetryPolicy retryPolicy, string localAddress, TransportTransactionMode transportTransactionMode, FailureInfoStorage failureInfoStorage)
        {
            this.retryPolicy = retryPolicy;
            this.localAddress = localAddress;
            this.transportTransactionMode = transportTransactionMode;
            this.failureInfoStorage = failureInfoStorage;
        }

        bool CanAbortReceiveOperation => transportTransactionMode != TransportTransactionMode.None;

        public override async Task Invoke(ITransportReceiveContext context, Func<Task> next, Func<IRoutingContext, Task> fork)
        {
            var message = context.Message;

            var failureInfo = failureInfoStorage.GetFailureInfoForMessage(context.Message.MessageId);

            if (MessageShouldBeDeferredForSecondLevelRetry(failureInfo))
            {
                await DeferMessageForSecondLevelRetry(context, fork, message, failureInfo.Exception).ConfigureAwait(false);

                return;
            }

            try
            {
                await next().ConfigureAwait(false);
            }
            catch (MessageDeserializationException)
            {
                context.Message.Headers.Remove(Headers.Retries);
                throw; // no SLR for poison messages
            }
            catch (Exception ex)
            {
                if (CanAbortReceiveOperation)
                {
                    failureInfoStorage.RecordFailureInfoForMessage(context.Message.MessageId, ex, shouldDeferForRetry: true);

                    context.AbortReceiveOperation();
                }
                else
                {
                    await DeferMessageForSecondLevelRetry(context, fork, message, ex).ConfigureAwait(false);
                }
            }
        }

        bool MessageShouldBeDeferredForSecondLevelRetry(ProcessingFailureInfo failureInfo)
        {
            return CanAbortReceiveOperation && failureInfo.ShouldDeferForRetry;
        }

        async Task DeferMessageForSecondLevelRetry(ITransportReceiveContext context, Func<IRoutingContext, Task> fork, IncomingMessage message, Exception exception)
        {
            var currentRetry = GetNumberOfRetries(message.Headers) + 1;

            TimeSpan delay;

            if (retryPolicy.TryGetDelay(message, exception, currentRetry, out delay))
            {
                message.RevertToOriginalBodyIfNeeded();
                var messageToRetry = new OutgoingMessage(message.MessageId, message.Headers, message.Body);

                messageToRetry.Headers[Headers.Retries] = currentRetry.ToString();
                messageToRetry.Headers[Headers.RetriesTimestamp] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);

                var dispatchContext = this.CreateRoutingContext(messageToRetry, localAddress, context);

                context.Extensions.Set(new List<DeliveryConstraint>
                {
                    new DelayDeliveryWith(delay)
                });

                Logger.Warn($"Second Level Retry will reschedule message '{message.MessageId}' after a delay of {delay} because of an exception:", exception);

                await fork(dispatchContext).ConfigureAwait(false);

                failureInfoStorage.ClearFailureInfoForMessage(message.MessageId);

                await context.RaiseNotification(new MessageToBeRetried(currentRetry, delay, context.Message, exception)).ConfigureAwait(false);

                return;
            }

            message.Headers.Remove(Headers.Retries);
            Logger.WarnFormat("Giving up Second Level Retries for message '{0}'.", message.MessageId);

            // TODO: This may require ExceptionDispatchInfo.Capture result to be stored inside ProcessingInfoFailure.
            throw exception;
        }

        static int GetNumberOfRetries(Dictionary<string, string> headers)
        {
            string value;
            if (headers.TryGetValue(Headers.Retries, out value))
            {
                int i;
                if (int.TryParse(value, out i))
                {
                    return i;
                }
            }
            return 0;
        }

        FailureInfoStorage failureInfoStorage;
        string localAddress;
        SecondLevelRetryPolicy retryPolicy;

        TransportTransactionMode transportTransactionMode;

        static ILog Logger = LogManager.GetLogger<SecondLevelRetriesBehavior>();

        public class Registration : RegisterStep
        {
            public Registration()
                : base("SecondLevelRetries", typeof(SecondLevelRetriesBehavior), "Performs second level retries")
            {
                InsertBeforeIfExists("FirstLevelRetries");
            }
        }
    }
}