using NUnit.Framework;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Core.Serialization;
using UnityEngine;

namespace RustCUIBuilder.Tests.Editor
{
    [TestFixture]
    public class CuiCodeGeneratorTests
    {
        [Test]
        public void Test_GeneratePluginCode_ContainsExpectedStructures()
        {
            var doc = new CuiDocument { ProjectName = "CodeGenTest" };

            var panel = new CuiElementNode("MyPanel", "Overlay");
            panel.Components.Add(new CuiImageComponent { Color = "0.1 0.1 0.1 0.8" });
            panel.Components.Add(new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" });
            panel.Components.Add(new CuiNeedsCursorComponent());

            var btn = new CuiElementNode("MyBtn", "MyPanel");
            btn.Components.Add(new CuiButtonComponent { Command = "plugin.action", Color = "0.2 0.8 0.2 1" });
            btn.Components.Add(new CuiTextComponent { Text = "Click Me", FontSize = 14 });
            btn.Components.Add(new CuiRectTransformComponent { AnchorMin = "0.4 0.4", AnchorMax = "0.6 0.6" });

            doc.AddElement(panel);
            doc.AddElement(btn);

            string code = CuiCodeGenerator.GeneratePluginCode(doc);

            Assert.IsNotEmpty(code);
            Assert.IsTrue(code.Contains("CuiElementContainer"));
            Assert.IsTrue(code.Contains("CuiHelper.AddUi"));
            Assert.IsTrue(code.Contains("CuiHelper.DestroyUi"));
            Assert.IsTrue(code.Contains("CuiPanel"));
            Assert.IsTrue(code.Contains("CuiButton"));
            Assert.IsTrue(code.Contains("plugin.action"));
            Assert.IsTrue(code.Contains("Click Me"));
        }
    }
}
