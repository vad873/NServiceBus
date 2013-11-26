namespace NServiceBus.Pipeline.Contexts
{
    class ReceivePhysicalMessageContext : BehaviorContext
    {
        public TransportMessage PhysicalMessage { get; private set; }

        public ReceivePhysicalMessageContext(RootContext parentContext, TransportMessage physicalMessage)
            : base(parentContext)
        {
            PhysicalMessage = physicalMessage;
            handleCurrentMessageLaterWasCalled = false;

            Set(IncomingPhysicalMessageKey, physicalMessage);
        }

        public static string IncomingPhysicalMessageKey
        {
            get { return "NServiceBus.IncomingPhysicalMessage"; }
        }
    }
}