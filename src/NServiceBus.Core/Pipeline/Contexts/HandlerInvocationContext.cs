namespace NServiceBus.Pipeline.Contexts
{
    using Unicast.Behaviors;

    class HandlerInvocationContext : BehaviorContext
    {
        public ReceiveLogicalMessageContext ParentContext { get; private set; }
        public MessageHandler MessageHandler { get; private set; }

        public HandlerInvocationContext(ReceiveLogicalMessageContext parentContext, MessageHandler messageHandler)
            : base(parentContext)
        {
            ParentContext = parentContext;
            MessageHandler = messageHandler;
        }
    }
}