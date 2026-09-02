using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas.Services
{
    public enum AlignmentTarget
    {
        SelectionBounds,
        Canvas,
        FirstSelected,
        LastSelected
    }

    /// <summary>
    /// Master alignment service for single and multi-selected CUI elements.
    /// Operates accurately across mixed hierarchies, nested parents, and varied element dimensions.
    /// </summary>
    public static class CanvasAlignmentService
    {
        public static void AlignLeft(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH, AlignmentTarget target = AlignmentTarget.SelectionBounds)
        {
            if (elements == null || elements.Count == 0) return;
            var coords = RustCanvasCoordinates.Instance;

            float targetLeft;
            if (target == AlignmentTarget.Canvas)
            {
                targetLeft = 0f;
            }
            else if (target == AlignmentTarget.FirstSelected && elements.Count > 0)
            {
                targetLeft = coords.GetElementCanvasRect(elements[0], doc, canvasW, canvasH).xMin;
            }
            else if (target == AlignmentTarget.LastSelected && elements.Count > 0)
            {
                targetLeft = coords.GetElementCanvasRect(elements[elements.Count - 1], doc, canvasW, canvasH).xMin;
            }
            else
            {
                if (elements.Count < 2) return;
                targetLeft = elements.Min(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).xMin);
            }

            foreach (var elem in elements)
            {
                if (elem.IsLocked) continue;
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(targetLeft, r.y, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void AlignCenterH(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH, AlignmentTarget target = AlignmentTarget.SelectionBounds)
        {
            if (elements == null || elements.Count == 0) return;
            var coords = RustCanvasCoordinates.Instance;

            float targetCenterX;
            if (target == AlignmentTarget.Canvas)
            {
                targetCenterX = canvasW * 0.5f;
            }
            else if (target == AlignmentTarget.FirstSelected && elements.Count > 0)
            {
                targetCenterX = coords.GetElementCanvasRect(elements[0], doc, canvasW, canvasH).center.x;
            }
            else if (target == AlignmentTarget.LastSelected && elements.Count > 0)
            {
                targetCenterX = coords.GetElementCanvasRect(elements[elements.Count - 1], doc, canvasW, canvasH).center.x;
            }
            else
            {
                if (elements.Count < 2) return;
                float minX = elements.Min(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).xMin);
                float maxX = elements.Max(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).xMax);
                targetCenterX = (minX + maxX) * 0.5f;
            }

            foreach (var elem in elements)
            {
                if (elem.IsLocked) continue;
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(targetCenterX - r.width * 0.5f, r.y, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void AlignRight(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH, AlignmentTarget target = AlignmentTarget.SelectionBounds)
        {
            if (elements == null || elements.Count == 0) return;
            var coords = RustCanvasCoordinates.Instance;

            float targetRight;
            if (target == AlignmentTarget.Canvas)
            {
                targetRight = canvasW;
            }
            else if (target == AlignmentTarget.FirstSelected && elements.Count > 0)
            {
                targetRight = coords.GetElementCanvasRect(elements[0], doc, canvasW, canvasH).xMax;
            }
            else if (target == AlignmentTarget.LastSelected && elements.Count > 0)
            {
                targetRight = coords.GetElementCanvasRect(elements[elements.Count - 1], doc, canvasW, canvasH).xMax;
            }
            else
            {
                if (elements.Count < 2) return;
                targetRight = elements.Max(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).xMax);
            }

            foreach (var elem in elements)
            {
                if (elem.IsLocked) continue;
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(targetRight - r.width, r.y, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void AlignTop(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH, AlignmentTarget target = AlignmentTarget.SelectionBounds)
        {
            if (elements == null || elements.Count == 0) return;
            var coords = RustCanvasCoordinates.Instance;

            float targetTop;
            if (target == AlignmentTarget.Canvas)
            {
                targetTop = 0f;
            }
            else if (target == AlignmentTarget.FirstSelected && elements.Count > 0)
            {
                targetTop = coords.GetElementCanvasRect(elements[0], doc, canvasW, canvasH).yMin;
            }
            else if (target == AlignmentTarget.LastSelected && elements.Count > 0)
            {
                targetTop = coords.GetElementCanvasRect(elements[elements.Count - 1], doc, canvasW, canvasH).yMin;
            }
            else
            {
                if (elements.Count < 2) return;
                targetTop = elements.Min(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).yMin);
            }

            foreach (var elem in elements)
            {
                if (elem.IsLocked) continue;
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(r.x, targetTop, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void AlignCenterV(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH, AlignmentTarget target = AlignmentTarget.SelectionBounds)
        {
            if (elements == null || elements.Count == 0) return;
            var coords = RustCanvasCoordinates.Instance;

            float targetCenterY;
            if (target == AlignmentTarget.Canvas)
            {
                targetCenterY = canvasH * 0.5f;
            }
            else if (target == AlignmentTarget.FirstSelected && elements.Count > 0)
            {
                targetCenterY = coords.GetElementCanvasRect(elements[0], doc, canvasW, canvasH).center.y;
            }
            else if (target == AlignmentTarget.LastSelected && elements.Count > 0)
            {
                targetCenterY = coords.GetElementCanvasRect(elements[elements.Count - 1], doc, canvasW, canvasH).center.y;
            }
            else
            {
                if (elements.Count < 2) return;
                float minY = elements.Min(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).yMin);
                float maxY = elements.Max(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).yMax);
                targetCenterY = (minY + maxY) * 0.5f;
            }

            foreach (var elem in elements)
            {
                if (elem.IsLocked) continue;
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(r.x, targetCenterY - r.height * 0.5f, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void AlignBottom(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH, AlignmentTarget target = AlignmentTarget.SelectionBounds)
        {
            if (elements == null || elements.Count == 0) return;
            var coords = RustCanvasCoordinates.Instance;

            float targetBottom;
            if (target == AlignmentTarget.Canvas)
            {
                targetBottom = canvasH;
            }
            else if (target == AlignmentTarget.FirstSelected && elements.Count > 0)
            {
                targetBottom = coords.GetElementCanvasRect(elements[0], doc, canvasW, canvasH).yMax;
            }
            else if (target == AlignmentTarget.LastSelected && elements.Count > 0)
            {
                targetBottom = coords.GetElementCanvasRect(elements[elements.Count - 1], doc, canvasW, canvasH).yMax;
            }
            else
            {
                if (elements.Count < 2) return;
                targetBottom = elements.Max(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).yMax);
            }

            foreach (var elem in elements)
            {
                if (elem.IsLocked) continue;
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(r.x, targetBottom - r.height, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }
    }
}
