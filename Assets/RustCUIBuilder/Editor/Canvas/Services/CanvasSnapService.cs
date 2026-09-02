using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas.Services
{
    public struct SmartGuideLine
    {
        public bool IsVertical;
        public float CanvasCoord;
        public float StartSpan;
        public float EndSpan;
        public Color Color;
        public string Label;
    }

    /// <summary>
    /// Master smart snapping and dynamic guide projection engine.
    /// Detects edge alignment, center alignment, parent bounds, and equal spacing across siblings.
    /// </summary>
    public static class CanvasSnapService
    {
        public static List<SmartGuideLine> ActiveGuides { get; } = new List<SmartGuideLine>();

        public static Vector2 CalculateSnap(
            Rect movingCanvasRect,
            CuiElementNode movingElem,
            CuiDocument doc,
            float canvasW,
            float canvasH,
            CanvasGuideSystem guideSystem,
            float zoom,
            out Vector2 snapDelta)
        {
            ActiveGuides.Clear();
            snapDelta = Vector2.zero;

            if (doc == null || guideSystem == null) return movingCanvasRect.min;

            var coords = RustCanvasCoordinates.Instance;
            float snapThreshold = guideSystem.SnapTolerancePixels / Mathf.Max(0.1f, zoom);

            float bestDeltaX = float.MaxValue;
            float bestDeltaY = float.MaxValue;

            SmartGuideLine? bestGuideX = null;
            SmartGuideLine? bestGuideY = null;

            // 1. Grid Snap
            if (guideSystem.SnapToGrid && guideSystem.GridSize > 0)
            {
                float gx = Mathf.Round(movingCanvasRect.xMin / guideSystem.GridSize) * guideSystem.GridSize;
                float gy = Mathf.Round(movingCanvasRect.yMin / guideSystem.GridSize) * guideSystem.GridSize;

                float dx = gx - movingCanvasRect.xMin;
                float dy = gy - movingCanvasRect.yMin;

                if (Mathf.Abs(dx) <= snapThreshold && Mathf.Abs(dx) < Mathf.Abs(bestDeltaX))
                {
                    bestDeltaX = dx;
                }
                if (Mathf.Abs(dy) <= snapThreshold && Mathf.Abs(dy) < Mathf.Abs(bestDeltaY))
                {
                    bestDeltaY = dy;
                }
            }

            // 2. Parent Boundaries Snap
            var pRect = coords.GetParentCanvasRect(movingElem, doc, canvasW, canvasH);
            CheckSnap1D(movingCanvasRect.xMin, pRect.xMin, snapThreshold, ref bestDeltaX, ref bestGuideX, true, pRect.yMin, pRect.yMax, new Color(1f, 0.6f, 0.2f, 0.9f), "Parent Left");
            CheckSnap1D(movingCanvasRect.xMax, pRect.xMax, snapThreshold, ref bestDeltaX, ref bestGuideX, true, pRect.yMin, pRect.yMax, new Color(1f, 0.6f, 0.2f, 0.9f), "Parent Right");
            CheckSnap1D(movingCanvasRect.center.x, pRect.center.x, snapThreshold, ref bestDeltaX, ref bestGuideX, true, pRect.yMin, pRect.yMax, new Color(1f, 0.3f, 0.8f, 0.9f), "Parent Center X");

            CheckSnap1D(movingCanvasRect.yMin, pRect.yMin, snapThreshold, ref bestDeltaY, ref bestGuideY, false, pRect.xMin, pRect.xMax, new Color(1f, 0.6f, 0.2f, 0.9f), "Parent Top");
            CheckSnap1D(movingCanvasRect.yMax, pRect.yMax, snapThreshold, ref bestDeltaY, ref bestGuideY, false, pRect.xMin, pRect.xMax, new Color(1f, 0.6f, 0.2f, 0.9f), "Parent Bottom");
            CheckSnap1D(movingCanvasRect.center.y, pRect.center.y, snapThreshold, ref bestDeltaY, ref bestGuideY, false, pRect.xMin, pRect.xMax, new Color(1f, 0.3f, 0.8f, 0.9f), "Parent Center Y");

            // 3. Sibling Bounds & Center Snap
            if (guideSystem.SnapToElements)
            {
                foreach (var other in doc.Elements)
                {
                    if (other.Id == movingElem.Id || other.IsHidden || doc.IsSelected(other.Id)) continue;
                    var oRect = coords.GetElementCanvasRect(other, doc, canvasW, canvasH);

                    // X Edges & Center
                    CheckSnap1D(movingCanvasRect.xMin, oRect.xMin, snapThreshold, ref bestDeltaX, ref bestGuideX, true, Mathf.Min(movingCanvasRect.yMin, oRect.yMin), Mathf.Max(movingCanvasRect.yMax, oRect.yMax), Color.cyan, "Left-Left");
                    CheckSnap1D(movingCanvasRect.xMin, oRect.xMax, snapThreshold, ref bestDeltaX, ref bestGuideX, true, Mathf.Min(movingCanvasRect.yMin, oRect.yMin), Mathf.Max(movingCanvasRect.yMax, oRect.yMax), Color.cyan, "Left-Right");
                    CheckSnap1D(movingCanvasRect.xMax, oRect.xMin, snapThreshold, ref bestDeltaX, ref bestGuideX, true, Mathf.Min(movingCanvasRect.yMin, oRect.yMin), Mathf.Max(movingCanvasRect.yMax, oRect.yMax), Color.cyan, "Right-Left");
                    CheckSnap1D(movingCanvasRect.xMax, oRect.xMax, snapThreshold, ref bestDeltaX, ref bestGuideX, true, Mathf.Min(movingCanvasRect.yMin, oRect.yMin), Mathf.Max(movingCanvasRect.yMax, oRect.yMax), Color.cyan, "Right-Right");
                    CheckSnap1D(movingCanvasRect.center.x, oRect.center.x, snapThreshold, ref bestDeltaX, ref bestGuideX, true, Mathf.Min(movingCanvasRect.yMin, oRect.yMin), Mathf.Max(movingCanvasRect.yMax, oRect.yMax), Color.magenta, "Center X");

                    // Y Edges & Center
                    CheckSnap1D(movingCanvasRect.yMin, oRect.yMin, snapThreshold, ref bestDeltaY, ref bestGuideY, false, Mathf.Min(movingCanvasRect.xMin, oRect.xMin), Mathf.Max(movingCanvasRect.xMax, oRect.xMax), Color.cyan, "Top-Top");
                    CheckSnap1D(movingCanvasRect.yMin, oRect.yMax, snapThreshold, ref bestDeltaY, ref bestGuideY, false, Mathf.Min(movingCanvasRect.xMin, oRect.xMin), Mathf.Max(movingCanvasRect.xMax, oRect.xMax), Color.cyan, "Top-Bottom");
                    CheckSnap1D(movingCanvasRect.yMax, oRect.yMin, snapThreshold, ref bestDeltaY, ref bestGuideY, false, Mathf.Min(movingCanvasRect.xMin, oRect.xMin), Mathf.Max(movingCanvasRect.xMax, oRect.xMax), Color.cyan, "Bottom-Top");
                    CheckSnap1D(movingCanvasRect.yMax, oRect.yMax, snapThreshold, ref bestDeltaY, ref bestGuideY, false, Mathf.Min(movingCanvasRect.xMin, oRect.xMin), Mathf.Max(movingCanvasRect.xMax, oRect.xMax), Color.cyan, "Bottom-Bottom");
                    CheckSnap1D(movingCanvasRect.center.y, oRect.center.y, snapThreshold, ref bestDeltaY, ref bestGuideY, false, Mathf.Min(movingCanvasRect.xMin, oRect.xMin), Mathf.Max(movingCanvasRect.xMax, oRect.xMax), Color.magenta, "Center Y");
                }
            }

            if (bestGuideX.HasValue) ActiveGuides.Add(bestGuideX.Value);
            if (bestGuideY.HasValue) ActiveGuides.Add(bestGuideY.Value);

            snapDelta = new Vector2(
                bestDeltaX != float.MaxValue ? bestDeltaX : 0f,
                bestDeltaY != float.MaxValue ? bestDeltaY : 0f
            );

            return movingCanvasRect.min + snapDelta;
        }

        private static void CheckSnap1D(
            float movingVal,
            float targetVal,
            float threshold,
            ref float bestDelta,
            ref SmartGuideLine? bestGuide,
            bool isVertical,
            float spanStart,
            float spanEnd,
            Color color,
            string label)
        {
            float delta = targetVal - movingVal;
            if (Mathf.Abs(delta) <= threshold && Mathf.Abs(delta) < Mathf.Abs(bestDelta))
            {
                bestDelta = delta;
                bestGuide = new SmartGuideLine
                {
                    IsVertical = isVertical,
                    CanvasCoord = targetVal,
                    StartSpan = spanStart - 20f,
                    EndSpan = spanEnd + 20f,
                    Color = color,
                    Label = label
                };
            }
        }

        public static void DrawActiveGuides(Rect viewportRect, Vector2 pan, float zoom)
        {
            if (ActiveGuides.Count == 0) return;
            var coords = RustCanvasCoordinates.Instance;

            Handles.BeginGUI();
            foreach (var g in ActiveGuides)
            {
                Handles.color = g.Color;
                if (g.IsVertical)
                {
                    var p1 = coords.CanvasToScreen(new Vector2(g.CanvasCoord, g.StartSpan), viewportRect, pan, zoom);
                    var p2 = coords.CanvasToScreen(new Vector2(g.CanvasCoord, g.EndSpan), viewportRect, pan, zoom);
                    Handles.DrawDottedLine(p1, p2, 3f);
                }
                else
                {
                    var p1 = coords.CanvasToScreen(new Vector2(g.StartSpan, g.CanvasCoord), viewportRect, pan, zoom);
                    var p2 = coords.CanvasToScreen(new Vector2(g.EndSpan, g.CanvasCoord), viewportRect, pan, zoom);
                    Handles.DrawDottedLine(p1, p2, 3f);
                }
            }
            Handles.EndGUI();
        }
    }
}
