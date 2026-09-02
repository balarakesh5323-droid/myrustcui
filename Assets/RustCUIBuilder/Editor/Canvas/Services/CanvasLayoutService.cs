using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas.Services
{
    /// <summary>
    /// Master layout and parent-relative alignment service.
    /// Implements Figma/Unity-style parent anchoring, padding, stretching, column systems, and grid layouts.
    /// </summary>
    public static class CanvasLayoutService
    {
        public static void CenterInParent(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count == 0 || doc == null) return;
            var coords = RustCanvasCoordinates.Instance;

            foreach (var elem in elements)
            {
                if (elem.IsLocked) continue;
                var pRect = coords.GetParentCanvasRect(elem, doc, canvasW, canvasH);
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);

                float newX = pRect.center.x - r.width * 0.5f;
                float newY = pRect.center.y - r.height * 0.5f;
                var newR = new Rect(newX, newY, r.width, r.height);

                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void FillParent(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count == 0) return;
            foreach (var elem in elements)
            {
                if (elem.IsLocked) continue;
                var rect = elem.GetComponent<CuiRectTransformComponent>() ?? elem.GetOrCreateComponent<CuiRectTransformComponent>();
                rect.AnchorMin = "0.0 0.0";
                rect.AnchorMax = "1.0 1.0";
                rect.OffsetMin = "0.0 0.0";
                rect.OffsetMax = "0.0 0.0";
            }
            doc?.NotifyModified();
        }

        public static void StretchH(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count == 0 || doc == null) return;
            var coords = RustCanvasCoordinates.Instance;

            foreach (var elem in elements)
            {
                if (elem.IsLocked) continue;
                var pRect = coords.GetParentCanvasRect(elem, doc, canvasW, canvasH);
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);

                var newR = new Rect(pRect.xMin, r.y, pRect.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void StretchV(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            if (elements == null || elements.Count == 0 || doc == null) return;
            var coords = RustCanvasCoordinates.Instance;

            foreach (var elem in elements)
            {
                if (elem.IsLocked) continue;
                var pRect = coords.GetParentCanvasRect(elem, doc, canvasW, canvasH);
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);

                var newR = new Rect(r.x, pRect.yMin, r.width, pRect.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void StretchBoth(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            FillParent(elements, doc, canvasW, canvasH);
        }

        public static void MatchParentWidth(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            StretchH(elements, doc, canvasW, canvasH);
        }

        public static void MatchParentHeight(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH)
        {
            StretchV(elements, doc, canvasW, canvasH);
        }

        public static void ApplyPadding(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH, float left, float top, float right, float bottom)
        {
            if (elements == null || elements.Count == 0 || doc == null) return;
            var coords = RustCanvasCoordinates.Instance;

            foreach (var elem in elements)
            {
                if (elem.IsLocked) continue;
                var pRect = coords.GetParentCanvasRect(elem, doc, canvasW, canvasH);
                float newW = Mathf.Max(10f, pRect.width - left - right);
                float newH = Mathf.Max(10f, pRect.height - top - bottom);
                var newR = new Rect(pRect.xMin + left, pRect.yMin + top, newW, newH);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void ApplyTwoColumnLayout(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH, float gap = 16f)
        {
            if (elements == null || elements.Count < 2 || doc == null) return;
            var coords = RustCanvasCoordinates.Instance;

            var pRect = coords.GetParentCanvasRect(elements[0], doc, canvasW, canvasH);
            float colW = (pRect.width - gap) * 0.5f;

            for (int i = 0; i < 2 && i < elements.Count; i++)
            {
                var elem = elements[i];
                if (elem.IsLocked) continue;
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                float newX = pRect.xMin + i * (colW + gap);
                var newR = new Rect(newX, r.y, colW, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void ApplyThreeColumnLayout(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH, float gap = 16f)
        {
            if (elements == null || elements.Count < 3 || doc == null) return;
            var coords = RustCanvasCoordinates.Instance;

            var pRect = coords.GetParentCanvasRect(elements[0], doc, canvasW, canvasH);
            float colW = (pRect.width - gap * 2f) / 3f;

            for (int i = 0; i < 3 && i < elements.Count; i++)
            {
                var elem = elements[i];
                if (elem.IsLocked) continue;
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                float newX = pRect.xMin + i * (colW + gap);
                var newR = new Rect(newX, r.y, colW, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void ApplyCardGridLayout(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH, int columns = 3, float gap = 12f)
        {
            if (elements == null || elements.Count == 0 || doc == null) return;
            var coords = RustCanvasCoordinates.Instance;

            var pRect = coords.GetParentCanvasRect(elements[0], doc, canvasW, canvasH);
            columns = Mathf.Max(1, columns);
            float cardW = (pRect.width - (columns - 1) * gap) / columns;
            float cardH = cardW * 0.75f; // Standard 4:3 card aspect

            for (int i = 0; i < elements.Count; i++)
            {
                var elem = elements[i];
                if (elem.IsLocked) continue;
                int row = i / columns;
                int col = i % columns;

                float newX = pRect.xMin + col * (cardW + gap);
                float newY = pRect.yMin + row * (cardH + gap);
                var newR = new Rect(newX, newY, cardW, cardH);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }
        }

        public static void ApplyVerticalStack(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH, float gap = 8f)
        {
            if (elements == null || elements.Count == 0 || doc == null) return;
            var coords = RustCanvasCoordinates.Instance;

            var sorted = elements.OrderBy(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).yMin).ToList();
            float currentY = coords.GetElementCanvasRect(sorted[0], doc, canvasW, canvasH).yMin;

            foreach (var elem in sorted)
            {
                if (elem.IsLocked) continue;
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(r.x, currentY, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
                currentY += r.height + gap;
            }
        }

        public static void ApplyHorizontalStack(List<CuiElementNode> elements, CuiDocument doc, float canvasW, float canvasH, float gap = 8f)
        {
            if (elements == null || elements.Count == 0 || doc == null) return;
            var coords = RustCanvasCoordinates.Instance;

            var sorted = elements.OrderBy(e => coords.GetElementCanvasRect(e, doc, canvasW, canvasH).xMin).ToList();
            float currentX = coords.GetElementCanvasRect(sorted[0], doc, canvasW, canvasH).xMin;

            foreach (var elem in sorted)
            {
                if (elem.IsLocked) continue;
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(currentX, r.y, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
                currentX += r.width + gap;
            }
        }
    }
}
