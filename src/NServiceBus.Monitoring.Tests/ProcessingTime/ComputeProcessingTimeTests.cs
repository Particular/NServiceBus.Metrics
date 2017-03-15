namespace NServiceBus.Monitoring.Tests
{
    using NUnit.Framework;
    using ProcessingTime;

    [TestFixture]
    public class ComputeProcessingTimeTests
    {
        [Test]
        public void TestIsWorking()
        {
            var processingTimeCalculator = new ComputeProcessingTime();
            Assert.IsTrue(processingTimeCalculator.IsWorking());
        }
    }
}
