using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas.Services
{
    /// <summary>
    /// Master hierarchy manipulation service.
    /// Handles Z-ordering, tree navigation, reparenting, grouping, and ungrouping while preserving absolute canvas coordinates.
    /// </summary>
    public static class CanvasHierarchyService
    {
        public static void BringToFront(List<CuiElementNode> elements, CuiDocument doc)
        {
            if (elements == null || elements.Count == 0 || doc == null) return;
            foreach (var elem in elements)
            {
                int curIdx = doc.Elements.IndexOf(elem);
                if (curIdx >= 0 && curIdx < doc.Elements.Count - 1)
                {
                    doc.Elements.RemoveAt(curIdx);
                    doc.Elements.Add(elem);
                }
            }
            doc.NotifyModified();
        }

        public static void SendToBack(List<CuiElementNode> elements, CuiDocument doc)
        {
            if (elements == null || elements.Count == 0 || doc == null) return;
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                var elem = elements[i];
                int curIdx = doc.Elements.IndexOf(elem);
                if (curIdx > 0)
                {
                    doc.Elements.RemoveAt(curIdx);
                    doc.Elements.Insert(0, elem);
                }
            }
            doc.NotifyModified();
        }

        public static void BringForward(List<CuiElementNode> elements, CuiDocument doc)
        {
            if (elements == null || elements.Count == 0 || doc == null) return;
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                var elem = elements[i];
                int curIdx = doc.Elements.IndexOf(elem);
                if (curIdx >= 0 && curIdx < doc.Elements.Count - 1)
                {
                    doc.Elements.RemoveAt(curIdx);
                    doc.Elements.Insert(curIdx + 1, elem);
                }
            }
            doc.NotifyModified();
        }

        public static void SendBackward(List<CuiElementNode> elements, CuiDocument doc)
        {
            if (elements == null || elements.Count == 0 || doc == null) return;
            foreach (var elem in elements)
            {
                int curIdx = doc.Elements.IndexOf(elem);
                if (curIdx > 0)
                {
                    doc.Elements.RemoveAt(curIdx);
                    doc.Elements.Insert(curIdx - 1, elem);
                }
            }
            doc.NotifyModified();
        }

        public static void MoveUp(List<CuiElementNode> elements, CuiDocument doc)
        {
            SendBackward(elements, doc);
        }

        public static void MoveDown(List<CuiElementNode> elements, CuiDocument doc)
        {
            BringForward(elements, doc);
        }

        public static void MoveIntoParent(List<CuiElementNode> elements, string newParentName, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count == 0 || doc == null || string.IsNullOrEmpty(newParentName)) return;
            var coords = RustCanvasCoordinates.Instance;

            foreach (var elem in elements)
            {
                if (elem.Name == newParentName) continue; // Cannot parent to self
                var oldCanvasRect = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                elem.Parent = newParentName;
                coords.ApplyNewCanvasRectToElementOffsets(oldCanvasRect, elem, doc, canvasW, canvasH);
            }
            doc.NotifyModified();
        }

        public static void MoveOutOfParent(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count == 0 || doc == null) return;
            var coords = RustCanvasCoordinates.Instance;

            foreach (var elem in elements)
            {
                var parentElem = doc.FindElementByName(elem.Parent);
                string grandParent = parentElem != null ? parentElem.Parent : "Overlay";
                var oldCanvasRect = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                elem.Parent = grandParent;
                coords.ApplyNewCanvasRectToElementOffsets(oldCanvasRect, elem, doc, canvasW, canvasH);
            }
            doc.NotifyModified();
        }

        public static CuiElementNode GroupSelection(CuiDocument doc, float canvasW, float canvasH)
        {
            if (doc == null || doc.SelectedElements.Count < 2) return null;
            var coords = RustCanvasCoordinates.Instance;

            var selected = doc.SelectedElements.ToList();

            // 1. Calculate composite bounding box in canvas workspace
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var elem in selected)
            {
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                if (r.xMin < minX) minX = r.xMin;
                if (r.yMin < minY) minY = r.yMin;
                if (r.xMax > maxX) maxX = r.xMax;
                if (r.yMax > maxY) maxY = r.yMax;
            }

            var groupCanvasRect = new Rect(minX, minY, maxX - minX, maxY - minY);

            // Determine parent layer/element of group (use parent of first element)
            string groupParent = selected[0].Parent;

            // 2. Create Group Container Element
            string groupName = "Group_" + Guid.NewGuid().ToString("N").Substring(0, 4);
            var groupElem = new CuiElementNode(groupName, groupParent);
            groupElem.Components.Add(new CuiRectTransformComponent());
            coords.ApplyNewCanvasRectToElementOffsets(groupCanvasRect, groupElem, doc, canvasW, canvasH);

            doc.AddElement(groupElem);

            // 3. Reparent selected elements to new group while preserving absolute canvas positions
            foreach (var elem in selected)
            {
                var oldRect = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                elem.Parent = groupName;
                coords.ApplyNewCanvasRectToElementOffsets(oldRect, elem, doc, canvasW, canvasH);
            }

            doc.Select(groupElem.Id);
            doc.NotifyModified();
            return groupElem;
        }

        public static void UngroupSelection(CuiDocument doc, float canvasW, float canvasH)
        {
            if (doc == null || doc.SelectedElements.Count == 0) return;
            var coords = RustCanvasCoordinates.Instance;

            var toUngroup = doc.SelectedElements.ToList();
            var newSelection = new List<string>();

            foreach (var groupElem in toUngroup)
            {
                var children = doc.GetChildrenOf(groupElem.Name).ToList();
                if (children.Count == 0) continue;

                string grandParent = groupElem.Parent;

                foreach (var child in children)
                {
                    var oldRect = coords.GetElementCanvasRect(child, doc, canvasW, canvasH);
                    child.Parent = grandParent;
                    coords.ApplyNewCanvasRectToElementOffsets(oldRect, child, doc, canvasW, canvasH);
                    newSelection.Add(child.Id);
                }

                doc.RemoveElement(groupElem.Id);
            }

            doc.ClearSelection();
            foreach (var id in newSelection) doc.Select(id, true);
            doc.NotifyModified();
        }
    }
}
