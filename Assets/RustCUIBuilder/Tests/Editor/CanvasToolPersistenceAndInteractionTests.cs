using NUnit.Framework;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;
using RustCUIBuilder.Editor.Canvas.Tools;
using RustCUIBuilder.Editor.Canvas;

namespace RustCUIBuilder.Tests.Editor
{
    [TestFixture]
    public class CanvasToolPersistenceAndInteractionTests
    {
        private const float CanvasW = 1920f;
        private const float CanvasH = 1080f;
        private RustCanvasCoordinates _coords;

        [SetUp]
        public void SetUp()
        {
            _coords = RustCanvasCoordinates.Instance;
        }

        [Test]
        public void ResizeTool_SEHandle_ExpandsElementAndOffsetsPersist()
        {
            var doc = new CuiDocument();
            var elem = new CuiElementNode("TestPanel", "Overlay");
            var rect = new CuiRectTransformComponent
            {
                AnchorMin = "0.2 0.2",
                AnchorMax = "0.8 0.8",
                OffsetMin = "0 0",
                OffsetMax = "0 0"
            };
            elem.Components.Add(rect);
            doc.AddElement(elem);
            doc.Select(elem.Id);

            var controller = new CanvasToolController();
            var viewportRect = new Rect(0, 0, 2560, 1440);
            var pan = Vector2.zero;
            float zoom = 1f;

            var origCanvasRect = _coords.GetElementCanvasRect(elem, doc, CanvasW, CanvasH);
            var origScreenRect = _coords.CanvasToScreen(origCanvasRect, viewportRect, pan, zoom);
            var seHandlePos = new Vector2(origScreenRect.xMax, origScreenRect.yMax);

            // MouseDown
            bool downHandled = controller.ProcessEvent(
                new Event { type = EventType.MouseDown, button = 0, mousePosition = seHandlePos },
                viewportRect, pan, zoom, CanvasW, CanvasH, doc, null, null, null);
            Assert.IsTrue(downHandled, "MouseDown on SE handle should be handled");
            Assert.IsTrue(controller.IsAnyToolInteracting, "Resize tool should report interacting");

            // MouseDrag
            var dragDelta = new Vector2(100f, 60f);
            bool dragHandled = controller.ProcessEvent(
                new Event { type = EventType.MouseDrag, button = 0, mousePosition = seHandlePos + dragDelta },
                viewportRect, pan, zoom, CanvasW, CanvasH, doc, null, null, null);
            Assert.IsTrue(dragHandled, "MouseDrag on SE handle should be handled");

            // MouseUp
            string undoAction = null;
            bool upHandled = controller.ProcessEvent(
                new Event { type = EventType.MouseUp, button = 0, mousePosition = seHandlePos + dragDelta },
                viewportRect, pan, zoom, CanvasW, CanvasH, doc, null, null, (u) => undoAction = u);
            Assert.IsTrue(upHandled, "MouseUp on SE handle should be handled");
            Assert.IsFalse(controller.IsAnyToolInteracting, "Tool should no longer be interacting after MouseUp");
            Assert.AreEqual("Resize TestPanel", undoAction);

            // Verify offsets expanded and didn't revert
            var oMin = RustCanvasScaler.ParseVector2(rect.OffsetMin, Vector2.zero);
            var oMax = RustCanvasScaler.ParseVector2(rect.OffsetMax, Vector2.zero);
            Assert.AreEqual(0f, oMin.x, 0.5f);
            Assert.AreEqual(-60f, oMin.y, 0.5f);
            Assert.AreEqual(100f, oMax.x, 0.5f);
            Assert.AreEqual(0f, oMax.y, 0.5f);
        }

        [Test]
        public void MoveTool_DragMovesElement_OffsetsPersist()
        {
            var doc = new CuiDocument();
            var elem = new CuiElementNode("MovePanel", "Overlay");
            var rect = new CuiRectTransformComponent
            {
                AnchorMin = "0.5 0.5",
                AnchorMax = "0.5 0.5",
                OffsetMin = "-100 -50",
                OffsetMax = "100 50"
            };
            elem.Components.Add(rect);
            doc.AddElement(elem);
            doc.Select(elem.Id);

            var controller = new CanvasToolController();
            controller.ActiveMode = CanvasToolMode.Move;
            var viewportRect = new Rect(0, 0, 2560, 1440);
            var pan = Vector2.zero;
            float zoom = 1f;

            var origCanvasRect = _coords.GetElementCanvasRect(elem, doc, CanvasW, CanvasH);
            var centerPos = origCanvasRect.center;

            // MouseDown
            controller.ProcessEvent(
                new Event { type = EventType.MouseDown, button = 0, mousePosition = centerPos },
                viewportRect, pan, zoom, CanvasW, CanvasH, doc, null, null, null);
            Assert.IsTrue(controller.IsAnyToolInteracting);

            // MouseDrag
            controller.ProcessEvent(
                new Event { type = EventType.MouseDrag, button = 0, mousePosition = centerPos + new Vector2(40f, -25f) },
                viewportRect, pan, zoom, CanvasW, CanvasH, doc, null, null, null);

            // MouseUp
            string undoAction = null;
            controller.ProcessEvent(
                new Event { type = EventType.MouseUp, button = 0, mousePosition = centerPos + new Vector2(40f, -25f) },
                viewportRect, pan, zoom, CanvasW, CanvasH, doc, null, null, (u) => undoAction = u);

            Assert.AreEqual("Move Element(s)", undoAction);
            var oMin = RustCanvasScaler.ParseVector2(rect.OffsetMin, Vector2.zero);
            var oMax = RustCanvasScaler.ParseVector2(rect.OffsetMax, Vector2.zero);

            Assert.AreEqual(-60f, oMin.x, 0.5f);
            Assert.AreEqual(-25f, oMin.y, 0.5f);
            Assert.AreEqual(140f, oMax.x, 0.5f);
            Assert.AreEqual(75f, oMax.y, 0.5f);
        }

        [Test]
        public void RotateTool_DragRotatesElement_RotationPersists()
        {
            var doc = new CuiDocument();
            var elem = new CuiElementNode("RotPanel", "Overlay");
            var rect = new CuiRectTransformComponent
            {
                AnchorMin = "0.5 0.5",
                AnchorMax = "0.5 0.5",
                OffsetMin = "-50 -50",
                OffsetMax = "50 50",
                Rotation = 0f
            };
            elem.Components.Add(rect);
            doc.AddElement(elem);
            doc.Select(elem.Id);

            var controller = new CanvasToolController();
            controller.ActiveMode = CanvasToolMode.Rotate;
            var viewportRect = new Rect(0, 0, 2560, 1440);
            var pan = Vector2.zero;
            float zoom = 1f;

            var origCanvasRect = _coords.GetElementCanvasRect(elem, doc, CanvasW, CanvasH);
            var center = origCanvasRect.center;

            // MouseDown to the right of center
            controller.ProcessEvent(
                new Event { type = EventType.MouseDown, button = 0, mousePosition = center + new Vector2(100f, 0f) },
                viewportRect, pan, zoom, CanvasW, CanvasH, doc, null, null, null);
            Assert.IsTrue(controller.IsAnyToolInteracting);

            // Drag downwards
            controller.ProcessEvent(
                new Event { type = EventType.MouseDrag, button = 0, mousePosition = center + new Vector2(0f, 100f) },
                viewportRect, pan, zoom, CanvasW, CanvasH, doc, null, null, null);

            string undoAction = null;
            controller.ProcessEvent(
                new Event { type = EventType.MouseUp, button = 0, mousePosition = center + new Vector2(0f, 100f) },
                viewportRect, pan, zoom, CanvasW, CanvasH, doc, null, null, (u) => undoAction = u);

            StringAssert.StartsWith("Rotate RotPanel", undoAction);
            Assert.AreEqual(-90f, rect.Rotation, 1f);
        }

        [Test]
        public void AnchorTool_DragAnchorPin_BypassesViewportGatingDuringDrag()
        {
            var doc = new CuiDocument();
            var elem = new CuiElementNode("AnchorPanel", "Overlay");
            var rect = new CuiRectTransformComponent
            {
                AnchorMin = "0.2 0.2",
                AnchorMax = "0.8 0.8",
                OffsetMin = "0 0",
                OffsetMax = "0 0"
            };
            elem.Components.Add(rect);
            doc.AddElement(elem);
            doc.Select(elem.Id);

            var controller = new CanvasToolController();
            controller.ActiveMode = CanvasToolMode.Anchor;
            var viewportRect = new Rect(0, 0, 2560, 1440);
            var pan = Vector2.zero;
            float zoom = 1f;

            var coords = RustCanvasCoordinates.Instance;
            var origCanvasRect = coords.GetElementCanvasRect(elem, doc, CanvasW, CanvasH);
            var sePinScreen = new Vector2(origCanvasRect.xMax, origCanvasRect.yMax);

            controller.ProcessEvent(
                new Event { type = EventType.MouseDown, button = 0, mousePosition = sePinScreen },
                viewportRect, pan, zoom, CanvasW, CanvasH, doc, null, null, null);
            Assert.IsTrue(controller.IsAnyToolInteracting);

            var outsidePos = new Vector2(2800f, 1600f);
            bool dragHandled = controller.ProcessEvent(
                new Event { type = EventType.MouseDrag, button = 0, mousePosition = outsidePos },
                viewportRect, pan, zoom, CanvasW, CanvasH, doc, null, null, null);
            Assert.IsTrue(dragHandled, "AnchorTool should allow drag events even when outside viewport");

            string undoAction = null;
            bool upHandled = controller.ProcessEvent(
                new Event { type = EventType.MouseUp, button = 0, mousePosition = outsidePos },
                viewportRect, pan, zoom, CanvasW, CanvasH, doc, null, null, (u) => undoAction = u);
            Assert.IsTrue(upHandled, "AnchorTool should receive MouseUp outside viewport");
            Assert.IsFalse(controller.IsAnyToolInteracting);
        }

        [Test]
        public void SelectMode_SeamlessBodyDrag_DelegatesToMoveTool()
        {
            var doc = new CuiDocument();
            var elem = new CuiElementNode("SelectMovePanel", "Overlay");
            var rect = new CuiRectTransformComponent
            {
                AnchorMin = "0.5 0.5",
                AnchorMax = "0.5 0.5",
                OffsetMin = "-50 -50",
                OffsetMax = "50 50"
            };
            elem.Components.Add(rect);
            doc.AddElement(elem);
            doc.Select(elem.Id);

            var controller = new CanvasToolController();
            controller.ActiveMode = CanvasToolMode.Select;
            var viewportRect = new Rect(0, 0, 2560, 1440);
            var pan = Vector2.zero;
            float zoom = 1f;

            var origCanvasRect = _coords.GetElementCanvasRect(elem, doc, CanvasW, CanvasH);
            var centerPos = origCanvasRect.center;

            bool downHandled = controller.ProcessEvent(
                new Event { type = EventType.MouseDown, button = 0, mousePosition = centerPos },
                viewportRect, pan, zoom, CanvasW, CanvasH, doc, null, null, null);
            Assert.IsTrue(downHandled);
            Assert.IsTrue(controller.IsAnyToolInteracting, "MoveTool should be interacting even though ActiveMode is Select");

            controller.ProcessEvent(
                new Event { type = EventType.MouseDrag, button = 0, mousePosition = centerPos + new Vector2(30f, 20f) },
                viewportRect, pan, zoom, CanvasW, CanvasH, doc, null, null, null);

            string undoAction = null;
            controller.ProcessEvent(
                new Event { type = EventType.MouseUp, button = 0, mousePosition = centerPos + new Vector2(30f, 20f) },
                viewportRect, pan, zoom, CanvasW, CanvasH, doc, null, null, (u) => undoAction = u);

            Assert.AreEqual("Move Element(s)", undoAction);
            var oMin = RustCanvasScaler.ParseVector2(rect.OffsetMin, Vector2.zero);
            Assert.AreEqual(-20f, oMin.x, 0.5f);
        }
    }
}
