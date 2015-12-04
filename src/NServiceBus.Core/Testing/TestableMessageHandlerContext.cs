namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
    using NServiceBus.Persistence;

    /// <summary>
    /// A testable implementation of <see cref="IMessageHandlerContext"/>.
    /// </summary>
    public class TestableMessageHandlerContext : IMessageHandlerContext
    {
        /// <summary>
        /// A <see cref="ContextBag"/> which can be used to extend the current object.
        /// </summary>
        public ContextBag Extensions { get; } = new ContextBag();

        /// <summary>
        /// The Id of the currently processed message.
        /// </summary>
        public string MessageId { get; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The address of the endpoint that sent the current message being handled.
        /// </summary>
        public string ReplyToAddress { get; set; } = "ReplyToAddress";

        /// <summary>
        /// Gets the list of key/value pairs found in the header of the message.
        /// </summary>
        public IReadOnlyDictionary<string, string> MessageHeaders { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">The options for the send.</param>
        public Task Send(object message, SendOptions options)
        {
            sentMessages.Add(new SentMessage<SendOptions>(message, options));
            return TaskEx.Completed;
        }

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">The options for the send.</param>
        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            var message = mapper.CreateInstance(messageConstructor);
            sentMessages.Add(new SentMessage<SendOptions>(message, options));
            return TaskEx.Completed;
        }

        /// <summary>
        ///  Publish the message to subscribers.
        /// </summary>
        /// <param name="message">The message to publish.</param>
        /// <param name="options">The options for the publish.</param>
        public Task Publish(object message, PublishOptions options)
        {
            publishedMessages.Add(new SentMessage<PublishOptions>(message, options));
            return TaskEx.Completed;
        }

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="publishOptions">Specific options for this event.</param>
        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            var message = mapper.CreateInstance(messageConstructor);
            publishedMessages.Add(new SentMessage<PublishOptions>(message, publishOptions));
            return TaskEx.Completed;
        }

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="eventType">The type of event to subscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            subscribedMessages.Add(new SubscriptionMessage<SubscribeOptions>(eventType, options));
            return TaskEx.Completed;
        }

        /// <summary>
        /// Unsubscribes to receive published messages of the specified type.
        /// </summary>
        /// <param name="eventType">The type of event to unsubscribe to.</param>
        /// <param name="options">Options for the subscribe.</param>
        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            unsubscribedMessages.Add(new SubscriptionMessage<UnsubscribeOptions>(eventType, options));
            return TaskEx.Completed;
        }

        /// <summary>
        /// Sends the message to the endpoint which sent the message currently being handled.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="options">Options for this reply.</param>
        public Task Reply(object message, ReplyOptions options)
        {
            repliedMessages.Add(new SentMessage<ReplyOptions>(message, options));
            return TaskEx.Completed;
        }

        ///  <summary>
        /// Instantiates a message of type T and performs a regular <see cref="IMessageProcessingContext.Reply"/>.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <param name="options">Options for this reply.</param>
        public Task Reply<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            var message = mapper.CreateInstance(messageConstructor);
            repliedMessages.Add(new SentMessage<ReplyOptions>(message, options));
            return TaskEx.Completed;
        }

        /// <summary>
        /// Forwards the current message being handled to the destination maintaining
        /// all of its transport-level properties and headers.
        /// </summary>
        public Task ForwardCurrentMessageTo(string destination)
        {
            forwardedMessageTo.Add(destination);
            return TaskEx.Completed;
        }

        /// <summary>
        /// Moves the message being handled to the back of the list of available 
        /// messages so it can be handled later.
        /// </summary>
        public Task HandleCurrentMessageLater()
        {
            HandleCurrentMessageLaterCalled = true;
            return TaskEx.Completed;
        }

        /// <summary>
        /// Tells the bus to stop dispatching the current message to additional
        /// handlers.
        /// </summary>
        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            DoNotContinueDispatchingCurrentMessageToHandlersCalled = true;
        }

        /// <summary>
        /// Gets the synchronized storage session for processing the current message. NServiceBus makes sure the changes made 
        /// via this session will be persisted before the message receive is acknowledged.
        /// </summary>
        public SynchronizedStorageSession SynchronizedStorageSession { get; }

        MessageMapper mapper = new MessageMapper();
        List<SentMessage<SendOptions>> sentMessages = new List<SentMessage<SendOptions>>();
        List<SentMessage<PublishOptions>> publishedMessages = new List<SentMessage<PublishOptions>>();
        List<SentMessage<ReplyOptions>> repliedMessages = new List<SentMessage<ReplyOptions>>();
        List<SubscriptionMessage<SubscribeOptions>> subscribedMessages = new List<SubscriptionMessage<SubscribeOptions>>();
        List<SubscriptionMessage<UnsubscribeOptions>> unsubscribedMessages = new List<SubscriptionMessage<UnsubscribeOptions>>();
        List<string> forwardedMessageTo = new List<string>();

        /// <summary>
        /// Indicates whether <see cref="IMessageHandlerContext.HandleCurrentMessageLater"/> has been called.
        /// </summary>
        public bool HandleCurrentMessageLaterCalled { get; private set; }

        /// <summary>
        /// Indicates whether <see cref="IMessageHandlerContext.DoNotContinueDispatchingCurrentMessageToHandlers"/> has been called.
        /// </summary>
        public bool DoNotContinueDispatchingCurrentMessageToHandlersCalled { get; private set; }

        class SentMessage<T> where T : ExtendableOptions
        {
            public SentMessage(object message, T options)
            {
                Message = message;
                Options = options;
            }

            public object Message { get; }
            public T Options { get; }
        }

        class SubscriptionMessage<T> where T : ExtendableOptions
        {
            public SubscriptionMessage(Type eventType, T options)
            {
                EventType = eventType;
                Options = options;
            }

            public Type EventType { get; }
            public T Options { get; }
        }
    }
}