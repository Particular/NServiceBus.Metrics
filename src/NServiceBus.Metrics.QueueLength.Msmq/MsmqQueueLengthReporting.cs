using System;
using System.Threading;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Transport;

class MsmqQueueLengthReporting : Feature
{
    public MsmqQueueLengthReporting()
    {
        EnableByDefault();
        // TODO: Should we expose the feature so that we can depend on it directly? This is brittle
        DependsOn("MetricsFeature");
        Prerequisite(ctx => ctx.Settings.HasExplicitValue<MetricsSensorFactory>(), "Metrics must be enabled");
        Prerequisite(ctx => ctx.Settings.Get<TransportDefinition>() is MsmqTransport, "MSMQ Transport must be in use");
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var sensorFactory = context.Settings.Get<MetricsSensorFactory>();
        var queueBindings = context.Settings.Get<QueueBindings>();

        var msmqQueueLength = sensorFactory.CreateGaugeSensor("Queue Length", "Captures the length of incoming queues");

        var updater = new MsmqQueueLengthUpdater(msmqQueueLength, queueBindings);

        var delayBetweenChecks = TimeSpan.FromSeconds(1);
        var startupTask = new PeriodicallyReportMsmqQueueLengths(updater, delayBetweenChecks);

        context.RegisterStartupTask(startupTask);
    }

    class PeriodicallyReportMsmqQueueLengths : FeatureStartupTask
    {
        MsmqQueueLengthUpdater updater;
        TimeSpan delayBetweenChecks;
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        Task task;

        public PeriodicallyReportMsmqQueueLengths(MsmqQueueLengthUpdater updater, TimeSpan delayBetweenChecks)
        {
            this.updater = updater;
            this.delayBetweenChecks = delayBetweenChecks;
        }

        protected override Task OnStart(IMessageSession session)
        {
            task = Task.Run(async () =>
            {
                updater.Warmup();
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    await updater.UpdateQueueLengths().ConfigureAwait(false);
                    await Task.Delay(delayBetweenChecks).ConfigureAwait(false);
                }
            });
            return Task.FromResult(0);
        }

        protected override Task OnStop(IMessageSession session)
        {
            cancellationTokenSource.Cancel();

            return task;
        }
    }
}
