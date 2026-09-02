using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas
{
    public enum HandleHitType
    {
        None,
        Body,
        NW,
        N,
        NE,
        E,
        SE,
        S,
        SW,
        W,
        Pivot,
        AnchorNW,
        AnchorNE,
        AnchorSW,
        AnchorSE
    }

    public class HandleHitResult
    {
        public HandleHitType HitType;
        public CuiElementNode Element;
        public Rect ElementScreenRect;
    }

    /// <summary>
    /// Deterministic hit testing engine for Rust CUI Canvas elements,
    /// bounding box edges, 8 resize handles, pivot point, and 4 anchor pins.
    /// </summary>
    public static class CanvasHitTester
    {
        public const float HandleHitSize = 10f;
        public const float AnchorHitSize = 12f;

        public static HandleHitResult HitTestHandles(
            Vector2 mouseScreenPos,
            CuiElementNode primarySelected,
            CuiDocument doc,
            Rect viewportRect,
            Vector2 pan,
            float zoom,
            float canvasWidth,
            float canvasHeight,
            bool showAnchors = true,
            bool showPivot = true)
        {
            if (primarySelected == null || primarySelected.IsHidden)
                return new HandleHitResult { HitType = HandleHitType.None };

            var coords = RustCanvasCoordinates.Instance;
            var elemScreenRect = coords.GetElementScreenRect(primarySelected, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);

            // 1. Pivot handle hit test
            if (showPivot)
            {
                var pivotScreen = coords.GetPivotScreenPoint(primarySelected, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);
                if (Vector2.Distance(mouseScreenPos, pivotScreen) <= HandleHitSize)
                {
                    return new HandleHitResult { HitType = HandleHitType.Pivot, Element = primarySelected, ElementScreenRect = elemScreenRect };
                }
            }

            // 2. Anchor handles hit test
            if (showAnchors)
            {
                var anchorPoints = coords.GetAnchorScreenPoints(primarySelected, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);
                if (anchorPoints.Length == 4)
                {
                    if (Vector2.Distance(mouseScreenPos, anchorPoints[0]) <= AnchorHitSize)
                        return new HandleHitResult { HitType = HandleHitType.AnchorNW, Element = primarySelected, ElementScreenRect = elemScreenRect };
                    if (Vector2.Distance(mouseScreenPos, anchorPoints[1]) <= AnchorHitSize)
                        return new HandleHitResult { HitType = HandleHitType.AnchorNE, Element = primarySelected, ElementScreenRect = elemScreenRect };
                    if (Vector2.Distance(mouseScreenPos, anchorPoints[2]) <= AnchorHitSize)
                        return new HandleHitResult { HitType = HandleHitType.AnchorSW, Element = primarySelected, ElementScreenRect = elemScreenRect };
                    if (Vector2.Distance(mouseScreenPos, anchorPoints[3]) <= AnchorHitSize)
                        return new HandleHitResult { HitType = HandleHitType.AnchorSE, Element = primarySelected, ElementScreenRect = elemScreenRect };
                }
            }

            // 3. 8-Point Bounding Box Resize Handles
            float hs = HandleHitSize;
            var nwRect = new Rect(elemScreenRect.xMin - hs / 2, elemScreenRect.yMin - hs / 2, hs, hs);
            var neRect = new Rect(elemScreenRect.xMax - hs / 2, elemScreenRect.yMin - hs / 2, hs, hs);
            var swRect = new Rect(elemScreenRect.xMin - hs / 2, elemScreenRect.yMax - hs / 2, hs, hs);
            var seRect = new Rect(elemScreenRect.xMax - hs / 2, elemScreenRect.yMax - hs / 2, hs, hs);

            var nRect = new Rect(elemScreenRect.center.x - hs / 2, elemScreenRect.yMin - hs / 2, hs, hs);
            var sRect = new Rect(elemScreenRect.center.x - hs / 2, elemScreenRect.yMax - hs / 2, hs, hs);
            var wRect = new Rect(elemScreenRect.xMin - hs / 2, elemScreenRect.center.y - hs / 2, hs, hs);
            var eRect = new Rect(elemScreenRect.xMax - hs / 2, elemScreenRect.center.y - hs / 2, hs, hs);

            if (nwRect.Contains(mouseScreenPos)) return new HandleHitResult { HitType = HandleHitType.NW, Element = primarySelected, ElementScreenRect = elemScreenRect };
            if (neRect.Contains(mouseScreenPos)) return new HandleHitResult { HitType = HandleHitType.NE, Element = primarySelected, ElementScreenRect = elemScreenRect };
            if (swRect.Contains(mouseScreenPos)) return new HandleHitResult { HitType = HandleHitType.SW, Element = primarySelected, ElementScreenRect = elemScreenRect };
            if (seRect.Contains(mouseScreenPos)) return new HandleHitResult { HitType = HandleHitType.SE, Element = primarySelected, ElementScreenRect = elemScreenRect };

            if (nRect.Contains(mouseScreenPos)) return new HandleHitResult { HitType = HandleHitType.N, Element = primarySelected, ElementScreenRect = elemScreenRect };
            if (sRect.Contains(mouseScreenPos)) return new HandleHitResult { HitType = HandleHitType.S, Element = primarySelected, ElementScreenRect = elemScreenRect };
            if (wRect.Contains(mouseScreenPos)) return new HandleHitResult { HitType = HandleHitType.W, Element = primarySelected, ElementScreenRect = elemScreenRect };
            if (eRect.Contains(mouseScreenPos)) return new HandleHitResult { HitType = HandleHitType.E, Element = primarySelected, ElementScreenRect = elemScreenRect };

            // 4. Body hit
            if (elemScreenRect.Contains(mouseScreenPos))
            {
                return new HandleHitResult { HitType = HandleHitType.Body, Element = primarySelected, ElementScreenRect = elemScreenRect };
            }

            return new HandleHitResult { HitType = HandleHitType.None };
        }

        public static CuiElementNode HitTestElements(
            Vector2 mouseScreenPos,
            CuiDocument doc,
            Rect viewportRect,
            Vector2 pan,
            float zoom,
            float canvasWidth,
            float canvasHeight)
        {
            if (doc == null || doc.Elements == null) return null;

            var coords = RustCanvasCoordinates.Instance;

            // Iterate reverse order (top-most element rendered last receives hit first)
            for (int i = doc.Elements.Count - 1; i >= 0; i--)
            {
                var elem = doc.Elements[i];
                if (elem.IsHidden) continue;

                var screenRect = coords.GetElementScreenRect(elem, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);
                if (screenRect.Contains(mouseScreenPos))
                {
                    return elem;
                }
            }

            return null;
        }

        public static List<CuiElementNode> MarqueeHitTest(
            Rect marqueeScreenRect,
            CuiDocument doc,
            Rect viewportRect,
            Vector2 pan,
            float zoom,
            float canvasWidth,
            float canvasHeight)
        {
            var result = new List<CuiElementNode>();
            if (doc == null || doc.Elements == null) return result;

            var coords = RustCanvasCoordinates.Instance;
            foreach (var elem in doc.Elements)
            {
                if (elem.IsHidden || elem.IsLocked) continue;

                var elemRect = coords.GetElementScreenRect(elem, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);
                if (marqueeScreenRect.Overlaps(elemRect))
                {
                    result.Add(elem);
                }
            }

            return result;
        }
    }
}
