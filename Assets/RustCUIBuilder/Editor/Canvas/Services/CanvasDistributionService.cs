using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas.Services
{
    /// <summary>
    /// Master distribution and spacing service.
    /// Computes accurate gaps between outer element bounds, enforces equal spacing, and matches dimensions.
    /// </summary>
    public static class CanvasDistributionService
    {
        public static void DistributeHorizontally(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count < 3) return;
            var coords = RustCanvasCoordinates.Instance;

            var sorted = elements.OrderBy(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).center.x).ToList();
            float minCenter = coords.GetElementCanvasRect(sorted[0], doc, canvasW, canvasH).center.x;
            float maxCenter = coords.GetElementCanvasRect(sorted[sorted.Count - 1], doc, canvasW, canvasH).center.x;

            float step = (maxCenter - minCenter) / (sorted.Count - 1);

            for (int i = 1; i < sorted.Count - 1; i++)
            {
                var elem = sorted[i];
                if (elem.IsLocked) continue;
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                float newCenterX = minCenter + i * step;
                var newR = new Rect(newCenterX - r.width * 0.5f, r.y, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void DistributeVertically(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count < 3) return;
            var coords = RustCanvasCoordinates.Instance;

            var sorted = elements.OrderBy(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).center.y).ToList();
            float minCenter = coords.GetElementCanvasRect(sorted[0], doc, canvasW, canvasH).center.y;
            float maxCenter = coords.GetElementCanvasRect(sorted[sorted.Count - 1], doc, canvasW, canvasH).center.y;

            float step = (maxCenter - minCenter) / (sorted.Count - 1);

            for (int i = 1; i < sorted.Count - 1; i++)
            {
                var elem = sorted[i];
                if (elem.IsLocked) continue;
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                float newCenterY = minCenter + i * step;
                var newR = new Rect(r.x, newCenterY - r.height * 0.5f, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void EqualHorizontalSpacing(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
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
                if (!elem.IsLocked)
                {
                    var newR = new Rect(currentX, r.y, r.width, r.height);
                    coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
                }
                currentX += r.width + gap;
            }
        }

        public static void EqualVerticalSpacing(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
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
                if (!elem.IsLocked)
                {
                    var newR = new Rect(r.x, currentY, r.width, r.height);
                    coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
                }
                currentY += r.height + gap;
            }
        }

        public static void SetHorizontalGap(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH, float gap)
        {
            if (elements == null || elements.Count < 2) return;
            var coords = RustCanvasCoordinates.Instance;

            var sorted = elements.OrderBy(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).xMin).ToList();
            float currentX = coords.GetElementCanvasRect(sorted[0], doc, canvasW, canvasH).xMin;

            foreach (var elem in sorted)
            {
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                if (!elem.IsLocked)
                {
                    var newR = new Rect(currentX, r.y, r.width, r.height);
                    coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
                }
                currentX += r.width + gap;
            }
        }

        public static void SetVerticalGap(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH, float gap)
        {
            if (elements == null || elements.Count < 2) return;
            var coords = RustCanvasCoordinates.Instance;

            var sorted = elements.OrderBy(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).yMin).ToList();
            float currentY = coords.GetElementCanvasRect(sorted[0], doc, canvasW, canvasH).yMin;

            foreach (var elem in sorted)
            {
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                if (!elem.IsLocked)
                {
                    var newR = new Rect(r.x, currentY, r.width, r.height);
                    coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
                }
                currentY += r.height + gap;
            }
        }

        public static void MakeSameWidth(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count < 2) return;
            var coords = RustCanvasCoordinates.Instance;

            // Target width is the primary selected element or the first element
            var primary = doc.PrimarySelectedElement ?? elements[0];
            float targetW = coords.GetElementCanvasRect(primary, doc, canvasW, canvasH).width;

            foreach (var elem in elements)
            {
                if (elem == primary || elem.IsLocked) continue;
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(r.x, r.y, targetW, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void MakeSameHeight(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count < 2) return;
            var coords = RustCanvasCoordinates.Instance;

            var primary = doc.PrimarySelectedElement ?? elements[0];
            float targetH = coords.GetElementCanvasRect(primary, doc, canvasW, canvasH).height;

            foreach (var elem in elements)
            {
                if (elem == primary || elem.IsLocked) continue;
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(r.x, r.y, r.width, targetH);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void MakeSameSize(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            MakeSameWidth(elements, doc, canvasW, canvasH);
            MakeSameHeight(elements, doc, canvasW, canvasH);
        }
    }
}
