namespace NServiceBus.Transports
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the infrastructure of the transport used for receiving.
    /// </summary>
    public class TransportReceiveInfrastructure
    {
        /// <summary>
        /// Creates new result.
        /// </summary>
        public TransportReceiveInfrastructure(
            Func<IPushMessages> messagePumpFactory,
            Func<ICreateQueues> queueCreatorFactory,
            Func<Task<StartupCheckResult>> preStartupCheck)
        {
            Guard.AgainstNull(nameof(messagePumpFactory), messagePumpFactory);
            Guard.AgainstNull(nameof(queueCreatorFactory), queueCreatorFactory);
            Guard.AgainstNull(nameof(preStartupCheck), preStartupCheck);

            MessagePumpFactory = messagePumpFactory;
            QueueCreatorFactory = queueCreatorFactory;
            PreStartupCheck = preStartupCheck;
        }

        internal Func<IPushMessages> MessagePumpFactory { get; }
        internal Func<ICreateQueues> QueueCreatorFactory { get; }
        internal Func<Task<StartupCheckResult>> PreStartupCheck { get; }
    }
}