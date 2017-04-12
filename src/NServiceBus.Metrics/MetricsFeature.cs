using System.Threading.Tasks;
using Metrics;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics;
using NServiceBus.ObjectBuilder;

class MetricsFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        context.ThrowIfSendonly();

        var settings = context.Settings;
        var metricsOptions = settings.Get<MetricsOptions>();

        // the context is used as originating endpoint in the headers
        MetricsContext metricsContext = new DefaultMetricsContext($"{settings.EndpointName()}");

        ConfigureMetrics(context, metricsContext);

        context.RegisterStartupTask(builder => new MetricsReporting(metricsContext, metricsOptions, builder));
    }

    static void ConfigureMetrics(FeatureConfigurationContext context, MetricsContext metricsContext)
    {
        var builders = AllMetrics.Create();

        foreach (var builder in builders)
        {
            builder.Define(metricsContext);
            builder.WireUp(context);
        }
    }

    class MetricsReporting : FeatureStartupTask
    {
        public MetricsReporting(MetricsContext metricsContext, MetricsOptions metricsOptions, IBuilder builder)
        {
            this.metricsOptions = metricsOptions;
            this.builder = builder;
            metricsConfig = new MetricsConfig(metricsContext);
        }

        protected override Task OnStart(IMessageSession session)
        {
            metricsOptions.SetUpReports(metricsConfig, builder);
            return Task.FromResult(0);
        }

        protected override Task OnStop(IMessageSession session)
        {
            metricsConfig.Dispose();
            return Task.FromResult(0);
        }

        MetricsOptions metricsOptions;
        IBuilder builder;
        MetricsConfig metricsConfig;
    }
}