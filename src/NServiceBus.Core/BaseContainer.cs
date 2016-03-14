namespace NServiceBus
{
    using System.Threading.Tasks;

    /// <summary>
    /// 
    /// </summary>
    public abstract class BaseContainer : IRuntimeContainer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="containerId"></param>
        protected BaseContainer(string containerId)
        {
            this.containerId = containerId;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<IEndpointInstance> Register(EndpointConfiguration endpointConfiguration)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task<IStartedContainer> Start()
        {
            throw new System.NotImplementedException();
        }

        string containerId;

    }
}