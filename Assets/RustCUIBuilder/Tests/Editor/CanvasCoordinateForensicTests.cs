using NUnit.Framework;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Tests.Editor
{
    public class CanvasCoordinateForensicTests
    {
        [TestCase(0, 0, 1920, 1080, 100, 50, 0.5f, 0, 0)]
        [TestCase(50, 50, 1280, 720, 300, 200, 1.25f, 40, -30)]
        [TestCase(100, 80, 2560, 1440, 800, 600, 2.0f, 150, 200)]
        public void ScreenToCanvas_RoundTrip_ReturnsOriginalScreenPoint(
            float vpX, float vpY, float vpW, float vpH,
            float origScreenX, float origScreenY, float zoom, float panX, float panY)
        {
            var coords = RustCanvasCoordinates.Instance;
            var viewport = new Rect(vpX, vpY, vpW, vpH);
            var pan = new Vector2(panX, panY);
            var origScreen = new Vector2(origScreenX, origScreenY);

            var canvasPoint = coords.ScreenToCanvas(origScreen, viewport, pan, zoom);
            var resultScreen = coords.CanvasToScreen(canvasPoint, viewport, pan, zoom);

            Assert.AreEqual(origScreen.x, resultScreen.x, 0.001f);
            Assert.AreEqual(origScreen.y, resultScreen.y, 0.001f);
        }

        [TestCase(0.0f, 0.0f, 1920f, 1080f, 0f, 1080f)]
        [TestCase(1.0f, 1.0f, 1920f, 1080f, 1920f, 0f)]
        [TestCase(0.5f, 0.5f, 1920f, 1080f, 960f, 540f)]
        [TestCase(0.2f, 0.8f, 1280f, 720f, 256f, 144f)]
        public void RustToCanvas_NormalizedCoordinates_MapToCorrectCanvasPixels(
            float normX, float normY, float canvasW, float canvasH, float expectedX, float expectedY)
        {
            var coords = RustCanvasCoordinates.Instance;
            var norm = new Vector2(normX, normY);

            var canvasPos = coords.RustToCanvas(norm, canvasW, canvasH);
            Assert.AreEqual(expectedX, canvasPos.x, 0.01f);
            Assert.AreEqual(expectedY, canvasPos.y, 0.01f);

            var roundTripNorm = coords.CanvasToRust(canvasPos, canvasW, canvasH);
            Assert.AreEqual(normX, roundTripNorm.x, 0.001f);
            Assert.AreEqual(normY, roundTripNorm.y, 0.001f);
        }

        [Test]
        public void ZoomToCursor_PreservesCursorWorldPosition()
        {
            var coords = RustCanvasCoordinates.Instance;
            var viewport = new Rect(0, 0, 1920, 1080);
            var pan = new Vector2(100, 100);
            float zoom1 = 0.5f;
            float zoom2 = 1.5f;

            var cursorScreen = new Vector2(500, 400);

            // Canvas coordinate under cursor at zoom1
            var canvasUnderCursor1 = coords.ScreenToCanvas(cursorScreen, viewport, pan, zoom1);

            // Compute new pan offset when zooming toward cursor
            var mouseRel = cursorScreen - (viewport.position + pan);
            var newPan = pan - mouseRel * (zoom2 / zoom1 - 1f);

            // Canvas coordinate under cursor at zoom2
            var canvasUnderCursor2 = coords.ScreenToCanvas(cursorScreen, viewport, newPan, zoom2);

            Assert.AreEqual(canvasUnderCursor1.x, canvasUnderCursor2.x, 0.001f);
            Assert.AreEqual(canvasUnderCursor1.y, canvasUnderCursor2.y, 0.001f);
        }

        [Test]
        public void NestedElement_CanvasRect_ResolvesRelativeToParent()
        {
            var coords = RustCanvasCoordinates.Instance;
            var doc = new CuiDocument();

            var parent = new CuiElementNode { Name = "ParentPanel", Parent = "Overlay" };
            parent.Components.Add(new CuiRectTransformComponent
            {
                AnchorMin = "0.2 0.2",
                AnchorMax = "0.8 0.8",
                OffsetMin = "0 0",
                OffsetMax = "0 0"
            });
            doc.AddElement(parent);

            var child = new CuiElementNode { Name = "ChildHeader", Parent = "ParentPanel" };
            child.Components.Add(new CuiRectTransformComponent
            {
                AnchorMin = "0 0.8",
                AnchorMax = "1 1",
                OffsetMin = "10 0",
                OffsetMax = "-10 0"
            });
            doc.AddElement(child);

            float canvasW = 1000f;
            float canvasH = 1000f;

            var pRect = coords.GetElementCanvasRect(parent, doc, canvasW, canvasH);
            Assert.AreEqual(200f, pRect.xMin, 0.01f);
            Assert.AreEqual(800f, pRect.xMax, 0.01f);
            Assert.AreEqual(200f, pRect.yMin, 0.01f);
            Assert.AreEqual(800f, pRect.yMax, 0.01f);
            Assert.AreEqual(600f, pRect.width, 0.01f);
            Assert.AreEqual(600f, pRect.height, 0.01f);

            var cRect = coords.GetElementCanvasRect(child, doc, canvasW, canvasH);
            // Child is in top 20% of parent (yMin = pRect.yMin = 200, yMax = 200 + 600 * 0.2 = 320)
            Assert.AreEqual(210f, cRect.xMin, 0.01f); // 200 + 10px offset
            Assert.AreEqual(790f, cRect.xMax, 0.01f); // 800 - 10px offset
            Assert.AreEqual(200f, cRect.yMin, 0.01f);
            Assert.AreEqual(320f, cRect.yMax, 0.01f);
        }
    }
}
