namespace NServiceBus.Metrics.Tests
{
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class StandardTests
    {
        [Test]
        public void AllMetrics_should_contain_all_builders()
        {
            var allBuilderTypes = from t in typeof(MetricsOptions).Assembly.GetTypes()
                from i in t.GetInterfaces()
                where
                typeof(IMetricBuilder).IsAssignableFrom(t)
                select t;

            CollectionAssert.AreEquivalent(allBuilderTypes, AllMetrics.Create().Select(b => b.GetType()));
        }
    }
}