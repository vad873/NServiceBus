namespace NServiceBus
{
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// </summary>
    public interface IStartedContainer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task Stop();
    }
}