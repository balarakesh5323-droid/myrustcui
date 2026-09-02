using NUnit.Framework;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Core.Serialization;
using UnityEngine;

namespace RustCUIBuilder.Tests.Editor
{
    [TestFixture]
    public class CuiSerializationTests
    {
        [Test]
        public void Test_SerializeAndDeserialize_Roundtrip()
        {
            var doc = new CuiDocument { ProjectName = "TestDoc" };

            var panel = new CuiElementNode("MainPanel", "Overlay");
            panel.Components.Add(new CuiImageComponent { Color = "0.1 0.2 0.3 0.9", Sprite = "assets/content/ui/ui.background.tile.psd" });
            panel.Components.Add(new CuiRectTransformComponent { AnchorMin = "0.1 0.2", AnchorMax = "0.9 0.8", OffsetMin = "-10 -20", OffsetMax = "10 20" });
            panel.Components.Add(new CuiNeedsCursorComponent());

            var label = new CuiElementNode("HeaderLabel", "MainPanel");
            label.Components.Add(new CuiTextComponent { Text = "Welcome to Rust", FontSize = 22, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" });
            label.Components.Add(new CuiRectTransformComponent { AnchorMin = "0 0.8", AnchorMax = "1 1" });

            var btn = new CuiElementNode("ClickBtn", "MainPanel");
            btn.Components.Add(new CuiButtonComponent { Command = "test.click", Color = "0.2 0.8 0.2 1.0", Close = "MainPanel" });
            btn.Components.Add(new CuiRectTransformComponent { AnchorMin = "0.3 0.1", AnchorMax = "0.7 0.3" });

            doc.AddElement(panel);
            doc.AddElement(label);
            doc.AddElement(btn);

            // Serialize to JSON
            string json = CuiJsonSerializer.SerializeDocument(doc, true);
            Assert.IsNotEmpty(json);
            Assert.IsTrue(json.Contains("MainPanel"));
            Assert.IsTrue(json.Contains("HeaderLabel"));
            Assert.IsTrue(json.Contains("ClickBtn"));
            Assert.IsTrue(json.Contains("test.click"));

            // Parse back
            var result = CuiParser.ParseJson(json);
            Assert.IsTrue(result.Success, "Parser should succeed without fatal errors");
            Assert.AreEqual(3, result.Document.Elements.Count);

            var parsedPanel = result.Document.FindByName("MainPanel");
            Assert.IsNotNull(parsedPanel);
            Assert.AreEqual("Overlay", parsedPanel.Parent);
            Assert.IsTrue(parsedPanel.HasComponent<CuiImageComponent>());
            Assert.IsTrue(parsedPanel.HasComponent<CuiRectTransformComponent>());
            Assert.IsTrue(parsedPanel.HasComponent<CuiNeedsCursorComponent>());

            var rect = parsedPanel.GetComponent<CuiRectTransformComponent>();
            Assert.AreEqual("0.1 0.2", rect.AnchorMin);
            Assert.AreEqual("0.9 0.8", rect.AnchorMax);
        }

        [Test]
        public void Test_All21Components_CanBeSerialized()
        {
            var doc = new CuiDocument { ProjectName = "AllComponents" };
            var elem = new CuiElementNode("MegaElement", "Overlay");

            elem.Components.Add(new CuiRectTransformComponent());
            elem.Components.Add(new CuiTextComponent());
            elem.Components.Add(new CuiImageComponent());
            elem.Components.Add(new CuiRawImageComponent());
            elem.Components.Add(new CuiButtonComponent());
            elem.Components.Add(new CuiInputFieldComponent());
            elem.Components.Add(new CuiCountdownComponent());
            elem.Components.Add(new CuiOutlineComponent());
            elem.Components.Add(new CuiScrollViewComponent());
            elem.Components.Add(new CuiCanvasGroupComponent());
            elem.Components.Add(new CuiMaskComponent());
            elem.Components.Add(new CuiNeedsCursorComponent());
            elem.Components.Add(new CuiNeedsKeyboardComponent());
            elem.Components.Add(new CuiHorizontalLayoutGroupComponent());
            elem.Components.Add(new CuiVerticalLayoutGroupComponent());
            elem.Components.Add(new CuiGridLayoutGroupComponent());
            elem.Components.Add(new CuiContentSizeFitterComponent());
            elem.Components.Add(new CuiLayoutElementComponent());
            elem.Components.Add(new CuiTooltipComponent());
            elem.Components.Add(new CuiDraggableComponent());
            elem.Components.Add(new CuiSlotComponent());

            doc.AddElement(elem);

            string json = CuiJsonSerializer.SerializeDocument(doc, false);
            Assert.IsNotEmpty(json);

            var parseResult = CuiParser.ParseJson(json);
            Assert.IsTrue(parseResult.Success);
            var parsedElem = parseResult.Document.FindByName("MegaElement");
            Assert.IsNotNull(parsedElem);
            Assert.GreaterOrEqual(parsedElem.Components.Count, 21);
        }
    }
}
