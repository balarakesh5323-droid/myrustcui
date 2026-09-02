using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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
        EdgeN,
        EdgeS,
        EdgeW,
        EdgeE,
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
    /// collective selection bounds, 8 resize handles, bounding edges, pivot point, and 4 anchor pins.
    /// Also registers interactive mouse cursors for professional Figma/Photoshop feel.
    /// </summary>
    public static class CanvasHitTester
    {
        public const float HandleHitSize = 12f;
        public const float AnchorHitSize = 14f;
        public const float EdgeHitThickness = 6f;

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

            return HitTestRectHandles(mouseScreenPos, elemScreenRect, primarySelected, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight, showAnchors, showPivot);
        }

        public static HandleHitResult HitTestSelectionHandles(
            Vector2 mouseScreenPos,
            List<CuiElementNode> selectedElements,
            CuiDocument doc,
            Rect viewportRect,
            Vector2 pan,
            float zoom,
            float canvasWidth,
            float canvasHeight,
            bool showAnchors = true,
            bool showPivot = true)
        {
            if (selectedElements == null || selectedElements.Count == 0)
                return new HandleHitResult { HitType = HandleHitType.None };

            if (selectedElements.Count == 1)
            {
                return HitTestHandles(mouseScreenPos, selectedElements[0], doc, viewportRect, pan, zoom, canvasWidth, canvasHeight, showAnchors, showPivot);
            }

            var coords = RustCanvasCoordinates.Instance;
            var combinedScreenRect = GetSelectionScreenRect(selectedElements, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);

            return HitTestRectHandles(mouseScreenPos, combinedScreenRect, selectedElements[0], doc, viewportRect, pan, zoom, canvasWidth, canvasHeight, false, false);
        }

        private static HandleHitResult HitTestRectHandles(
            Vector2 mouseScreenPos,
            Rect elemScreenRect,
            CuiElementNode primarySelected,
            CuiDocument doc,
            Rect viewportRect,
            Vector2 pan,
            float zoom,
            float canvasWidth,
            float canvasHeight,
            bool showAnchors,
            bool showPivot)
        {
            var coords = RustCanvasCoordinates.Instance;

            // 1. Pivot handle hit test
            if (showPivot && primarySelected != null)
            {
                var pivotScreen = coords.GetPivotScreenPoint(primarySelected, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);
                if (Vector2.Distance(mouseScreenPos, pivotScreen) <= HandleHitSize)
                {
                    return new HandleHitResult { HitType = HandleHitType.Pivot, Element = primarySelected, ElementScreenRect = elemScreenRect };
                }
            }

            // 2. Anchor handles hit test
            if (showAnchors && primarySelected != null)
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

            // 3. 8-Point Bounding Box Resize Corner & Edge Handles
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

            // 4. Edge-line hit testing (within border thickness)
            float et = EdgeHitThickness;
            var topEdge = new Rect(elemScreenRect.xMin, elemScreenRect.yMin - et / 2, elemScreenRect.width, et);
            var btmEdge = new Rect(elemScreenRect.xMin, elemScreenRect.yMax - et / 2, elemScreenRect.width, et);
            var leftEdge = new Rect(elemScreenRect.xMin - et / 2, elemScreenRect.yMin, et, elemScreenRect.height);
            var rightEdge = new Rect(elemScreenRect.xMax - et / 2, elemScreenRect.yMin, et, elemScreenRect.height);

            if (topEdge.Contains(mouseScreenPos)) return new HandleHitResult { HitType = HandleHitType.N, Element = primarySelected, ElementScreenRect = elemScreenRect };
            if (btmEdge.Contains(mouseScreenPos)) return new HandleHitResult { HitType = HandleHitType.S, Element = primarySelected, ElementScreenRect = elemScreenRect };
            if (leftEdge.Contains(mouseScreenPos)) return new HandleHitResult { HitType = HandleHitType.W, Element = primarySelected, ElementScreenRect = elemScreenRect };
            if (rightEdge.Contains(mouseScreenPos)) return new HandleHitResult { HitType = HandleHitType.E, Element = primarySelected, ElementScreenRect = elemScreenRect };

            // 5. Body hit
            if (elemScreenRect.Contains(mouseScreenPos))
            {
                return new HandleHitResult { HitType = HandleHitType.Body, Element = primarySelected, ElementScreenRect = elemScreenRect };
            }

            return new HandleHitResult { HitType = HandleHitType.None };
        }

        public static Rect GetSelectionScreenRect(
            List<CuiElementNode> elements,
            CuiDocument doc,
            Rect viewportRect,
            Vector2 pan,
            float zoom,
            float canvasWidth,
            float canvasHeight)
        {
            if (elements == null || elements.Count == 0) return Rect.zero;

            var coords = RustCanvasCoordinates.Instance;
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var elem in elements)
            {
                if (elem.IsHidden) continue;
                var r = coords.GetElementScreenRect(elem, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);
                minX = Mathf.Min(minX, r.xMin);
                minY = Mathf.Min(minY, r.yMin);
                maxX = Mathf.Max(maxX, r.xMax);
                maxY = Mathf.Max(maxY, r.yMax);
            }

            if (minX == float.MaxValue) return Rect.zero;
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
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
            var results = new List<CuiElementNode>();
            if (doc == null || doc.Elements == null) return results;

            var coords = RustCanvasCoordinates.Instance;

            foreach (var elem in doc.Elements)
            {
                if (elem.IsHidden || elem.IsLocked) continue;

                var screenRect = coords.GetElementScreenRect(elem, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);
                if (marqueeScreenRect.Overlaps(screenRect))
                {
                    results.Add(elem);
                }
            }

            return results;
        }

        public static void AddResizeCursorRects(Rect elemScreenRect)
        {
            float hs = HandleHitSize;
            var nwRect = new Rect(elemScreenRect.xMin - hs / 2, elemScreenRect.yMin - hs / 2, hs, hs);
            var neRect = new Rect(elemScreenRect.xMax - hs / 2, elemScreenRect.yMin - hs / 2, hs, hs);
            var swRect = new Rect(elemScreenRect.xMin - hs / 2, elemScreenRect.yMax - hs / 2, hs, hs);
            var seRect = new Rect(elemScreenRect.xMax - hs / 2, elemScreenRect.yMax - hs / 2, hs, hs);

            var nRect = new Rect(elemScreenRect.center.x - hs / 2, elemScreenRect.yMin - hs / 2, hs, hs);
            var sRect = new Rect(elemScreenRect.center.x - hs / 2, elemScreenRect.yMax - hs / 2, hs, hs);
            var wRect = new Rect(elemScreenRect.xMin - hs / 2, elemScreenRect.center.y - hs / 2, hs, hs);
            var eRect = new Rect(elemScreenRect.xMax - hs / 2, elemScreenRect.center.y - hs / 2, hs, hs);

            EditorGUIUtility.AddCursorRect(nwRect, MouseCursor.ResizeUpLeft);
            EditorGUIUtility.AddCursorRect(seRect, MouseCursor.ResizeUpLeft);
            EditorGUIUtility.AddCursorRect(neRect, MouseCursor.ResizeUpRight);
            EditorGUIUtility.AddCursorRect(swRect, MouseCursor.ResizeUpRight);

            EditorGUIUtility.AddCursorRect(nRect, MouseCursor.ResizeVertical);
            EditorGUIUtility.AddCursorRect(sRect, MouseCursor.ResizeVertical);
            EditorGUIUtility.AddCursorRect(wRect, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(eRect, MouseCursor.ResizeHorizontal);
        }
    }
}
