namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;
    using Unicast;
    using Unicast.Messages;

    class SendLogicalMessagesContext : BehaviorContext
    {
        public SendOptions SendOptions { get; private set; }
        public IEnumerable<LogicalMessage> LogicalMessages { get; private set; }

        public SendLogicalMessagesContext(BehaviorContext parentContext, SendOptions sendOptions, IEnumerable<LogicalMessage> messages)
            : base(parentContext)
        {
            SendOptions = sendOptions;
            LogicalMessages = messages;
        }
    }
}