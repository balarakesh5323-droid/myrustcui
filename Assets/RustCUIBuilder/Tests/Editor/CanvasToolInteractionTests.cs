using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RustCUIBuilder.Editor.Canvas;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Tests.Editor
{
    public class CanvasToolInteractionTests
    {
        [Test]
        public void CanvasAlignment_AlignLeft_AlignsAllElementsToMinX()
        {
            var doc = new CuiDocument();
            var e1 = new CuiElementNode { Name = "E1", Parent = "Overlay" };
            e1.Components.Add(new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "100 100", OffsetMax = "200 200" });

            var e2 = new CuiElementNode { Name = "E2", Parent = "Overlay" };
            e2.Components.Add(new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "300 100", OffsetMax = "400 200" });

            doc.AddElement(e1);
            doc.AddElement(e2);

            var list = new List<CuiElementNode> { e1, e2 };
            CanvasAlignmentEngine.AlignLeft(list, doc, 1000, 1000);

            var coords = RustCanvasCoordinates.Instance;
            var r1 = coords.GetElementCanvasRect(e1, doc, 1000, 1000);
            var r2 = coords.GetElementCanvasRect(e2, doc, 1000, 1000);

            Assert.AreEqual(r1.xMin, r2.xMin, 0.01f);
            Assert.AreEqual(100f, r1.xMin, 0.01f);
        }

        [Test]
        public void CanvasAlignment_AlignCenter_AlignsAllElementsToAverageCenter()
        {
            var doc = new CuiDocument();
            var e1 = new CuiElementNode { Name = "E1", Parent = "Overlay" };
            e1.Components.Add(new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "100 100", OffsetMax = "200 200" }); // center = 150

            var e2 = new CuiElementNode { Name = "E2", Parent = "Overlay" };
            e2.Components.Add(new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "300 100", OffsetMax = "400 200" }); // center = 350

            doc.AddElement(e1);
            doc.AddElement(e2);

            var list = new List<CuiElementNode> { e1, e2 };
            CanvasAlignmentEngine.AlignCenter(list, doc, 1000, 1000); // avg center = 250

            var coords = RustCanvasCoordinates.Instance;
            var r1 = coords.GetElementCanvasRect(e1, doc, 1000, 1000);
            var r2 = coords.GetElementCanvasRect(e2, doc, 1000, 1000);

            Assert.AreEqual(250f, r1.center.x, 0.01f);
            Assert.AreEqual(250f, r2.center.x, 0.01f);
        }

        [Test]
        public void CanvasGuideSystem_GridSnap_SnapsToExactGridMultiples()
        {
            var guides = new CanvasGuideSystem { SnapToGrid = true, GridSize = 20f, SnapTolerancePixels = 10f };
            var original = new Vector2(103f, 196f);

            var snapped = guides.SnapCanvasPoint(original, 1.0f);
            Assert.AreEqual(100f, snapped.x, 0.01f);
            Assert.AreEqual(200f, snapped.y, 0.01f);
        }
    }
}
