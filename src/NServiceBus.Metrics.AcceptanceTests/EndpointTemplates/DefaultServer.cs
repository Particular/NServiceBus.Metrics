namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;

    public class DefaultServer : IEndpointSetupTemplate
    {
        public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Func<EndpointConfiguration, Task> configurationBuilderCustomization)
        {
            var configuration = new EndpointConfiguration(endpointConfiguration.EndpointName);

            configuration.EnableInstallers();

            configuration.UsePersistence<AcceptanceTestingPersistence>();
            var storageDir = Path.Combine(NServiceBusAcceptanceTest.StorageRootDir, NUnit.Framework.TestContext.CurrentContext.Test.ID);
            configuration.UseTransport(new LearningTransport { StorageDirectory = storageDir });
            configuration.UseSerialization<SystemJsonSerializer>();

            var recoverability = configuration.Recoverability();
            recoverability.Delayed(delayed => delayed.NumberOfRetries(0));
            recoverability.Immediate(immediate => immediate.NumberOfRetries(0));

            configuration.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

            await configurationBuilderCustomization(configuration);

            // scan types at the end so that all types used by the configuration have been loaded into the AppDomain
            configuration.ScanTypesForTest(endpointConfiguration);

            return configuration;
        }
    }
}