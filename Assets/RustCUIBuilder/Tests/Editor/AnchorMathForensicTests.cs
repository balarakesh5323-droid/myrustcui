using NUnit.Framework;
using RustCUIBuilder.Runtime.Rendering.Canvas;
using UnityEngine;

namespace RustCUIBuilder.Tests.Editor
{
    public class AnchorMathForensicTests
    {
        [TestCase(1280, 720, "0 0", "1 1", "0 0", "0 0", 0, 0, 1280, 720)]
        [TestCase(1920, 1080, "0 0", "1 1", "0 0", "0 0", 0, 0, 1920, 1080)]
        [TestCase(2560, 1440, "0 0", "1 1", "0 0", "0 0", 0, 0, 2560, 1440)]
        [TestCase(3840, 2160, "0 0", "1 1", "0 0", "0 0", 0, 0, 3840, 2160)]
        public void AnchorMath_FullStretch_MatchesExactScreenDimensions(
            int screenW, int screenH, string aMin, string aMax, string oMin, string oMax,
            float expectedXMin, float expectedYMin, float expectedXMax, float expectedYMax)
        {
            var screenRect = new Rect(0, 0, screenW, screenH);
            Vector2 minAnchor = RustCanvasScaler.ParseVector2(aMin, Vector2.zero);
            Vector2 maxAnchor = RustCanvasScaler.ParseVector2(aMax, Vector2.one);
            Vector2 minOffset = RustCanvasScaler.ParseVector2(oMin, Vector2.zero);
            Vector2 maxOffset = RustCanvasScaler.ParseVector2(oMax, Vector2.zero);

            float xMin = screenRect.x + screenRect.width * minAnchor.x + minOffset.x;
            float yMin = screenRect.y + screenRect.height * minAnchor.y + minOffset.y;
            float xMax = screenRect.x + screenRect.width * maxAnchor.x + maxOffset.x;
            float yMax = screenRect.y + screenRect.height * maxAnchor.y + maxOffset.y;

            Assert.AreEqual(expectedXMin, xMin, 0.01f);
            Assert.AreEqual(expectedYMin, yMin, 0.01f);
            Assert.AreEqual(expectedXMax, xMax, 0.01f);
            Assert.AreEqual(expectedYMax, yMax, 0.01f);
        }

        [TestCase(1920, 1080, "0.5 0.5", "0.5 0.5", "-100 -50", "100 50", 860, 490, 1060, 590)]
        public void AnchorMath_CenteredFixedSize_CalculatesExactPixelBounds(
            int screenW, int screenH, string aMin, string aMax, string oMin, string oMax,
            float expectedXMin, float expectedYMin, float expectedXMax, float expectedYMax)
        {
            var screenRect = new Rect(0, 0, screenW, screenH);
            Vector2 minAnchor = RustCanvasScaler.ParseVector2(aMin, Vector2.zero);
            Vector2 maxAnchor = RustCanvasScaler.ParseVector2(aMax, Vector2.one);
            Vector2 minOffset = RustCanvasScaler.ParseVector2(oMin, Vector2.zero);
            Vector2 maxOffset = RustCanvasScaler.ParseVector2(oMax, Vector2.zero);

            float xMin = screenRect.x + screenRect.width * minAnchor.x + minOffset.x;
            float yMin = screenRect.y + screenRect.height * minAnchor.y + minOffset.y;
            float xMax = screenRect.x + screenRect.width * maxAnchor.x + maxOffset.x;
            float yMax = screenRect.y + screenRect.height * maxAnchor.y + maxOffset.y;

            Assert.AreEqual(expectedXMin, xMin, 0.01f);
            Assert.AreEqual(expectedYMin, yMin, 0.01f);
            Assert.AreEqual(expectedXMax, xMax, 0.01f);
            Assert.AreEqual(expectedYMax, yMax, 0.01f);
            Assert.AreEqual(200f, xMax - xMin, 0.01f);
            Assert.AreEqual(100f, yMax - yMin, 0.01f);
        }
    }
}
