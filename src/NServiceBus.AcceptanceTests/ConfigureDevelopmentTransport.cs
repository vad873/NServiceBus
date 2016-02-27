using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;

public class ConfigureDevelopmentTransport : IConfigureTestExecution
{
    public IEnumerable<Type> UnsupportedScenarioDescriptorTypes { get; }

    public Task Configure(EndpointConfiguration configuration, IDictionary<string, string> settings)
    {
        configuration.UseTransport<DevelopmentTransport>();
        return Task.FromResult(0);
    }

    public Task Cleanup()
    {

        return Task.FromResult(0);
    }
}
