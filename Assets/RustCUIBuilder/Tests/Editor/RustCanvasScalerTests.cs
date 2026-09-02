using NUnit.Framework;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Core.Validation;
using RustCUIBuilder.Runtime.Rendering.Canvas;
using UnityEngine;

namespace RustCUIBuilder.Tests.Editor
{
    [TestFixture]
    public class RustCanvasScalerTests
    {
        [Test]
        public void Test_ParseVector2_VariousFormats()
        {
            var v1 = RustCanvasScaler.ParseVector2("0.5 0.5", Vector2.zero);
            Assert.AreEqual(0.5f, v1.x, 0.001f);
            Assert.AreEqual(0.5f, v1.y, 0.001f);

            var v2 = RustCanvasScaler.ParseVector2("-100, 250", Vector2.zero);
            Assert.AreEqual(-100f, v2.x, 0.001f);
            Assert.AreEqual(250f, v2.y, 0.001f);

            var v3 = RustCanvasScaler.ParseVector2("invalid", Vector2.one);
            Assert.AreEqual(1f, v3.x);
            Assert.AreEqual(1f, v3.y);
        }

        [Test]
        public void Test_CalculateScreenRect_AccurateBounds()
        {
            var parent = new Rect(0, 0, 1280, 720);
            var rect = RustCanvasScaler.CalculateScreenRect(parent, new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), Vector2.zero, Vector2.zero);

            Assert.AreEqual(256f, rect.xMin, 0.001f);
            Assert.AreEqual(144f, rect.yMin, 0.001f);
            Assert.AreEqual(1024f, rect.xMax, 0.001f);
            Assert.AreEqual(576f, rect.yMax, 0.001f);
            Assert.AreEqual(768f, rect.width, 0.001f);
            Assert.AreEqual(432f, rect.height, 0.001f);
        }
    }

    [TestFixture]
    public class CuiValidatorTests
    {
        [Test]
        public void Test_Validator_CatchesDuplicateNames()
        {
            var doc = new CuiDocument();
            var e1 = new CuiElementNode("DuplicateName", "Overlay");
            e1.Components.Add(new CuiRectTransformComponent());
            var e2 = new CuiElementNode("DuplicateName", "Overlay");
            e2.Components.Add(new CuiRectTransformComponent());

            doc.Elements.Add(e1);
            doc.Elements.Add(e2);

            var report = CuiValidator.ValidateDocument(doc);
            Assert.IsFalse(report.IsValid);
            Assert.GreaterOrEqual(report.ErrorCount, 1);
        }

        [Test]
        public void Test_Validator_CatchesInvalidAnchors()
        {
            var doc = new CuiDocument();
            var elem = new CuiElementNode("BadAnchorElem", "Overlay");
            elem.Components.Add(new CuiRectTransformComponent
            {
                AnchorMin = "0.8 0.8",
                AnchorMax = "0.2 0.2" // Min > Max
            });
            doc.Elements.Add(elem);

            var report = CuiValidator.ValidateDocument(doc);
            Assert.IsFalse(report.IsValid);
            Assert.GreaterOrEqual(report.ErrorCount, 1);
        }
    }
}
