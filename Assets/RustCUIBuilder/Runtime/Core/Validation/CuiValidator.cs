using System;
using System.Collections.Generic;
using System.Linq;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Discovery;
using UnityEngine;

namespace RustCUIBuilder.Runtime.Core.Validation
{
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    public class CuiDiagnostic
    {
        public DiagnosticSeverity Severity { get; set; }
        public string ElementId { get; set; }
        public string ElementName { get; set; }
        public string Message { get; set; }
        public string RuleId { get; set; }
    }

    public class CuiValidationReport
    {
        public bool IsValid => !Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
        public List<CuiDiagnostic> Diagnostics { get; set; } = new List<CuiDiagnostic>();

        public int ErrorCount => Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
        public int WarningCount => Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
        public int InfoCount => Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Info);
    }

    /// <summary>
    /// Static validation engine checking structural hierarchy, anchor geometry,
    /// Rust CUI compatibility rules, and component constraints.
    /// </summary>
    public static class CuiValidator
    {
        private static readonly HashSet<string> ValidRootLayers = new HashSet<string>(RustAssetDiscovery.VerifiedLayers, StringComparer.OrdinalIgnoreCase);

        public static CuiValidationReport ValidateDocument(CuiDocument doc)
        {
            var report = new CuiValidationReport();
            if (doc == null || doc.Elements == null)
            {
                report.Diagnostics.Add(new CuiDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = "Document or elements list is null.",
                    RuleId = "STRUCT_NULL_DOC"
                });
                return report;
            }

            var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var allNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var elem in doc.Elements)
            {
                if (string.IsNullOrWhiteSpace(elem.Name))
                {
                    report.Diagnostics.Add(new CuiDiagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        ElementId = elem.Id,
                        ElementName = "<unnamed>",
                        Message = "Element has an empty or whitespace name.",
                        RuleId = "STRUCT_EMPTY_NAME"
                    });
                }
                else
                {
                    allNames.Add(elem.Name);
                    nameCounts[elem.Name] = nameCounts.TryGetValue(elem.Name, out int c) ? c + 1 : 1;
                }
            }

            // Check duplicate names
            foreach (var kvp in nameCounts)
            {
                if (kvp.Value > 1)
                {
                    report.Diagnostics.Add(new CuiDiagnostic
                    {
                        Severity = DiagnosticSeverity.Error,
                        ElementName = kvp.Key,
                        Message = $"Duplicate element name '{kvp.Key}' used {kvp.Value} times. Element names must be unique.",
                        RuleId = "STRUCT_DUPLICATE_NAME"
                    });
                }
            }

            // Validate each element
            foreach (var elem in doc.Elements)
            {
                ValidateElement(elem, allNames, report);
            }

            return report;
        }

        private static void ValidateElement(CuiElementNode elem, HashSet<string> allNames, CuiValidationReport report)
        {
            // 1. Parent validation
            if (string.IsNullOrWhiteSpace(elem.Parent))
            {
                report.Diagnostics.Add(new CuiDiagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    ElementId = elem.Id,
                    ElementName = elem.Name,
                    Message = "Element has no parent specified. Defaulting to 'Overlay' layer.",
                    RuleId = "STRUCT_NO_PARENT"
                });
            }
            else if (!ValidRootLayers.Contains(elem.Parent) && !allNames.Contains(elem.Parent))
            {
                report.Diagnostics.Add(new CuiDiagnostic
                {
                    Severity = DiagnosticSeverity.Warning,
                    ElementId = elem.Id,
                    ElementName = elem.Name,
                    Message = $"Parent '{elem.Parent}' is neither a recognized root layer (Overlay, Hud, etc.) nor an existing element in this document.",
                    RuleId = "STRUCT_ORPHAN_PARENT"
                });
            }

            // 2. Component validation
            var rect = elem.GetComponent<CuiRectTransformComponent>();
            if (rect == null)
            {
                report.Diagnostics.Add(new CuiDiagnostic
                {
                    Severity = DiagnosticSeverity.Error,
                    ElementId = elem.Id,
                    ElementName = elem.Name,
                    Message = "Element is missing a required RectTransform component.",
                    RuleId = "LAYOUT_MISSING_RECT"
                });
            }
            else
            {
                ValidateRectTransform(elem, rect, report);
            }

            // Button validation
            var btn = elem.GetComponent<CuiButtonComponent>();
            if (btn != null)
            {
                if (string.IsNullOrEmpty(btn.Command) && string.IsNullOrEmpty(btn.Close))
                {
                    report.Diagnostics.Add(new CuiDiagnostic
                    {
                        Severity = DiagnosticSeverity.Warning,
                        ElementId = elem.Id,
                        ElementName = elem.Name,
                        Message = "Button has neither a 'command' to execute nor a 'close' target panel.",
                        RuleId = "INTERACT_NO_BUTTON_ACTION"
                    });
                }
            }

            // Font validation
            var txt = elem.GetComponent<CuiTextComponent>();
            if (txt != null && !string.IsNullOrEmpty(txt.Font))
            {
                if (!RustAssetDiscovery.VerifiedFonts.Contains(txt.Font))
                {
                    report.Diagnostics.Add(new CuiDiagnostic
                    {
                        Severity = DiagnosticSeverity.Warning,
                        ElementId = elem.Id,
                        ElementName = elem.Name,
                        Message = $"Font '{txt.Font}' is not one of Rust's standard built-in fonts (RobotoCondensed-Bold.ttf, RobotoCondensed-Regular.ttf, DroidSansMono.ttf, PermanentMarker.ttf).",
                        RuleId = "COMPAT_UNKNOWN_FONT"
                    });
                }
            }
        }

        private static void ValidateRectTransform(CuiElementNode elem, CuiRectTransformComponent rect, CuiValidationReport report)
        {
            var minParts = rect.AnchorMin.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            var maxParts = rect.AnchorMax.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            if (minParts.Length >= 2 && maxParts.Length >= 2)
            {
                if (float.TryParse(minParts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float minX) &&
                    float.TryParse(maxParts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float maxX))
                {
                    if (minX > maxX)
                    {
                        report.Diagnostics.Add(new CuiDiagnostic
                        {
                            Severity = DiagnosticSeverity.Error,
                            ElementId = elem.Id,
                            ElementName = elem.Name,
                            Message = $"AnchorMin.X ({minX}) cannot be greater than AnchorMax.X ({maxX}).",
                            RuleId = "LAYOUT_INVALID_ANCHOR_X"
                        });
                    }
                }

                if (float.TryParse(minParts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float minY) &&
                    float.TryParse(maxParts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float maxY))
                {
                    if (minY > maxY)
                    {
                        report.Diagnostics.Add(new CuiDiagnostic
                        {
                            Severity = DiagnosticSeverity.Error,
                            ElementId = elem.Id,
                            ElementName = elem.Name,
                            Message = $"AnchorMin.Y ({minY}) cannot be greater than AnchorMax.Y ({maxY}).",
                            RuleId = "LAYOUT_INVALID_ANCHOR_Y"
                        });
                    }
                }
            }
        }
    }
}
