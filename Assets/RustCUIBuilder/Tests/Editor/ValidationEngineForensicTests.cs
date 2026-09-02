using System.Linq;
using NUnit.Framework;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Core.Validation;

namespace RustCUIBuilder.Tests.Editor
{
    public class ValidationEngineForensicTests
    {
        [Test]
        public void Validator_DetectsDuplicateElementNames()
        {
            var doc = new CuiDocument();
            doc.AddElement(new CuiElementNode { Name = "DuplicateName", Parent = "Overlay" });
            doc.AddElement(new CuiElementNode { Name = "DuplicateName", Parent = "Overlay" });

            var report = CuiValidator.ValidateDocument(doc);
            Assert.IsFalse(report.IsValid);
            Assert.IsTrue(report.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error && d.RuleId == "STRUCT_DUPLICATE_NAME"));
        }

        [Test]
        public void Validator_DetectsMissingParent()
        {
            var doc = new CuiDocument();
            doc.AddElement(new CuiElementNode { Name = "OrphanElement", Parent = "NonExistentParent" });

            var report = CuiValidator.ValidateDocument(doc);
            Assert.IsFalse(report.IsValid);
            Assert.IsTrue(report.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error && d.RuleId == "HIERARCHY_ORPHAN"));
        }

        [Test]
        public void Validator_DetectsInvalidAnchorValues()
        {
            var doc = new CuiDocument();
            var elem = new CuiElementNode { Name = "BadAnchorElement", Parent = "Overlay" };
            elem.Components.Add(new CuiRectTransformComponent { AnchorMin = "invalid_anchor_text", AnchorMax = "1 1" });
            doc.AddElement(elem);

            var report = CuiValidator.ValidateDocument(doc);
            Assert.IsFalse(report.IsValid);
            Assert.IsTrue(report.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error && d.RuleId == "RECT_INVALID_ANCHOR_MIN"));
        }

        [Test]
        public void Validator_DetectsInvertedAnchors()
        {
            var doc = new CuiDocument();
            var elem = new CuiElementNode { Name = "InvertedAnchorElement", Parent = "Overlay" };
            elem.Components.Add(new CuiRectTransformComponent { AnchorMin = "0.8 0.8", AnchorMax = "0.2 0.2" });
            doc.AddElement(elem);

            var report = CuiValidator.ValidateDocument(doc);
            Assert.IsFalse(report.IsValid);
            Assert.IsTrue(report.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error && d.RuleId == "RECT_INVERTED_ANCHORS"));
        }

        [Test]
        public void Validator_DetectsInvalidColorStrings()
        {
            var doc = new CuiDocument();
            var elem = new CuiElementNode { Name = "BadColorElement", Parent = "Overlay" };
            elem.Components.Add(new CuiImageComponent { Color = "not_a_valid_color" });
            doc.AddElement(elem);

            var report = CuiValidator.ValidateDocument(doc);
            Assert.IsFalse(report.IsValid);
            Assert.IsTrue(report.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning && d.RuleId == "COLOR_INVALID_FORMAT"));
        }
    }
}
