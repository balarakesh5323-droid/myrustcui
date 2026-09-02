using NUnit.Framework;
using UnityEngine;
using RustCUIBuilder.Editor.Canvas;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Tests.Editor
{
    [TestFixture]
    public class CanvasViewportTests
    {
        private RustCanvasCoordinates _coords;

        [SetUp]
        public void Setup()
        {
            _coords = RustCanvasCoordinates.Instance;
        }

        [Test]
        public void Test_1_ViewportCalculation()
        {
            var containerRect = new Rect(260, 22, 1000, 700);
            float topToolbarHeight = 26f;
            float bottomToolbarHeight = 24f;

            var viewportRect = new Rect(
                containerRect.x,
                containerRect.y + topToolbarHeight,
                containerRect.width,
                containerRect.height - topToolbarHeight - bottomToolbarHeight
            );

            Assert.AreEqual(260f, viewportRect.x);
            Assert.AreEqual(48f, viewportRect.y);
            Assert.AreEqual(1000f, viewportRect.width);
            Assert.AreEqual(650f, viewportRect.height);
        }

        [Test]
        public void Test_2_CanvasCentering_FitCalculatesEvenMargins()
        {
            var view = new CuiCanvasEditorView();
            var viewport = new Rect(0, 0, 1200, 800);

            view.FitCanvas(viewport);

            float expectedScaleW = (1200f - 80f) / 1920f;
            float expectedScaleH = (800f - 80f) / 1080f;
            float expectedZoom = Mathf.Min(expectedScaleW, expectedScaleH);

            Assert.AreEqual(expectedZoom, view.CurrentZoom, 0.001f);

            float screenW = 1920f * view.CurrentZoom;
            float screenH = 1080f * view.CurrentZoom;
            float expectedPanX = (1200f - screenW) * 0.5f;
            float expectedPanY = (800f - screenH) * 0.5f;

            Assert.AreEqual(expectedPanX, view.CurrentPan.x, 0.001f);
            Assert.AreEqual(expectedPanY, view.CurrentPan.y, 0.001f);
        }

        [Test]
        public void Test_3_FitZoom_ClampsToSafeRange()
        {
            var view = new CuiCanvasEditorView();
            var tinyViewport = new Rect(0, 0, 100, 100);

            view.FitCanvas(tinyViewport);
            Assert.GreaterOrEqual(view.CurrentZoom, 0.2f);
            Assert.LessOrEqual(view.CurrentZoom, 2.0f);
        }

        [Test]
        public void Test_4_ZoomAroundCursor_PreservesCanvasPointUnderMouse()
        {
            var viewportRect = new Rect(0, 0, 1000, 600);
            Vector2 mouseViewportPos = new Vector2(450, 320);

            Vector2 initialPan = new Vector2(50, 40);
            float initialZoom = 0.5f;

            // 1. Find point on canvas under mouse before zoom
            Vector2 canvasPoint = _coords.ViewportToCanvas(mouseViewportPos, initialPan, initialZoom);

            // 2. Perform zoom
            float newZoom = 1.25f;
            Vector2 newPan = mouseViewportPos - canvasPoint * newZoom;

            // 3. Invariant: Canvas point under mouse after zoom must evaluate to mouseViewportPos
            Vector2 afterZoomScreen = _coords.CanvasToViewport(canvasPoint, newPan, newZoom);

            Assert.AreEqual(mouseViewportPos.x, afterZoomScreen.x, 0.001f);
            Assert.AreEqual(mouseViewportPos.y, afterZoomScreen.y, 0.001f);
        }

        [Test]
        public void Test_5_PanTransformation_DoesNotModifyElementCoordinates()
        {
            var doc = new CuiDocument();
            var elem = new CuiElementNode("TestPanel", "Overlay");
            elem.Components.Add(new CuiRectTransformComponent
            {
                AnchorMin = "0.2 0.3",
                AnchorMax = "0.8 0.7",
                OffsetMin = "10 20",
                OffsetMax = "-10 -20"
            });
            doc.AddElement(elem);

            var rectBefore = _coords.GetElementCanvasRect(elem, doc, 1920, 1080);

            // Change pan and zoom
            Vector2 pan1 = new Vector2(100, 50);
            Vector2 pan2 = new Vector2(500, -200);

            var screen1 = _coords.GetElementScreenRect(elem, doc, new Rect(0, 0, 1000, 600), pan1, 1f, 1920, 1080);
            var screen2 = _coords.GetElementScreenRect(elem, doc, new Rect(0, 0, 1000, 600), pan2, 1f, 1920, 1080);

            var rectAfter = _coords.GetElementCanvasRect(elem, doc, 1920, 1080);

            // Invariant: Canvas space element rect is invariant under pan
            Assert.AreEqual(rectBefore.xMin, rectAfter.xMin, 0.001f);
            Assert.AreEqual(rectBefore.yMin, rectAfter.yMin, 0.001f);
            Assert.AreEqual(rectBefore.width, rectAfter.width, 0.001f);
            Assert.AreEqual(rectBefore.height, rectAfter.height, 0.001f);

            // Screen space shifts by exact pan delta
            Assert.AreEqual(400f, screen2.x - screen1.x, 0.001f);
            Assert.AreEqual(-250f, screen2.y - screen1.y, 0.001f);
        }

        [Test]
        public void Test_6_ScreenToViewport_And_ViewportToScreen()
        {
            var viewport = new Rect(250, 50, 800, 600);
            var screenPoint = new Vector2(350, 150);

            var viewportPoint = _coords.ScreenToViewport(screenPoint, viewport);
            Assert.AreEqual(100f, viewportPoint.x);
            Assert.AreEqual(100f, viewportPoint.y);

            var backToScreen = _coords.ViewportToScreen(viewportPoint, viewport);
            Assert.AreEqual(screenPoint.x, backToScreen.x);
            Assert.AreEqual(screenPoint.y, backToScreen.y);
        }

        [Test]
        public void Test_7_ViewportToCanvas_And_CanvasToViewport()
        {
            Vector2 pan = new Vector2(80, 60);
            float zoom = 0.5f;

            var canvasPoint = new Vector2(1920, 1080);
            var viewportPoint = _coords.CanvasToViewport(canvasPoint, pan, zoom);

            Assert.AreEqual(80f + 1920f * 0.5f, viewportPoint.x);
            Assert.AreEqual(60f + 1080f * 0.5f, viewportPoint.y);

            var backToCanvas = _coords.ViewportToCanvas(viewportPoint, pan, zoom);
            Assert.AreEqual(canvasPoint.x, backToCanvas.x, 0.001f);
            Assert.AreEqual(canvasPoint.y, backToCanvas.y, 0.001f);
        }

        [Test]
        public void Test_8_ScreenToCanvas_And_CanvasToScreen_Composition()
        {
            var viewport = new Rect(200, 100, 1000, 700);
            Vector2 pan = new Vector2(50, 50);
            float zoom = 0.75f;

            Vector2 screenPos = new Vector2(500, 400);
            Vector2 canvasPos = _coords.ScreenToCanvas(screenPos, viewport, pan, zoom);
            Vector2 backToScreen = _coords.CanvasToScreen(canvasPos, viewport, pan, zoom);

            Assert.AreEqual(screenPos.x, backToScreen.x, 0.001f);
            Assert.AreEqual(screenPos.y, backToScreen.y, 0.001f);
        }

        [Test]
        public void Test_9_RustToCanvas_And_CanvasToRust()
        {
            Vector2 rustNormalized = new Vector2(0.25f, 0.75f);
            float canvasW = 1920f;
            float canvasH = 1080f;

            Vector2 canvasPoint = _coords.RustToCanvas(rustNormalized, canvasW, canvasH);
            Assert.AreEqual(480f, canvasPoint.x);
            Assert.AreEqual(270f, canvasPoint.y); // Y is flipped in GUI (1.0 - 0.75) * 1080 = 270

            Vector2 backToRust = _coords.CanvasToRust(canvasPoint, canvasW, canvasH);
            Assert.AreEqual(rustNormalized.x, backToRust.x, 0.001f);
            Assert.AreEqual(rustNormalized.y, backToRust.y, 0.001f);
        }

        [Test]
        public void Test_10_ViewportContainment()
        {
            var localViewport = new Rect(0, 0, 800, 600);

            Assert.IsTrue(localViewport.Contains(new Vector2(400, 300)));
            Assert.IsTrue(localViewport.Contains(new Vector2(0, 0)));
            Assert.IsFalse(localViewport.Contains(new Vector2(-10, 300)));
            Assert.IsFalse(localViewport.Contains(new Vector2(850, 300)));
            Assert.IsFalse(localViewport.Contains(new Vector2(400, -5)));
            Assert.IsFalse(localViewport.Contains(new Vector2(400, 650)));
        }

        [Test]
        public void Test_11_CuiRectTransformOffsetsRemainUnaltered()
        {
            var rectComp = new CuiRectTransformComponent
            {
                AnchorMin = "0.1 0.1",
                AnchorMax = "0.9 0.9",
                OffsetMin = "15 15",
                OffsetMax = "-15 -15"
            };

            Assert.AreEqual("0.1 0.1", rectComp.AnchorMin);
            Assert.AreEqual("0.9 0.9", rectComp.AnchorMax);
            Assert.AreEqual("15 15", rectComp.OffsetMin);
            Assert.AreEqual("-15 -15", rectComp.OffsetMax);
        }

        [Test]
        public void Test_12_ResponsiveWindowResize_AdaptsViewportDimensions()
        {
            float windowW1 = 1280f;
            float leftW1 = Mathf.Clamp(windowW1 * 0.18f, 220f, 280f);
            float rightW1 = Mathf.Clamp(windowW1 * 0.24f, 280f, 380f);
            float centerW1 = windowW1 - leftW1 - rightW1;

            Assert.Greater(centerW1, 500f);

            float windowW2 = 2560f;
            float leftW2 = Mathf.Clamp(windowW2 * 0.18f, 220f, 280f);
            float rightW2 = Mathf.Clamp(windowW2 * 0.24f, 280f, 380f);
            float centerW2 = windowW2 - leftW2 - rightW2;

            Assert.AreEqual(280f, leftW2);
            Assert.AreEqual(380f, rightW2);
            Assert.AreEqual(1900f, centerW2);
        }
    }
}
