using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Metrics;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.ObjectBuilder;

class MetricsFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        context.ThrowIfSendonly();

        var settings = context.Settings;
        var metricsOptions = settings.Get<MetricsOptions>();

        var hostId = settings.Get<Guid>("NServiceBus.HostInformation.HostId");
        MetricsContext metricsContext = new DefaultMetricsContext($"{settings.EndpointName()}@{hostId}");

        ConfigureMetrics(context, metricsContext);

        context.RegisterStartupTask(builder => new MetricsReporting(metricsContext, metricsOptions, builder));
    }

    static void ConfigureMetrics(FeatureConfigurationContext context, MetricsContext metricsContext)
    {
        var messagesUnit = Unit.Custom("Messages");

        ActivateAndInvoke<IMetricBuilder>(
            context.Settings.GetAvailableTypes(),
            builder => builder.WireUp(context, metricsContext, messagesUnit)
        );
    }

    static void ForAllTypes<T>(IEnumerable<Type> types, Action<Type> action) where T : class
    {
        // ReSharper disable HeapView.SlowDelegateCreation
        foreach (var type in types.Where(t => typeof(T).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface)))
        {
            action(type);
        }
        // ReSharper restore HeapView.SlowDelegateCreation
    }

    static void ActivateAndInvoke<T>(IList<Type> types, Action<T> action) where T : class
    {
        ForAllTypes<T>(types, t =>
        {
            if (!HasDefaultConstructor(t))
            {
                throw new Exception($"Unable to create the type '{t.Name}'. Types implementing '{typeof(T).Name}' must have a public parameterless (default) constructor.");
            }

            var instanceToInvoke = (T)Activator.CreateInstance(t);
            action(instanceToInvoke);
        });
    }

    static bool HasDefaultConstructor(Type type) => type.GetConstructor(Type.EmptyTypes) != null;

    class MetricsReporting : FeatureStartupTask
    {
        MetricsOptions metricsOptions;
        IBuilder builder;
        MetricsConfig metricsConfig;

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
    }
}
