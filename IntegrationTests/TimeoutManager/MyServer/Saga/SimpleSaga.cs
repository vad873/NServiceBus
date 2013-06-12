namespace MyServer.Saga
{
    using System;
    using NServiceBus;
    using NServiceBus.Saga;

    public class SimpleSaga:Saga<SimpleSagaData>,
        IAmStartedByMessages<StartSagaMessage>,
        IHandleMessages<CustomerMadePreferred>,
    IHandleTimeouts<MyTimeOutState>
    {
        public void Handle(StartSagaMessage message)
        {
            Data.OrderId = message.OrderId;
            Data.CustomerId = message.CustomerId;

            Console.Out.WriteLine("Order placed for customer: {0}",Data.CustomerId);

        }

        public override void ConfigureHowToFindSaga()
        {
            ConfigureMapping<StartSagaMessage>(m => m.OrderId).ToSaga(s => s.OrderId);
            ConfigureMapping<CustomerMadePreferred>(s => s.CustomerId).ToSaga(s => s.CustomerId);
        }

        void LogMessage(string message)
        {
            Console.WriteLine(string.Format("{0} - {1} - SagaId:{2}", DateTime.Now.ToLongTimeString(),message,Data.Id));
        }

        public void Timeout(MyTimeOutState state)
        {
            LogMessage("Timeout fired, with state: " + state.SomeValue);

            LogMessage("Marking the saga as complete, be aware that this will remove the document from the storage (RavenDB)");
            MarkAsComplete();
        }

        public void Handle(CustomerMadePreferred message)
        {
            Console.Out.WriteLine("Order {0} discounted since customer {1} was made prefered", Data.OrderId, Data.CustomerId);
        }
    }

    public class CustomerMadePreferred:IMessage
    {
        public Guid CustomerId { get; set; }
    }
}