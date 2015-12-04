namespace NServiceBus.Pipeline.Contexts
{
    using NServiceBus.Unicast.Messages;

    /// <summary>
    /// A context of behavior execution in logical message processing stage.
    /// </summary>
    public interface IncomingLogicalMessageContext : IncomingContext
    {
        /// <summary>
        /// Message being handled.
        /// </summary>
        LogicalMessage Message { get; }
    }
}