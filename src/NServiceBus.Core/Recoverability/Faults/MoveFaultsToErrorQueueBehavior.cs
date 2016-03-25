namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;
    using Transports;

    class MoveFaultsToErrorQueueBehavior : ForkConnector<ITransportReceiveContext, IFaultContext>
    {
        // TODO: Remove duplication in MoveFaultsToErrorQueueBehavior and MoveFaultsToErrorQueueNoTransactionBehavior.
        public MoveFaultsToErrorQueueBehavior(CriticalError criticalError, string errorQueueAddress, string localAddress, FailureInfoStorage failureInfoStorage)
        {
            this.criticalError = criticalError;
            this.errorQueueAddress = errorQueueAddress;
            this.localAddress = localAddress;
            this.failureInfoStorage = failureInfoStorage;
        }

        public override async Task Invoke(ITransportReceiveContext context, Func<Task> next, Func<IFaultContext, Task> fork)
        {
            var failureInfo = failureInfoStorage.GetFailureInfoForMessage(context.Message.MessageId);

            if (failureInfo.ShouldMoveToErrorQueue)
            {
                try
                {
                    var message = context.Message;

                    Logger.Error($"Moving message '{message.MessageId}' to the error queue because processing failed due to an exception:", failureInfo.Exception);

                    message.RevertToOriginalBodyIfNeeded();

                    message.SetExceptionHeaders(failureInfo.Exception, localAddress);

                    message.Headers.Remove(Headers.Retries);

                    var outgoingMessage = new OutgoingMessage(message.MessageId, message.Headers, message.Body);
                    var faultContext = this.CreateFaultContext(context, outgoingMessage, errorQueueAddress, failureInfo.Exception);

                    await fork(faultContext).ConfigureAwait(false);

                    failureInfoStorage.ClearFailureInfoForMessage(message.MessageId);

                    await context.RaiseNotification(new MessageFaulted(message, failureInfo.Exception)).ConfigureAwait(false);

                    return;
                }
                catch (Exception ex)
                {
                    criticalError.Raise("Failed to forward message to error queue", ex);
                    throw;
                }
            }

            try
            {
                await next().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                failureInfoStorage.RecordFailureInfoForMessage(context.Message.MessageId, ex, shouldMoveToErrorQueue: true);

                context.AbortReceiveOperation();
            }
        }

        CriticalError criticalError;
        string errorQueueAddress;
        string localAddress;
        FailureInfoStorage failureInfoStorage;
        static ILog Logger = LogManager.GetLogger<MoveFaultsToErrorQueueBehavior>();

        public class Registration : RegisterStep
        {
            public Registration(string errorQueueAddress, string localAddress)
                : base("MoveFaultsToErrorQueue", typeof(MoveFaultsToErrorQueueBehavior), "Moved failing messages to the configured error queue", b => new MoveFaultsToErrorQueueBehavior(
                    b.Build<CriticalError>(),
                    errorQueueAddress,
                    localAddress,
                    b.Build<FailureInfoStorage>()))
            {
                InsertBeforeIfExists("FirstLevelRetries");
                InsertBeforeIfExists("SecondLevelRetries");
            }
        }
    }
}