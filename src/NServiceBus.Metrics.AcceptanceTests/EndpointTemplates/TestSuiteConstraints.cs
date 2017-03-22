namespace NServiceBus.AcceptanceTests
{
    using AcceptanceTesting.Support;

    public interface ITestSuiteConstraints
    {
        bool SupportsDtc { get; }

        bool SupportsCrossQueueTransactions { get; }

        bool SupportsNativePubSub { get; }

        bool SupportsNativeDeferral { get; }

        bool SupportsOutbox { get; }

        IConfigureEndpointTestExecution TransportConfiguration { get; }

        IConfigureEndpointTestExecution PersistenceConfiguration { get; }
    }

    // ReSharper disable once PartialTypeWithSinglePart
    public class TestSuiteConstraints : ITestSuiteConstraints
    {
        public static TestSuiteConstraints Current = new TestSuiteConstraints();
        public bool SupportsDtc => true;
        public bool SupportsCrossQueueTransactions => true;
        public bool SupportsNativePubSub => false;
        public bool SupportsNativeDeferral => false;
        public bool SupportsOutbox => true;
        public IConfigureEndpointTestExecution TransportConfiguration => new ConfigureEndpointMsmqTransport();
        public IConfigureEndpointTestExecution PersistenceConfiguration => new ConfigureEndpointInMemoryPersistence();
    }
}