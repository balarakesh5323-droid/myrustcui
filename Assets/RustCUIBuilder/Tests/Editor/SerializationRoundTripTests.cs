using NUnit.Framework;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Core.Serialization;

namespace RustCUIBuilder.Tests.Editor
{
    public class SerializationRoundTripTests
    {
        [Test]
        public void RoundTrip_All21Components_PreservesIntegrity()
        {
            var doc = new CuiDocument();
            var elem = new CuiElementNode { Name = "AllCompElement", Parent = "Overlay" };

            elem.Components.Add(new CuiRectTransformComponent { AnchorMin = "0.1 0.2", AnchorMax = "0.8 0.9", OffsetMin = "10 20", OffsetMax = "-10 -20" });
            elem.Components.Add(new CuiTextComponent { Text = "RoundTrip Label", FontSize = 18, Color = "1 1 0 1", Align = UnityEngine.TextAnchor.MiddleCenter });
            elem.Components.Add(new CuiImageComponent { Sprite = "assets/content/ui/ui.rounded.psd", Color = "0.2 0.3 0.4 0.9", ItemId = 12345 });
            elem.Components.Add(new CuiRawImageComponent { Url = "https://test.com/img.png", Color = "1 1 1 0.8" });
            elem.Components.Add(new CuiButtonComponent { Command = "test.cmd", Close = "AllCompElement", Color = "0 1 0 1" });
            elem.Components.Add(new CuiInputFieldComponent { Text = "Input", Command = "input.submit", CharsLimit = 64, IsPassword = true });
            elem.Components.Add(new CuiCountdownComponent { StartTime = 60, EndTime = 0, Step = 1, TimerFormat = CuiTimerFormat.MinutesSeconds });
            elem.Components.Add(new CuiOutlineComponent { Color = "0 0 0 1", Distance = "2 -2" });
            elem.Components.Add(new CuiScrollViewComponent { Horizontal = true, Vertical = true, MovementType = UnityEngine.UI.ScrollRect.MovementType.Clamped });
            elem.Components.Add(new CuiCanvasGroupComponent { Alpha = 0.85f, BlocksRaycasts = true, Interactable = false });
            elem.Components.Add(new CuiNeedsCursorComponent());
            elem.Components.Add(new CuiNeedsKeyboardComponent());
            elem.Components.Add(new CuiTooltipComponent { Text = "Hover Tooltip", TooltipType = CuiTooltipType.AlwaysOnTop });
            elem.Components.Add(new CuiDraggableComponent { DragAlpha = 0.5f, DropAnywhere = true });
            elem.Components.Add(new CuiSlotComponent { Filter = "weapon" });
            elem.Components.Add(new CuiMaskComponent { ShowMaskGraphic = false });
            elem.Components.Add(new CuiHorizontalLayoutGroupComponent { Spacing = 8f, ChildAlignment = UnityEngine.TextAnchor.MiddleLeft });
            elem.Components.Add(new CuiVerticalLayoutGroupComponent { Spacing = 4f, ChildAlignment = UnityEngine.TextAnchor.UpperCenter });
            elem.Components.Add(new CuiGridLayoutGroupComponent { CellSize = "64 64", Spacing = "4 4", ConstraintCount = 4 });
            elem.Components.Add(new CuiLayoutElementComponent { PreferredWidth = 200f, PreferredHeight = 50f });
            elem.Components.Add(new CuiContentSizeFitterComponent { HorizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize });

            doc.AddElement(elem);

            // Step 1: Model -> JSON
            string json1 = CuiJsonSerializer.SerializeDocument(doc, true);

            // Step 2: JSON -> Model
            var parseResult1 = CuiParser.ParseJson(json1);
            Assert.IsTrue(parseResult1.Success, "First parse failed: " + string.Join("; ", parseResult1.Errors));

            // Step 3: Model -> JSON
            string json2 = CuiJsonSerializer.SerializeDocument(parseResult1.Document, true);

            // Step 4: JSON -> Model
            var parseResult2 = CuiParser.ParseJson(json2);
            Assert.IsTrue(parseResult2.Success, "Second parse failed: " + string.Join("; ", parseResult2.Errors));

            // Verify element equality
            var parsedElem = parseResult2.Document.Elements[0];
            Assert.AreEqual("AllCompElement", parsedElem.Name);
            Assert.AreEqual("Overlay", parsedElem.Parent);
            Assert.AreEqual(21, parsedElem.Components.Count);
        }
    }
}
