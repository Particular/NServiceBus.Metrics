namespace NServiceBus.Metrics.Tests
{
    using System;
    using System.Linq;
    using System.Text;
    using global::Metrics;
    using global::Metrics.Core;
    using global::Metrics.Json;
    using global::Metrics.MetricData;
    using NUnit.Framework;

    [TestFixture]
    public class PayloadSizeTest
    {
        const int MaxBodySize = 32 * 1024;

        [Test]
        public void It_can_fit_500_gauge_values_in_half_of_lowest_transport_message_limit()
        {
            var gauges = Enumerable.Range(0, 500).Select(i => new GaugeValueSource("Gauge " + i, new FunctionGauge(() => 666), Unit.Calls, MetricTags.None));
            var data = new MetricsData("ContextName", DateTime.UtcNow, 
                new EnvironmentEntry[0], 
                gauges, 
                new CounterValueSource[0], 
                new MeterValueSource[0], 
                new HistogramValueSource[0], 
                new TimerValueSource[0], 
                new MetricsData[0]);

            var serialized = JsonBuilderV2.BuildJson(data);

            var body = Encoding.UTF8.GetBytes(serialized);

            Assert.IsTrue(body.Length < MaxBodySize);
        }
    }
}