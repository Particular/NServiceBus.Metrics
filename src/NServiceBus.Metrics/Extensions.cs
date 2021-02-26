using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Features;

static class Extensions
{
    public static bool TryGetTimeSent(this ReceiveCompleted completed, out DateTimeOffset timeSent)
    {
        var headers = completed.Headers;
        if (headers.TryGetValue(Headers.TimeSent, out var timeSentString))
        {
            timeSent = DateTimeOffsetHelper.ToDateTimeOffset(timeSentString);
            return true;
        }
        timeSent = DateTime.MinValue;
        return false;
    }

    public static bool TryGetMessageType(this ReceiveCompleted completed, out string processedMessageType)
    {
        return completed.Headers.TryGetMessageType(out processedMessageType);
    }

    internal static bool TryGetMessageType(this IReadOnlyDictionary<string, string> headers, out string processedMessageType)
    {
        if (headers.TryGetValue(Headers.EnclosedMessageTypes, out var enclosedMessageType))
        {
            processedMessageType = enclosedMessageType;
            return true;
        }
        processedMessageType = null;
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