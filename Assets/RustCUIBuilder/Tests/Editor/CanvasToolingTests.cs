using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;
using RustCUIBuilder.Editor.Canvas.Services;

namespace RustCUIBuilder.Tests.Editor
{
    [TestFixture]
    public class CanvasToolingTests
    {
        private const float CanvasW = 1920f;
        private const float CanvasH = 1080f;

        [Test]
        public void TestAlignment_AlignLeft_AlignsAllElementsToSmallestX()
        {
            var doc = new CuiDocument();
            var e1 = CreateTestElement("E1", 100, 100, 200, 100);
            var e2 = CreateTestElement("E2", 300, 250, 150, 80);
            var e3 = CreateTestElement("E3", 200, 400, 180, 90);
            doc.AddElement(e1); doc.AddElement(e2); doc.AddElement(e3);

            var list = new List<CuiElementNode> { e1, e2, e3 };
            CanvasAlignmentService.AlignLeft(list, doc, CanvasW, CanvasH, AlignmentTarget.SelectionBounds);

            var coords = RustCanvasCoordinates.Instance;
            Assert.AreEqual(100f, coords.GetElementCanvasRect(e1, doc, CanvasW, CanvasH).xMin, 0.5f);
            Assert.AreEqual(100f, coords.GetElementCanvasRect(e2, doc, CanvasW, CanvasH).xMin, 0.5f);
            Assert.AreEqual(100f, coords.GetElementCanvasRect(e3, doc, CanvasW, CanvasH).xMin, 0.5f);
        }

        [Test]
        public void TestAlignment_AlignCenterH_CentersAroundMidpoint()
        {
            var doc = new CuiDocument();
            var e1 = CreateTestElement("E1", 100, 100, 200, 100); // center: 200
            var e2 = CreateTestElement("E2", 500, 100, 100, 100); // center: 550, max: 600
            doc.AddElement(e1); doc.AddElement(e2);

            // Selection minX = 100, maxX = 600 => targetCenter = 350
            var list = new List<CuiElementNode> { e1, e2 };
            CanvasAlignmentService.AlignCenterH(list, doc, CanvasW, CanvasH, AlignmentTarget.SelectionBounds);

            var coords = RustCanvasCoordinates.Instance;
            Assert.AreEqual(350f, coords.GetElementCanvasRect(e1, doc, CanvasW, CanvasH).center.x, 0.5f);
            Assert.AreEqual(350f, coords.GetElementCanvasRect(e2, doc, CanvasW, CanvasH).center.x, 0.5f);
        }

        [Test]
        public void TestDistribution_EqualHorizontalSpacing_CreatesUniformGaps()
        {
            var doc = new CuiDocument();
            // 3 elements of width 100: Total width = 300.
            // E1 at x=0..100, E3 at x=500..600. Total Span = 600.
            // Total Gap = 600 - 300 = 300. Gap per slot = 300 / 2 = 150.
            // Expected: E1 at 0, E2 at 250, E3 at 500.
            var e1 = CreateTestElement("E1", 0, 100, 100, 50);
            var e2 = CreateTestElement("E2", 150, 100, 100, 50);
            var e3 = CreateTestElement("E3", 500, 100, 100, 50);
            doc.AddElement(e1); doc.AddElement(e2); doc.AddElement(e3);

            var list = new List<CuiElementNode> { e1, e2, e3 };
            CanvasDistributionService.EqualHorizontalSpacing(list, doc, CanvasW, CanvasH);

            var coords = RustCanvasCoordinates.Instance;
            var r1 = coords.GetElementCanvasRect(e1, doc, CanvasW, CanvasH);
            var r2 = coords.GetElementCanvasRect(e2, doc, CanvasW, CanvasH);
            var r3 = coords.GetElementCanvasRect(e3, doc, CanvasW, CanvasH);

            Assert.AreEqual(0f, r1.xMin, 0.5f);
            Assert.AreEqual(250f, r2.xMin, 0.5f);
            Assert.AreEqual(500f, r3.xMin, 0.5f);
            Assert.AreEqual(150f, r2.xMin - r1.xMax, 0.5f);
            Assert.AreEqual(150f, r3.xMin - r2.xMax, 0.5f);
        }

        [Test]
        public void TestHierarchy_GroupAndUngroup_PreservesAbsoluteCanvasCoordinates()
        {
            var doc = new CuiDocument();
            var e1 = CreateTestElement("E1", 200, 300, 100, 50);
            var e2 = CreateTestElement("E2", 350, 320, 120, 60);
            doc.AddElement(e1); doc.AddElement(e2);
            doc.Select(e1.Id, true);
            doc.Select(e2.Id, true);

            var coords = RustCanvasCoordinates.Instance;
            var r1Before = coords.GetElementCanvasRect(e1, doc, CanvasW, CanvasH);
            var r2Before = coords.GetElementCanvasRect(e2, doc, CanvasW, CanvasH);

            // Group
            var group = CanvasHierarchyService.GroupSelection(doc, CanvasW, CanvasH);
            Assert.IsNotNull(group);
            Assert.AreEqual(group.Name, e1.Parent);
            Assert.AreEqual(group.Name, e2.Parent);

            var r1Grouped = coords.GetElementCanvasRect(e1, doc, CanvasW, CanvasH);
            var r2Grouped = coords.GetElementCanvasRect(e2, doc, CanvasW, CanvasH);
            Assert.AreEqual(r1Before.xMin, r1Grouped.xMin, 1f);
            Assert.AreEqual(r1Before.yMin, r1Grouped.yMin, 1f);

            // Ungroup
            CanvasHierarchyService.UngroupSelection(doc, CanvasW, CanvasH);
            Assert.AreEqual("Overlay", e1.Parent);
            Assert.AreEqual("Overlay", e2.Parent);

            var r1After = coords.GetElementCanvasRect(e1, doc, CanvasW, CanvasH);
            var r2After = coords.GetElementCanvasRect(e2, doc, CanvasW, CanvasH);
            Assert.AreEqual(r1Before.xMin, r1After.xMin, 1f);
            Assert.AreEqual(r1Before.yMin, r1After.yMin, 1f);
            Assert.AreEqual(r2Before.xMin, r2After.xMin, 1f);
            Assert.AreEqual(r2Before.yMin, r2After.yMin, 1f);
        }

        [Test]
        public void TestLayout_CenterInParent_AlignsToParentCenter()
        {
            var doc = new CuiDocument();
            var parent = CreateTestElement("Parent", 200, 200, 600, 400);
            var child = CreateTestElement("Child", 0, 0, 200, 100);
            child.Parent = "Parent";
            doc.AddElement(parent); doc.AddElement(child);

            var coords = RustCanvasCoordinates.Instance;
            CanvasLayoutService.CenterInParent(new List<CuiElementNode> { child }, doc, CanvasW, CanvasH);

            var pRect = coords.GetElementCanvasRect(parent, doc, CanvasW, CanvasH);
            var cRect = coords.GetElementCanvasRect(child, doc, CanvasW, CanvasH);

            Assert.AreEqual(pRect.center.x, cRect.center.x, 0.5f);
            Assert.AreEqual(pRect.center.y, cRect.center.y, 0.5f);
        }

        [Test]
        public void TestClipboard_Duplicate_CreatesOffsetCloneWithUniqueName()
        {
            var doc = new CuiDocument();
            var original = CreateTestElement("Original", 100, 100, 200, 80);
            doc.AddElement(original);
            doc.Select(original.Id);

            var duplicated = CanvasClipboardService.Duplicate(new List<CuiElementNode> { original }, doc, CanvasW, CanvasH);
            Assert.AreEqual(1, duplicated.Count);
            Assert.AreNotEqual(original.Id, duplicated[0].Id);
            Assert.AreNotEqual(original.Name, duplicated[0].Name);

            var coords = RustCanvasCoordinates.Instance;
            var rOrig = coords.GetElementCanvasRect(original, doc, CanvasW, CanvasH);
            var rDup = coords.GetElementCanvasRect(duplicated[0], doc, CanvasW, CanvasH);

            Assert.AreEqual(rOrig.xMin + 20f, rDup.xMin, 0.5f);
            Assert.AreEqual(rOrig.yMin + 20f, rDup.yMin, 0.5f);
        }

        private CuiElementNode CreateTestElement(string name, float x, float y, float w, float h)
        {
            var elem = new CuiElementNode(name, "Overlay");
            var rect = new CuiRectTransformComponent();
            elem.Components.Add(rect);
            RustCanvasCoordinates.Instance.ApplyNewCanvasRectToElementOffsets(new Rect(x, y, w, h), elem, null, CanvasW, CanvasH);
            return elem;
        }
    }
}
