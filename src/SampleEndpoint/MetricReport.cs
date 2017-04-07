namespace NServiceBus.Metrics
{
    using global::Newtonsoft.Json.Linq;

    public class MetricReport : IMessage
    {
        public JObject Data { get; set; }
    }
}