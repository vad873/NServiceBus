namespace NServiceBus
{
    using System;
    using NServiceBus.Encryption;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Transport;

    class DecryptBehavior : IBehavior<IncomingContext>
    {
        EncryptionMutator messageMutator;

        public DecryptBehavior(EncryptionMutator messageMutator)
        {
            this.messageMutator = messageMutator;
        }
        public void Invoke(IncomingContext context, Action next)
        {
            if (context.IncomingLogicalMessage.IsControlMessage())
            {
                next();
                return;
            }

            string keyIdentifier;

            context.IncomingLogicalMessage.Headers.TryGetValue(Headers.EncryptionKeyIdentifier, out keyIdentifier);

            var current = context.IncomingLogicalMessage.Instance;
            messageMutator.DecryptionKeyIdentifier = keyIdentifier;
            current = messageMutator.MutateIncoming(current);
            context.IncomingLogicalMessage.UpdateMessageInstance(current);
            next();
        }

        public class DecryptRegistration : RegisterStep
        {
            public DecryptRegistration()
                : base("InvokeDecryption", typeof(DecryptBehavior), "Invokes the decryption logic")
            {
                InsertAfter(WellKnownStep.ExecuteLogicalMessages);
                InsertBefore(WellKnownStep.MutateIncomingMessages);
            }

        }
    }
}