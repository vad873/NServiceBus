namespace NServiceBus
{
    using System;

    class ProcessingFailureInfo
    {
        public ProcessingFailureInfo(int numberOfFailedAttempts, Exception exception, bool shouldMoveToErrorQueue = false, bool shouldDeferForSecondLevelRetry = false)
        {
            NumberOfFailedAttempts = numberOfFailedAttempts;
            Exception = exception;
            ShouldMoveToErrorQueue = shouldMoveToErrorQueue;
            ShouldDeferForSecondLevelRetry = shouldDeferForSecondLevelRetry;
        }

        public int NumberOfFailedAttempts { get; }
        public Exception Exception { get; }

        public bool ShouldMoveToErrorQueue { get; }
        public bool ShouldDeferForSecondLevelRetry { get; }

        public static readonly ProcessingFailureInfo NullFailureInfo = new ProcessingFailureInfo(0, null);
    }
}