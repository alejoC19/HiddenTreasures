using System.Linq;
using HuesoOxido.Core;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public sealed class PerfStatsTests
    {
        [Test]
        public void Constant60Fps_GivesExpectedNumbers()
        {
            var s = PerfStats.Summarize(Enumerable.Repeat(16.6667f, 600));
            Assert.AreEqual(600, s.Frames);
            Assert.AreEqual(60f, s.AvgFps, 0.1f);
            Assert.AreEqual(60f, s.OnePercentLowFps, 0.1f);
            Assert.AreEqual(0, s.Hitches);
            Assert.AreEqual(16.6667f, s.P99Ms, 0.01f);
        }

        [Test]
        public void Hitches_LowerTheOnePercentLow_ButNotTheAverageMuch()
        {
            var frames = Enumerable.Repeat(16.6667f, 990).Concat(Enumerable.Repeat(100f, 10)).ToList();
            var s = PerfStats.Summarize(frames);
            Assert.Greater(s.AvgFps, 55f);
            Assert.AreEqual(10f, s.OnePercentLowFps, 0.2f);
            Assert.AreEqual(10, s.Hitches);
            Assert.AreEqual(100f, s.MaxMs, 1e-3f);
        }

        [Test]
        public void InvalidSamples_AreIgnored_AndEmptyIsSafe()
        {
            var s = PerfStats.Summarize(new[] { 0f, -1f, float.NaN, 20f, 20f });
            Assert.AreEqual(2, s.Frames);
            Assert.AreEqual(50f, s.AvgFps, 0.01f);
            Assert.AreEqual(0, PerfStats.Summarize(new float[0]).Frames);
            Assert.AreEqual(0f, PerfStats.Average(new float[0]));
        }

        [Test]
        public void Percentile_InterpolatesBetweenSamples()
        {
            var sorted = new[] { 10f, 20f, 30f, 40f };
            Assert.AreEqual(10f, PerfStats.Percentile(sorted, 0f), 1e-4f);
            Assert.AreEqual(40f, PerfStats.Percentile(sorted, 1f), 1e-4f);
            Assert.AreEqual(25f, PerfStats.Percentile(sorted, 0.5f), 1e-4f);
        }
    }
}
