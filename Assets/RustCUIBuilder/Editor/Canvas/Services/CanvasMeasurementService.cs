using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas.Services
{
    /// <summary>
    /// Master measurement and distance inspection service.
    /// Provides Figma-style Alt distance measurements, parent boundary gaps, and live dimension HUD overlays.
    /// </summary>
    public static class CanvasMeasurementService
    {
        private static GUIStyle _pillStyle;

        private static GUIStyle PillStyle
        {
            get
            {
                if (_pillStyle == null)
                {
                    _pillStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white },
                        fontSize = 10,
                        fontStyle = FontStyle.Bold
                    };
                }
                return _pillStyle;
            }
        }

        public static void DrawMeasurements(
            CuiElementNode selectedElem,
            Vector2 mouseScreenPos,
            CuiDocument doc,
            Rect viewportRect,
            Vector2 pan,
            float zoom,
            float canvasW,
            float canvasH)
        {
            if (selectedElem == null || doc == null) return;
            var coords = RustCanvasCoordinates.Instance;

            var selRect = coords.GetElementCanvasRect(selectedElem, doc, canvasW, canvasH);
            var selScreen = coords.CanvasToScreen(selRect, viewportRect, pan, zoom);

            // 1. Check if hovering over another element to show relative distances (Figma Alt mode)
            var mouseCanvas = coords.ScreenToCanvas(mouseScreenPos, viewportRect, pan, zoom);
            CuiElementNode targetElem = null;

            foreach (var elem in doc.Elements)
            {
                if (elem.Id == selectedElem.Id || elem.IsHidden) continue;
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                if (r.Contains(mouseCanvas))
                {
                    targetElem = elem;
                    break;
                }
            }

            Rect targetRect = targetElem != null
                ? coords.GetElementCanvasRect(targetElem, doc, canvasW, canvasH)
                : coords.GetParentCanvasRect(selectedElem, doc, canvasW, canvasH);

            DrawDistanceBetweenRects(selRect, targetRect, viewportRect, pan, zoom, targetElem != null ? "Element" : "Parent");
        }

        private static void DrawDistanceBetweenRects(Rect r1, Rect r2, Rect viewportRect, Vector2 pan, float zoom, string targetType)
        {
            var coords = RustCanvasCoordinates.Instance;
            Handles.BeginGUI();
            Color measurementColor = new Color(1.0f, 0.25f, 0.4f, 0.95f); // Figma Pink
            Handles.color = measurementColor;

            // Horizontal Gap
            if (r1.xMax < r2.xMin)
            {
                // r1 is left of r2
                float midY = (Mathf.Max(r1.yMin, r2.yMin) + Mathf.Min(r1.yMax, r2.yMax)) * 0.5f;
                var p1 = coords.CanvasToScreen(new Vector2(r1.xMax, midY), viewportRect, pan, zoom);
                var p2 = coords.CanvasToScreen(new Vector2(r2.xMin, midY), viewportRect, pan, zoom);
                DrawDistanceLineWithPill(p1, p2, Mathf.RoundToInt(r2.xMin - r1.xMax), measurementColor);
            }
            else if (r2.xMax < r1.xMin)
            {
                // r2 is left of r1
                float midY = (Mathf.Max(r1.yMin, r2.yMin) + Mathf.Min(r1.yMax, r2.yMax)) * 0.5f;
                var p1 = coords.CanvasToScreen(new Vector2(r2.xMax, midY), viewportRect, pan, zoom);
                var p2 = coords.CanvasToScreen(new Vector2(r1.xMin, midY), viewportRect, pan, zoom);
                DrawDistanceLineWithPill(p1, p2, Mathf.RoundToInt(r1.xMin - r2.xMax), measurementColor);
            }

            // Vertical Gap
            if (r1.yMax < r2.yMin)
            {
                // r1 is above r2
                float midX = (Mathf.Max(r1.xMin, r2.xMin) + Mathf.Min(r1.xMax, r2.xMax)) * 0.5f;
                var p1 = coords.CanvasToScreen(new Vector2(midX, r1.yMax), viewportRect, pan, zoom);
                var p2 = coords.CanvasToScreen(new Vector2(midX, r2.yMin), viewportRect, pan, zoom);
                DrawDistanceLineWithPill(p1, p2, Mathf.RoundToInt(r2.yMin - r1.yMax), measurementColor);
            }
            else if (r2.yMax < r1.yMin)
            {
                // r2 is above r1
                float midX = (Mathf.Max(r1.xMin, r2.xMin) + Mathf.Min(r1.xMax, r2.xMax)) * 0.5f;
                var p1 = coords.CanvasToScreen(new Vector2(midX, r2.yMax), viewportRect, pan, zoom);
                var p2 = coords.CanvasToScreen(new Vector2(midX, r1.yMin), viewportRect, pan, zoom);
                DrawDistanceLineWithPill(p1, p2, Mathf.RoundToInt(r1.yMin - r2.yMax), measurementColor);
            }

            // Inside Bounds Distances (e.g. Left/Top/Right/Bottom inside Parent)
            if (r2.Contains(r1.min) && r2.Contains(r1.max))
            {
                // Left offset
                var pL1 = coords.CanvasToScreen(new Vector2(r2.xMin, r1.center.y), viewportRect, pan, zoom);
                var pL2 = coords.CanvasToScreen(new Vector2(r1.xMin, r1.center.y), viewportRect, pan, zoom);
                DrawDistanceLineWithPill(pL1, pL2, Mathf.RoundToInt(r1.xMin - r2.xMin), measurementColor);

                // Right offset
                var pR1 = coords.CanvasToScreen(new Vector2(r1.xMax, r1.center.y), viewportRect, pan, zoom);
                var pR2 = coords.CanvasToScreen(new Vector2(r2.xMax, r1.center.y), viewportRect, pan, zoom);
                DrawDistanceLineWithPill(pR1, pR2, Mathf.RoundToInt(r2.xMax - r1.xMax), measurementColor);

                // Top offset
                var pT1 = coords.CanvasToScreen(new Vector2(r1.center.x, r2.yMin), viewportRect, pan, zoom);
                var pT2 = coords.CanvasToScreen(new Vector2(r1.center.x, r1.yMin), viewportRect, pan, zoom);
                DrawDistanceLineWithPill(pT1, pT2, Mathf.RoundToInt(r1.yMin - r2.yMin), measurementColor);

                // Bottom offset
                var pB1 = coords.CanvasToScreen(new Vector2(r1.center.x, r1.yMax), viewportRect, pan, zoom);
                var pB2 = coords.CanvasToScreen(new Vector2(r1.center.x, r2.yMax), viewportRect, pan, zoom);
                DrawDistanceLineWithPill(pB1, pB2, Mathf.RoundToInt(r2.yMax - r1.yMax), measurementColor);
            }

            Handles.EndGUI();
        }

        private static void DrawDistanceLineWithPill(Vector2 p1, Vector2 p2, int distancePx, Color color)
        {
            if (distancePx <= 0) return;
            Handles.color = color;
            Handles.DrawLine(p1, p2);

            // Draw End Ticks
            Vector2 dir = (p2 - p1).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x) * 4f;
            Handles.DrawLine(p1 - perp, p1 + perp);
            Handles.DrawLine(p2 - perp, p2 + perp);

            // Draw Dimension Pill Badge
            Vector2 mid = (p1 + p2) * 0.5f;
            string text = $"{distancePx}px";
            var size = PillStyle.CalcSize(new GUIContent(text));
            var pillRect = new Rect(mid.x - size.x * 0.5f - 3f, mid.y - size.y * 0.5f - 1f, size.x + 6f, size.y + 2f);

            EditorGUI.DrawRect(pillRect, new Color(0.9f, 0.15f, 0.35f, 0.95f));
            GUI.Label(pillRect, text, PillStyle);
        }

        public static void DrawDimensionHud(Rect elemCanvasRect, Rect viewportRect, Vector2 pan, float zoom)
        {
            var coords = RustCanvasCoordinates.Instance;
            var screenRect = coords.CanvasToScreen(elemCanvasRect, viewportRect, pan, zoom);

            string text = $"W: {Mathf.RoundToInt(elemCanvasRect.width)}px  H: {Mathf.RoundToInt(elemCanvasRect.height)}px";
            var size = PillStyle.CalcSize(new GUIContent(text));
            var hudRect = new Rect(screenRect.center.x - size.x * 0.5f - 4f, screenRect.yMax + 6f, size.x + 8f, size.y + 3f);

            EditorGUI.DrawRect(hudRect, new Color(0.12f, 0.14f, 0.18f, 0.92f));
            GUI.Label(hudRect, text, PillStyle);
        }
    }
}
