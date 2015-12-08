namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;

    class RemoveSagaHeadersFromEventsBehavior : Behavior<IncomingPhysicalMessageContext>
    {
        public override Task Invoke(IncomingPhysicalMessageContext context, Func<Task> next)
        {
            // We need this for backwards compatibility because in v4.0.0 we still have this headers being sent as part of the message even if MessageIntent == MessageIntentEnum.Publish
            string messageIntentString;
            if (context.Message.Headers.TryGetValue(Headers.MessageIntent, out messageIntentString))
            {
                MessageIntentEnum messageIntent;

                if (Enum.TryParse(messageIntentString, true, out messageIntent) && messageIntent == MessageIntentEnum.Publish)
                {
                    context.Message.Headers.Remove(Headers.SagaId);
                    context.Message.Headers.Remove(Headers.SagaType);
                }
            }

            return next();
        }
    }
}