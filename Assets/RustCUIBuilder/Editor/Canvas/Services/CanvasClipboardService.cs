using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas.Services
{
    /// <summary>
    /// Master clipboard and duplication service.
    /// Handles recursive subtree cloning, unique name generation, and smart coordinate pasting.
    /// </summary>
    public static class CanvasClipboardService
    {
        private static readonly List<CuiElementNode> _clipboard = new List<CuiElementNode>();

        public static bool HasClipboardData => _clipboard.Count > 0;

        public static void Copy(List<CuiElementNode> elements, CuiDocument doc)
        {
            if (elements == null || elements.Count == 0 || doc == null) return;
            _clipboard.Clear();

            var toCopy = new HashSet<string>(elements.Select(e => e.Id));
            // Include children recursively
            foreach (var elem in elements)
            {
                CollectSubtree(elem, doc, toCopy);
            }

            foreach (var elem in doc.Elements.Where(e => toCopy.Contains(e.Id)))
            {
                _clipboard.Add(elem.Clone(false, elem.Name));
            }
        }

        public static void Cut(List<CuiElementNode> elements, CuiDocument doc)
        {
            Copy(elements, doc);
            if (elements == null || doc == null) return;
            foreach (var elem in elements)
            {
                doc.RemoveElement(elem.Id);
            }
            doc.NotifyModified();
        }

        public static List<CuiElementNode> Paste(CuiDocument doc, float canvasW, float canvasH, Vector2? pasteCanvasPos = null)
        {
            if (_clipboard.Count == 0 || doc == null) return new List<CuiElementNode>();
            var coords = RustCanvasCoordinates.Instance;

            var oldToNewNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var pastedElements = new List<CuiElementNode>();

            // Generate unique names for top-level and children
            foreach (var original in _clipboard)
            {
                string newName = $"{original.Name}_Copy";
                // Ensure unique name in doc
                int count = 1;
                while (doc.FindElementByName(newName) != null || oldToNewNames.ContainsValue(newName))
                {
                    newName = $"{original.Name}_Copy{count++}";
                }
                oldToNewNames[original.Name] = newName;
            }

            // Map old parent references and add cloned elements
            foreach (var original in _clipboard)
            {
                var clone = original.Clone(true, oldToNewNames[original.Name]);

                if (oldToNewNames.TryGetValue(original.Parent, out var newParent))
                {
                    clone.Parent = newParent;
                }

                // If this is a top-level pasted element, offset slightly (+20px, +20px) or place at pasteCanvasPos
                if (!oldToNewNames.ContainsKey(original.Parent))
                {
                    var oldRect = coords.GetElementCanvasRect(original, doc, canvasW, canvasH);
                    Vector2 offset = pasteCanvasPos.HasValue
                        ? (pasteCanvasPos.Value - oldRect.min)
                        : new Vector2(20f, 20f);

                    var newRect = new Rect(oldRect.x + offset.x, oldRect.y + offset.y, oldRect.width, oldRect.height);
                    coords.ApplyNewCanvasRectToElementOffsets(newRect, clone, doc, canvasW, canvasH);
                }

                doc.AddElement(clone);
                pastedElements.Add(clone);
            }

            // Select top-level pasted items
            doc.ClearSelection();
            foreach (var elem in pastedElements.Where(e => !oldToNewNames.ContainsKey(e.Parent)))
            {
                doc.Select(elem.Id, true);
            }

            doc.NotifyModified();
            return pastedElements;
        }

        public static List<CuiElementNode> Duplicate(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            Copy(elements, doc);
            return Paste(doc, canvasW, canvasH);
        }

        private static void CollectSubtree(CuiElementNode parent, CuiDocument doc, HashSet<string> collectedIds)
        {
            var children = doc.GetChildrenOf(parent.Name);
            foreach (var child in children)
            {
                if (collectedIds.Add(child.Id))
                {
                    CollectSubtree(child, doc, collectedIds);
                }
            }
        }
    }
}
