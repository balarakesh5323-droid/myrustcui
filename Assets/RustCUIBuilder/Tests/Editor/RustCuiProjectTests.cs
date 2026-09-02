using NUnit.Framework;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Core.Project;
using RustCUIBuilder.Runtime.Core.Serialization;

namespace RustCUIBuilder.Tests.Editor
{
    public class RustCuiProjectTests
    {
        [Test]
        public void Project_SaveAndLoad_MaintainsHierarchyAndSnapshots()
        {
            var project = new RustCuiProject
            {
                ProjectName = "ShopUI",
                Author = "RustAdmin",
                Description = "High performance server shop"
            };

            var doc = new CuiDocument();
            var panel = new CuiElementNode { Name = "MainPanel", Parent = "Overlay" };
            panel.Components.Add(new CuiRectTransformComponent { AnchorMin = "0.1 0.1", AnchorMax = "0.9 0.9" });
            panel.Components.Add(new CuiImageComponent { Color = "0.2 0.2 0.2 1" });
            doc.AddElement(panel);

            project.FromDocument(doc);
            project.Snapshots.Add(new RustCuiProject.ProjectSnapshot
            {
                Name = "v1.0 Release",
                Description = "Initial store layout",
                CuiJson = CuiJsonSerializer.SerializeDocument(doc, false)
            });

            // Convert back to document
            var loadedDoc = project.ToDocument();

            Assert.AreEqual(1, loadedDoc.Elements.Count);
            Assert.AreEqual("MainPanel", loadedDoc.Elements[0].Name);
            Assert.AreEqual("Overlay", loadedDoc.Elements[0].Parent);
            Assert.AreEqual(1, project.Snapshots.Count);
            Assert.AreEqual("v1.0 Release", project.Snapshots[0].Name);
        }

        [Test]
        public void ImageLibraryHelper_GeneratesValidHooks()
        {
            var doc = new CuiDocument();
            var elem = new CuiElementNode { Name = "WebIcon", Parent = "Overlay" };
            elem.Components.Add(new CuiRawImageComponent { Url = "https://example.com/icon.png" });
            doc.AddElement(elem);

            string code = ImageLibraryHelper.GenerateImageLibraryHook(doc, "ShopPlugin");

            Assert.IsTrue(code.Contains("ImageLibrary.Call(\"AddImage\""));
            Assert.IsTrue(code.Contains("https://example.com/icon.png"));
            Assert.IsTrue(code.Contains("WebIcon_icon"));
        }
    }
}
