namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;
    using Transports;

    class MoveFaultsToErrorQueueNoTransactionBehavior : ForkConnector<ITransportReceiveContext, IFaultContext>
    {
        public MoveFaultsToErrorQueueNoTransactionBehavior(CriticalError criticalError, string errorQueueAddress, string localAddress)
        {
            this.criticalError = criticalError;
            this.errorQueueAddress = errorQueueAddress;
            this.localAddress = localAddress;
        }

        public override async Task Invoke(ITransportReceiveContext context, Func<Task> next, Func<IFaultContext, Task> fork)
        {
            try
            {
                await next().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                try
                {
                    var message = context.Message;

                    Logger.Error($"Moving message '{message.MessageId}' to the error queue because processing failed due to an exception:", exception);

                    message.RevertToOriginalBodyIfNeeded();

                    message.SetExceptionHeaders(exception, localAddress);

                    message.Headers.Remove(Headers.Retries);

                    var outgoingMessage = new OutgoingMessage(message.MessageId, message.Headers, message.Body);
                    var faultContext = this.CreateFaultContext(context, outgoingMessage, errorQueueAddress, exception);

                    await fork(faultContext).ConfigureAwait(false);

                    await context.RaiseNotification(new MessageFaulted(message, exception)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    criticalError.Raise("Failed to forward message to error queue", ex);
                    throw;
                }
            }
        }

        CriticalError criticalError;
        string errorQueueAddress;
        string localAddress;
        static ILog Logger = LogManager.GetLogger<MoveFaultsToErrorQueueNoTransactionBehavior>();

        public class Registration : RegisterStep
        {
            public Registration(string errorQueueAddress, string localAddress)
                : base("MoveFaultsToErrorQueue", typeof(MoveFaultsToErrorQueueNoTransactionBehavior), "Moved failing messages to the configured error queue", b => new MoveFaultsToErrorQueueNoTransactionBehavior(
                    b.Build<CriticalError>(),
                    errorQueueAddress,
                    localAddress))
            {
                InsertBeforeIfExists("FirstLevelRetries");
                InsertBeforeIfExists("SecondLevelRetries");
            }
        }
    }
}