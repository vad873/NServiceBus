namespace NServiceBus.Testing
{
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Behaviors;
    using NServiceBus.Unicast.Messages;

    /// <summary>
    /// A testable implementation of <see cref="InvokeHandlerContext"/>.
    /// </summary>
    public class TestableInvokeHandlerContext : TestableMessageHandlerContext, InvokeHandlerContext
    {
        /// <summary>
        /// The current <see cref="IBuilder"/>.
        /// </summary>
        IBuilder BehaviorContext.Builder => Builder;

        /// <summary>
        /// The <see cref="FakeBuilder"/> providing a testable <see cref="IBuilder"/> implementation.
        /// </summary>
        public FakeBuilder Builder { get; set; } = new FakeBuilder();

        /// <summary>
        /// The current <see cref="IHandleMessages{T}" /> being executed.
        /// </summary>
        public MessageHandler MessageHandler { get; set; } = new MessageHandler((o, o1, arg3) => TaskEx.Completed, typeof(object));

        /// <summary>
        /// The message instance being handled.
        /// </summary>
        public object MessageBeingHandled { get; set; } = new object();

        /// <summary>
        /// Indicates whether <see cref="IMessageHandlerContext.HandleCurrentMessageLater"/> has been called.
        /// </summary>
        public bool HandleCurrentMessageLaterWasCalled => HandleCurrentMessageLaterCalled;

        /// <summary>
        /// <code>true</code> if <see cref="IMessageHandlerContext.DoNotContinueDispatchingCurrentMessageToHandlers" /> or <see cref="IMessageHandlerContext.HandleCurrentMessageLater"/> has been called.
        /// </summary>
        public bool HandlerInvocationAborted => HandleCurrentMessageLaterCalled || DoNotContinueDispatchingCurrentMessageToHandlersCalled;

        /// <summary>
        /// Metadata for the incoming message.
        /// </summary>
        public MessageMetadata MessageMetadata { get; set; } = new MessageMetadata(typeof(object));
    }
}