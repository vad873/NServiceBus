using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public class ConfigureDevelopmentTransport : IConfigureTestExecution
{
    public Task Configure(BusConfiguration configuration, IDictionary<string, string> settings)
    {
        configuration.UseTransport<DevelopmentTransport>();
        return Task.FromResult(0);
    }

    public Task Cleanup()
    {

        return Task.FromResult(0);
    }
}
