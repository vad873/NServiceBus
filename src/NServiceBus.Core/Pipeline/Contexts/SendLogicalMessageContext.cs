namespace NServiceBus.Pipeline.Contexts
{
    using Unicast.Messages;

    class SendLogicalMessageContext : BehaviorContext
    {
        public SendLogicalMessagesContext ParentContext { get; private set; }
        public LogicalMessage MessageToSend { get; private set; }

        public SendLogicalMessageContext(SendLogicalMessagesContext parentContext, LogicalMessage messageToSend)
            : base(parentContext)
        {
            ParentContext = parentContext;
            MessageToSend = messageToSend;
        }

    }
}