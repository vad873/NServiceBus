namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.Timeout.Core;

    class CyclingOutageTimeoutPersister : IPersistTimeouts, IQueryTimeouts
    {
        public CyclingOutageTimeoutPersister(TimeSpan timeToWaitBeforeFakeOutage)
        {
            timeToWait = timeToWaitBeforeFakeOutage;
            NextChangeTime = DateTime.Now.Add(timeToWait);
        }

        public Task<bool> TryRemove(string timeoutId, ContextBag context)
        {
            ThrowExceptionUntilWaitTimeReached();

            TimeoutData timeoutData = null;

            if (storage.ContainsKey(timeoutId))
            {
                storage.TryRemove(timeoutId, out timeoutData);
            }

            return Task.FromResult(timeoutData != null);
        }

        public Task RemoveTimeoutBy(Guid sagaId, ContextBag context)
        {
            ThrowExceptionUntilWaitTimeReached();
            return completedTask;
        }

        public Task Add(TimeoutData timeout, ContextBag context)
        {
            ThrowExceptionUntilWaitTimeReached();
            storage.TryAdd(timeout.Id, timeout);
            return completedTask;
        }

        public Task<TimeoutData> Peek(string timeoutId, ContextBag context)
        {
            ThrowExceptionUntilWaitTimeReached();
            if (storage.ContainsKey(timeoutId))
            {
                return Task.FromResult(storage[timeoutId]);
            }
            return Task.FromResult<TimeoutData>(null);
        }

        public Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
        {
            ThrowExceptionUntilWaitTimeReached();

            var timeoutsDue = new List<TimeoutsChunk.Timeout>();
            foreach (var key in storage.Keys)
            {
                var value = storage[key];
                if (value.Time <= startSlice)
                {
                    var timeout = new TimeoutsChunk.Timeout(key, value.Time);
                    timeoutsDue.Add(timeout);
                }
            }

            var chunk = new TimeoutsChunk(timeoutsDue, DateTime.Now.AddSeconds(5));

            return Task.FromResult(chunk);
        }

        void ThrowExceptionUntilWaitTimeReached()
        {
            if (NextChangeTime > DateTime.Now)
            {
                throw new Exception("Persister is temporarily unavailable");
            }

            NextChangeTime = DateTime.Now.Add(timeToWait);
        }

        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            ThrowExceptionUntilWaitTimeReached();
            nextTimeToRunQuery = DateTime.Now.AddSeconds(2);
            return Enumerable.Empty<Tuple<string, DateTime>>();
        }

        public Task Add(TimeoutData timeout)
        {
            ThrowExceptionUntilWaitTimeReached();
            return completedTask;
        }

        Task completedTask = Task.FromResult(0);
        DateTime NextChangeTime;
        TimeSpan timeToWait;
        ConcurrentDictionary<string, TimeoutData> storage = new ConcurrentDictionary<string, TimeoutData>();
    }
}