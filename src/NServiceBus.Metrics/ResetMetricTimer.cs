using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;

class ResetMetricTimer
{
    public event EventHandler NoMessageSentForAWhile;

    public ResetMetricTimer(FeatureConfigurationContext featureConfigurationContext)
    {
        timer = new System.Threading.Timer(ResetCounterValueIfNoMessageHasBeenProcessedRecently, null, 0, 2000);

        featureConfigurationContext.Pipeline.OnReceivePipelineCompleted(e =>
        {
            DateTime timeSent;
            if (e.TryGetTimeSent(out timeSent))
            {
                lastMessageProcessedTime = e.CompletedAt;
            }

            return Task.FromResult(0);
        });

    }

    void ResetCounterValueIfNoMessageHasBeenProcessedRecently(object state)
    {
        // Concurreny : This runs every 2s and might just fire before an update
        if (NoMessageHasBeenProcessedRecently())
        {
            NoMessageSentForAWhile?.Invoke(this, null);
        }
    }

    bool NoMessageHasBeenProcessedRecently()
    {
        var timeFromLastMessageProcessed = DateTime.UtcNow - lastMessageProcessedTime;
        // Concurrency : This currently ignores messages being processed atm.
        return timeFromLastMessageProcessed > estimatedMaximumProcessingDuration;
    }

    TimeSpan estimatedMaximumProcessingDuration = TimeSpan.FromSeconds(2);
    DateTime lastMessageProcessedTime;
    // ReSharper disable once NotAccessedField.Local
    System.Threading.Timer timer; // We need this member or GC will dispose timer immediately.
}