namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Message metadata class.
    /// </summary>
    public partial class MessageMetadata
    {
        /// <summary>
        /// Creates a new <see cref="MessageMetadata"/> instance for the given message type.
        /// </summary>
        /// <param name="messageType">The type of the related message.</param>
        /// <param name="messageHierarchy">A list representing the message type's parent types. The list must not contain <paramref name="messageType"/>.</param>
        public MessageMetadata(Type messageType, IEnumerable<Type> messageHierarchy = null)
        {
            MessageType = messageType;
            MessageHierarchy = (new[] { messageType}).Concat(messageHierarchy ?? new Type[0]);
        }

        /// <summary>
        /// The <see cref="Type"/> of the message instance.
        /// </summary>
        public Type MessageType { get; private set; }

     
        /// <summary>
        /// The message instance hierarchy. Lists all parent types of the current type in their hierarchical order.
        /// </summary>
        public IEnumerable<Type> MessageHierarchy { get; private set; }
    }
}