namespace NServiceBus
{
    using System;

    class ProcessingFailureInfo
    {
        public ProcessingFailureInfo(int numberOfFailedAttempts, Exception exception, bool shouldMoveToErrorQueue = false, bool shouldDeferForRetry = false)
        {
            NumberOfFailedAttempts = numberOfFailedAttempts;
            Exception = exception;
            ShouldMoveToErrorQueue = shouldMoveToErrorQueue;
            ShouldDeferForRetry = shouldDeferForRetry;
        }

        public int NumberOfFailedAttempts { get; }
        public Exception Exception { get; }

        public bool ShouldMoveToErrorQueue { get; }
        public bool ShouldDeferForRetry { get; }

        public static readonly ProcessingFailureInfo NullFailureInfo = new ProcessingFailureInfo(0, null);
    }
}