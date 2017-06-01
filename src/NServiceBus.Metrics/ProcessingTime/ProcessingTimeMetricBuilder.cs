using System;
using System.Threading.Tasks;
using Metrics;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics;

class ProcessingTimeMetricBuilder : MetricBuilder
{
    readonly ResetMetricTimer resetMetricTimer;

    public ProcessingTimeMetricBuilder(ResetMetricTimer resetMetricTimer)
    {
        this.resetMetricTimer = resetMetricTimer;
    }

    public override void WireUp(FeatureConfigurationContext featureConfigurationContext)
    {
        resetMetricTimer.NoMessageSentForAWhile += (sender, message) =>
        {
            processingTimeTimer.Record(0, TimeUnit.Milliseconds);
        };

        featureConfigurationContext.Pipeline.OnReceivePipelineCompleted(e =>
        {
            var processingTimeInMilliseconds = ProcessingTimeCalculator.Calculate(e.StartedAt, e.CompletedAt).TotalMilliseconds;

            Console.WriteLine($"ProcTime : {e.StartedAt} to {e.CompletedAt} = {processingTimeInMilliseconds}");

            string messageTypeProcessed;
            e.TryGetMessageType(out messageTypeProcessed);

            processingTimeTimer.Record((long) processingTimeInMilliseconds, TimeUnit.Milliseconds);

            return Task.FromResult(0);
        });
    }

    [Timer("Processing Time", "Messages", "Age of the oldest message in the queue.")]
    Timer processingTimeTimer = default(Timer);
}