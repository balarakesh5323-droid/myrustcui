using System.IO;
using NUnit.Framework;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Core.Serialization;

namespace RustCUIBuilder.Tests.Editor
{
    public class ImportValidationTests
    {
        [TestCase("panel.json", 1)]
        [TestCase("button.json", 1)]
        [TestCase("nested-ui.json", 3)]
        [TestCase("image.json", 1)]
        [TestCase("scrollview.json", 1)]
        [TestCase("complex-ui.json", 4)]
        public void Import_Fixtures_ParsesSuccessfully(string fixtureFileName, int expectedElementCount)
        {
            string path = Path.Combine("Assets/RustCUIBuilder/Tests/Fixtures", fixtureFileName);
            Assert.IsTrue(File.Exists(path), $"Fixture file not found: {path}");

            string json = File.ReadAllText(path);
            var result = CuiParser.ParseJson(json);

            Assert.IsTrue(result.Success, $"Failed to parse {fixtureFileName}: " + string.Join("; ", result.Errors));
            Assert.IsNotNull(result.Document);
            Assert.AreEqual(expectedElementCount, result.Document.Elements.Count, $"Element count mismatch for {fixtureFileName}");
        }
    }
}
