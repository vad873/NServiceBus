namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;

    class SendValidatorBehavior : IBehavior<SendLogicalMessageContext>
    {
        public void Invoke(SendLogicalMessageContext context, Action next)
        {
            if (context.ParentContext.SendOptions.Destination == Address.Undefined)
            {
                throw new InvalidOperationException("No destination specified for message: " + context.MessageToSend.MessageType);
            }

            switch (context.ParentContext.SendOptions.Intent)
            {
                case MessageIntentEnum.Init:
                case MessageIntentEnum.Subscribe:
                case MessageIntentEnum.Unsubscribe:
                    break;
                case MessageIntentEnum.Publish:
                    MessagingBestPractices.AssertIsValidForPubSub(context.MessageToSend.MessageType);
                    break;
                case MessageIntentEnum.Reply:
                    MessagingBestPractices.AssertIsValidForReply(context.MessageToSend.MessageType);
                    break;
                case MessageIntentEnum.Send:
                    MessagingBestPractices.AssertIsValidForSend(context.MessageToSend.MessageType, context.ParentContext.SendOptions.Intent);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            next();
        }
    }
}