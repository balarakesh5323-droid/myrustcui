using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas.Tools
{
    /// <summary>
    /// Selection tool supporting single-click picking, Shift/Ctrl multi-selection,
    /// empty-canvas deselect, and drag marquee bounding-box selection.
    /// </summary>
    public class SelectTool : ICanvasTool
    {
        public CanvasToolMode ToolMode => CanvasToolMode.Select;
        public string ToolName => "Select / Marquee";

        private bool _isMarqueeDragging;
        private Vector2 _marqueeStartScreen;
        private Vector2 _marqueeCurrentScreen;

        public void OnToolActivate() { _isMarqueeDragging = false; }
        public void OnToolDeactivate() { _isMarqueeDragging = false; }

        public bool ProcessEvent(
            Event currentEvent,
            Rect viewportRect,
            Vector2 pan,
            float zoom,
            float canvasWidth,
            float canvasHeight,
            CuiDocument doc,
            CanvasGuideSystem guideSystem,
            Action onModified,
            Action<string> onCommitUndo)
        {
            if (doc == null || !viewportRect.Contains(currentEvent.mousePosition)) return false;

            // 1. Mouse Down
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
            {
                var hitElem = CanvasHitTester.HitTestElements(currentEvent.mousePosition, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);

                if (hitElem != null)
                {
                    if (currentEvent.control || currentEvent.command)
                    {
                        // Toggle selection
                        if (doc.IsSelected(hitElem.Id)) doc.Deselect(hitElem.Id);
                        else doc.Select(hitElem.Id, true);
                    }
                    else if (currentEvent.shift)
                    {
                        // Add to selection
                        doc.Select(hitElem.Id, true);
                    }
                    else
                    {
                        // Single selection (if not already selected)
                        if (!doc.IsSelected(hitElem.Id))
                        {
                            doc.Select(hitElem.Id, false);
                        }
                    }
                }
                else
                {
                    // Clicked empty canvas -> start marquee or clear selection
                    if (!currentEvent.shift && !currentEvent.control)
                    {
                        doc.ClearSelection();
                    }
                    _isMarqueeDragging = true;
                    _marqueeStartScreen = currentEvent.mousePosition;
                    _marqueeCurrentScreen = currentEvent.mousePosition;
                }

                currentEvent.Use();
                return true;
            }

            // 2. Mouse Drag (Marquee)
            if (currentEvent.type == EventType.MouseDrag && _isMarqueeDragging)
            {
                _marqueeCurrentScreen = currentEvent.mousePosition;
                var marqueeRect = GetMarqueeRect();

                var enclosed = CanvasHitTester.MarqueeHitTest(marqueeRect, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);
                if (!currentEvent.shift && !currentEvent.control)
                {
                    doc.ClearSelection();
                }
                foreach (var elem in enclosed)
                {
                    doc.Select(elem.Id, true);
                }

                currentEvent.Use();
                return true;
            }

            // 3. Mouse Up
            if (currentEvent.type == EventType.MouseUp && _isMarqueeDragging)
            {
                _isMarqueeDragging = false;
                currentEvent.Use();
                return true;
            }

            return false;
        }

        public void DrawToolOverlay(
            Rect viewportRect,
            Vector2 pan,
            float zoom,
            float canvasWidth,
            float canvasHeight,
            CuiDocument doc)
        {
            if (!_isMarqueeDragging) return;

            var marqueeRect = GetMarqueeRect();
            EditorGUI.DrawRect(marqueeRect, new Color(0.2f, 0.6f, 1.0f, 0.15f));

            Handles.BeginGUI();
            Handles.color = new Color(0.3f, 0.75f, 1.0f, 0.9f);
            Handles.DrawPolyLine(
                new Vector3(marqueeRect.xMin, marqueeRect.yMin, 0),
                new Vector3(marqueeRect.xMax, marqueeRect.yMin, 0),
                new Vector3(marqueeRect.xMax, marqueeRect.yMax, 0),
                new Vector3(marqueeRect.xMin, marqueeRect.yMax, 0),
                new Vector3(marqueeRect.xMin, marqueeRect.yMin, 0)
            );
            Handles.EndGUI();
        }

        private Rect GetMarqueeRect()
        {
            float xMin = Mathf.Min(_marqueeStartScreen.x, _marqueeCurrentScreen.x);
            float xMax = Mathf.Max(_marqueeStartScreen.x, _marqueeCurrentScreen.x);
            float yMin = Mathf.Min(_marqueeStartScreen.y, _marqueeCurrentScreen.y);
            float yMax = Mathf.Max(_marqueeStartScreen.y, _marqueeCurrentScreen.y);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
