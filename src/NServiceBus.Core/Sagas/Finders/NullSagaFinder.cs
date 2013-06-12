namespace NServiceBus.Sagas.Finders
{
    using System.Collections.Generic;
    using NServiceBus.Saga;

    /// <summary>
    /// Catch-all finder to return null - so that we can later check
    /// for whether a new saga should be created.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NullSagaFinder<T> : IFindSagas<T>.Using<object> where T : IContainSagaData
    {
        /// <summary>
        /// Returns null.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public IEnumerable<T> FindBy(object message)
        {
            yield return default(T);
        }
    }
}
