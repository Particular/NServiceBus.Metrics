namespace NServiceBus.Metrics
{
    using Newtonsoft.Json.Linq;

    public class MetricReport : IMessage
    {
        public JObject Data { get; set; }
    }
}