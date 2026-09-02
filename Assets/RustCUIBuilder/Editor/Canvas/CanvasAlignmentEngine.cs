using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas
{
    /// <summary>
    /// Professional alignment and distribution engine for multi-selected CUI elements.
    /// Operates on actual computed element workspace rectangles.
    /// </summary>
    public static class CanvasAlignmentEngine
    {
        public static void AlignLeft(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count < 2) return;
            var coords = RustCanvasCoordinates.Instance;

            float minX = float.MaxValue;
            foreach (var elem in elements)
            {
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                if (r.xMin < minX) minX = r.xMin;
            }

            foreach (var elem in elements)
            {
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(minX, r.y, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void AlignCenter(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count < 2) return;
            var coords = RustCanvasCoordinates.Instance;

            float avgCenter = elements.Average(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).center.x);

            foreach (var elem in elements)
            {
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(avgCenter - r.width / 2f, r.y, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void AlignRight(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count < 2) return;
            var coords = RustCanvasCoordinates.Instance;

            float maxX = elements.Max(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).xMax);

            foreach (var elem in elements)
            {
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(maxX - r.width, r.y, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void AlignTop(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count < 2) return;
            var coords = RustCanvasCoordinates.Instance;

            float minY = elements.Min(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).yMin);

            foreach (var elem in elements)
            {
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(r.x, minY, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void AlignMiddle(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count < 2) return;
            var coords = RustCanvasCoordinates.Instance;

            float avgCenter = elements.Average(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).center.y);

            foreach (var elem in elements)
            {
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(r.x, avgCenter - r.height / 2f, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void AlignBottom(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count < 2) return;
            var coords = RustCanvasCoordinates.Instance;

            float maxY = elements.Max(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).yMax);

            foreach (var elem in elements)
            {
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(r.x, maxY - r.height, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void DistributeHorizontally(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count < 3) return;
            var coords = RustCanvasCoordinates.Instance;

            var sorted = elements.OrderBy(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).xMin).ToList();
            float minX = coords.GetElementCanvasRect(sorted[0], doc, canvasW, canvasH).xMin;
            float maxX = coords.GetElementCanvasRect(sorted[sorted.Count - 1], doc, canvasW, canvasH).xMax;
            float totalElemWidth = sorted.Sum(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).width);

            float totalSpan = maxX - minX;
            float totalGap = totalSpan - totalElemWidth;
            float gap = totalGap / (sorted.Count - 1);

            float currentX = minX;
            foreach (var elem in sorted)
            {
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(currentX, r.y, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
                currentX += r.width + gap;
            }
        }

        public static void DistributeVertically(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count < 3) return;
            var coords = RustCanvasCoordinates.Instance;

            var sorted = elements.OrderBy(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).yMin).ToList();
            float minY = coords.GetElementCanvasRect(sorted[0], doc, canvasW, canvasH).yMin;
            float maxY = coords.GetElementCanvasRect(sorted[sorted.Count - 1], doc, canvasW, canvasH).yMax;
            float totalElemHeight = sorted.Sum(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).height);

            float totalSpan = maxY - minY;
            float totalGap = totalSpan - totalElemHeight;
            float gap = totalGap / (sorted.Count - 1);

            float currentY = minY;
            foreach (var elem in sorted)
            {
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(r.x, currentY, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
                currentY += r.height + gap;
            }
        }
    }
}
