namespace NServiceBus.Metrics.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using ApprovalTests;
    using ApprovalTests.Reporters;
    using NUnit.Framework;
    using PublicApiGenerator;

    [TestFixture]
    public class APIApprovals
    {
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [UseReporter(typeof(DiffReporter))]
        public void ApproveAPI()
        {
            var publicApi = Filter(ApiGenerator.GeneratePublicApi(typeof(MetricsFeature).Assembly));
            Approvals.Verify(publicApi);
        }

        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [UseReporter(typeof(DiffReporter))]
        public void ApproveMetrics()
        {
            var writer = new StringWriter();
            AllMetrics.Define(writer);
            Approvals.Verify(writer);
        }

        string Filter(string text)
        {
            return string.Join(Environment.NewLine, text.Split(new[]
            {
                Environment.NewLine
            }, StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !l.StartsWith("[assembly: ReleaseDateAttribute("))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                );
        }

    }
}