namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using AcceptanceTesting.Customization;
    using AcceptanceTesting.Support;
    using Configuration.AdvancedExtensibility;
    using Features;

    public class DefaultServer : IEndpointSetupTemplate
    {
        public DefaultServer()
        {
            typesToInclude = new List<Type>();
        }

        public DefaultServer(List<Type> typesToInclude)
        {
            this.typesToInclude = typesToInclude;
        }

        public Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, Action<EndpointConfiguration> configurationBuilderCustomization)
        {
            var types = endpointConfiguration.GetTypesScopedByTestClass();

            typesToInclude.AddRange(types);

            var configuration = new EndpointConfiguration(endpointConfiguration.EndpointName);

            configuration.TypesToIncludeInScan(typesToInclude);
            configuration.EnableInstallers();

            configuration.DisableFeature<TimeoutManager>();

            configuration.UsePersistence<AcceptanceTestingPersistence>();
            var storageDir = Path.Combine(NServiceBusAcceptanceTest.StorageRootDir, NUnit.Framework.TestContext.CurrentContext.Test.ID);
            configuration.UseTransport<LearningTransport>()
                .StorageDirectory(storageDir);

            var recoverability = configuration.Recoverability();
            recoverability.Delayed(delayed => delayed.NumberOfRetries(0));
            recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
            configuration.SendFailedMessagesTo("error");

            configuration.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

            configuration.GetSettings().SetDefault("ScaleOut.UseSingleBrokerQueue", true);
            configurationBuilderCustomization(configuration);

            return Task.FromResult(configuration);
        }

        List<Type> typesToInclude;
    }
}