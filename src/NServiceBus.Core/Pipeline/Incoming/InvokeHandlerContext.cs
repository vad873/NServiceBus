namespace NServiceBus.Pipeline.Contexts
{
    using NServiceBus.Unicast.Behaviors;
    using NServiceBus.Unicast.Messages;

    /// <summary>
    /// A context of handling a logical message by a handler.
    /// </summary>
    public interface InvokeHandlerContext : IncomingContext, IMessageHandlerContext
    {
        /// <summary>
        /// The current <see cref="IHandleMessages{T}" /> being executed.
        /// </summary>
        MessageHandler MessageHandler { get; }

        /// <summary>
        /// The message instance being handled.
        /// </summary>
        object MessageBeingHandled { get; }

        /// <summary>
        /// Indicates whether <see cref="IMessageHandlerContext.HandleCurrentMessageLater"/> has been called.
        /// </summary>
        bool HandleCurrentMessageLaterWasCalled { get; }

        /// <summary>
        /// <code>true</code> if <see cref="IMessageHandlerContext.DoNotContinueDispatchingCurrentMessageToHandlers" /> or <see cref="IMessageHandlerContext.HandleCurrentMessageLater"/> has been called.
        /// </summary>
        bool HandlerInvocationAborted { get; }

        /// <summary>
        /// Metadata for the incoming message.
        /// </summary>
        MessageMetadata MessageMetadata { get; }
    }
}