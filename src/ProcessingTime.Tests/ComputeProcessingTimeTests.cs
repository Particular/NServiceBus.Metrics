namespace ProcessingTime.Tests
{
    using NUnit.Framework;

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
