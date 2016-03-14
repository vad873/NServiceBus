namespace NServiceBus
{
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// </summary>
    public interface IRuntimeContainer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IEndpointInstance> Register(EndpointConfiguration endpointConfiguration);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task<IStartedContainer> Start();

    }
}