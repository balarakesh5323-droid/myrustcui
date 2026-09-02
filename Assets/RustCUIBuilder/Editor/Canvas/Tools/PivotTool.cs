using System;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas.Tools
{
    /// <summary>
    /// Interactive pivot point manipulation tool.
    /// Displays and allows visual dragging of the element's Pivot property.
    /// </summary>
    public class PivotTool : ICanvasTool
    {
        public CanvasToolMode ToolMode => CanvasToolMode.Pivot;
        public string ToolName => "Pivot";

        private bool _isDraggingPivot;
        private Vector2 _initialPivot;

        public void OnToolActivate() { _isDraggingPivot = false; }
        public void OnToolDeactivate() { _isDraggingPivot = false; }

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

            var primary = doc.PrimarySelectedElement;
            if (primary == null || primary.IsLocked || primary.IsHidden) return false;

            var rectComp = primary.GetComponent<CuiRectTransformComponent>();
            if (rectComp == null) return false;

            var coords = RustCanvasCoordinates.Instance;

            // 1. Mouse Down
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
            {
                var hit = CanvasHitTester.HitTestHandles(
                    currentEvent.mousePosition, primary, doc,
                    viewportRect, pan, zoom, canvasWidth, canvasHeight,
                    false, true);

                if (hit.HitType == HandleHitType.Pivot)
                {
                    _isDraggingPivot = true;
                    _initialPivot = RustCanvasScaler.ParseVector2(rectComp.Pivot, new Vector2(0.5f, 0.5f));

                    currentEvent.Use();
                    return true;
                }
            }

            // 2. Mouse Drag
            if (currentEvent.type == EventType.MouseDrag && _isDraggingPivot)
            {
                var elemCanvasRect = coords.GetElementCanvasRect(primary, doc, canvasWidth, canvasHeight);
                var mouseCanvas = coords.ScreenToCanvas(currentEvent.mousePosition, viewportRect, pan, zoom);

                float pX = elemCanvasRect.width > 0 ? Mathf.Clamp01((mouseCanvas.x - elemCanvasRect.x) / elemCanvasRect.width) : 0.5f;
                float pY = elemCanvasRect.height > 0 ? Mathf.Clamp01(1.0f - ((mouseCanvas.y - elemCanvasRect.y) / elemCanvasRect.height)) : 0.5f;

                if (currentEvent.control)
                {
                    pX = Mathf.Round(pX * 2f) / 2f; // Snap to 0.0, 0.5, 1.0
                    pY = Mathf.Round(pY * 2f) / 2f;
                }

                rectComp.Pivot = $"{pX:0.###} {pY:0.###}";
                onModified?.Invoke();
                currentEvent.Use();
                return true;
            }

            // 3. Mouse Up
            if (currentEvent.type == EventType.MouseUp && _isDraggingPivot)
            {
                _isDraggingPivot = false;
                onCommitUndo?.Invoke($"Set Pivot for {primary.Name}");
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
            var primary = doc?.PrimarySelectedElement;
            if (primary == null || primary.IsHidden) return;

            var coords = RustCanvasCoordinates.Instance;
            var pivotScreen = coords.GetPivotScreenPoint(primary, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);

            Handles.BeginGUI();
            Handles.color = Color.cyan;
            Handles.DrawWireDisc(pivotScreen, Vector3.forward, 6f);
            Handles.DrawLine(new Vector3(pivotScreen.x - 10, pivotScreen.y, 0), new Vector3(pivotScreen.x + 10, pivotScreen.y, 0));
            Handles.DrawLine(new Vector3(pivotScreen.x, pivotScreen.y - 10, 0), new Vector3(pivotScreen.x, pivotScreen.y + 10, 0));
            Handles.EndGUI();
        }
    }
}
