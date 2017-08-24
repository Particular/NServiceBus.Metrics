using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Features;

static class Extensions
{
    public static bool TryGetTimeSent(this ReceivePipelineCompleted completed, out DateTime timeSent)
    {
        var headers = completed.ProcessedMessage.Headers;
        if (headers.TryGetValue(Headers.TimeSent, out string timeSentString))
        {
            timeSent = DateTimeExtensions.ToUtcDateTime(timeSentString);
            return true;
        }
        timeSent = DateTime.MinValue;
        return false;
    }

    public static bool TryGetMessageType(this ReceivePipelineCompleted completed, out string processedMessageType)
    {
        return completed.ProcessedMessage.Headers.TryGetMessageType(out processedMessageType);
    }

    internal static bool TryGetMessageType(this IReadOnlyDictionary<string, string> headers, out string processedMessageType)
    {
        if (headers.TryGetValue(Headers.EnclosedMessageTypes, out string enclosedMessageType))
        {
            processedMessageType = enclosedMessageType;
            return true;
        }
        processedMessageType = "Undefined";
        return false;
    }

    public static void ThrowIfSendonly(this FeatureConfigurationContext context)
    {
        var isSendOnly = context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
        if (isSendOnly)
        {
            throw new Exception("Metrics are not supported on send only endpoints.");
        }
    }
}