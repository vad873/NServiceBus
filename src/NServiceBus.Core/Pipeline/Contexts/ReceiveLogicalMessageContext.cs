namespace NServiceBus.Pipeline.Contexts
{
    using Unicast.Messages;

    class ReceiveLogicalMessageContext : BehaviorContext
    {
        public ReceivePhysicalMessageContext ParentContext { get; private set; }
        public LogicalMessage LogicalMessage { get; private set; }

        public ReceiveLogicalMessageContext(ReceivePhysicalMessageContext parentContext, LogicalMessage logicalMessage)
            : base(parentContext)
        {
            ParentContext = parentContext;
            LogicalMessage = logicalMessage;
        }
    }
}