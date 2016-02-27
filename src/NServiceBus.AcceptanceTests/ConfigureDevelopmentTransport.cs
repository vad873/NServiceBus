using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.AcceptanceTests.ScenarioDescriptors;

public class ConfigureDevelopmentTransport : IConfigureTestExecution
{
    public IEnumerable<Type> UnsupportedScenarioDescriptorTypes { get; } = new[]
   {
        typeof(AllDtcTransports),
        typeof(AllTransportsWithMessageDrivenPubSub)
    };

    public Task Configure(EndpointConfiguration configuration, IDictionary<string, string> settings)
    {
        //todo: use a path local to the test dir
        configuration.UseTransport<DevelopmentTransport>();
        return Task.FromResult(0);
    }

    public Task Cleanup()
    {
        //todo: cleanup the test dir
        return Task.FromResult(0);
    }
}
