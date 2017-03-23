namespace NServiceBus.Metrics.Tests.ProcessingTime
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class ComputeProcessingTimeTests
    {
        [Test]
        public void Test_calculation_time()
        {
            var now = DateTime.Now;
            var computed = ProcessingTimeCalculator.Calculate(now, now.AddSeconds(3));
            Assert.IsTrue(Convert.ToInt32(computed.TotalSeconds) == 3);
        }
    }
}