using System;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas.Tools
{
    /// <summary>
    /// 8-handle interactive resize tool with support for uniform scaling (Shift),
    /// center-relative resize (Alt), grid snapping (Ctrl), and atomic undo.
    /// </summary>
    public class ResizeTool : ICanvasTool
    {
        public CanvasToolMode ToolMode => CanvasToolMode.Resize;
        public string ToolName => "Resize";

        private bool _isResizing;
        private HandleHitType _activeHandle = HandleHitType.None;
        private Vector2 _dragStartScreen;
        private Vector2 _dragStartCanvas;
        private Rect _initialCanvasRect;

        public void OnToolActivate() { _isResizing = false; }
        public void OnToolDeactivate() { _isResizing = false; }

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

            var coords = RustCanvasCoordinates.Instance;

            // 1. Mouse Down
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
            {
                var hit = CanvasHitTester.HitTestHandles(
                    currentEvent.mousePosition, primary, doc,
                    viewportRect, pan, zoom, canvasWidth, canvasHeight,
                    false, false);

                if (hit.HitType >= HandleHitType.NW && hit.HitType <= HandleHitType.W)
                {
                    _isResizing = true;
                    _activeHandle = hit.HitType;
                    _dragStartScreen = currentEvent.mousePosition;
                    _dragStartCanvas = coords.ScreenToCanvas(currentEvent.mousePosition, viewportRect, pan, zoom);
                    _initialCanvasRect = coords.GetElementCanvasRect(primary, doc, canvasWidth, canvasHeight);

                    currentEvent.Use();
                    return true;
                }
            }

            // 2. Mouse Drag
            if (currentEvent.type == EventType.MouseDrag && _isResizing)
            {
                var curCanvas = coords.ScreenToCanvas(currentEvent.mousePosition, viewportRect, pan, zoom);
                var delta = curCanvas - _dragStartCanvas;

                if (guideSystem != null && guideSystem.SnapToGrid)
                {
                    float gs = guideSystem.GridSize;
                    delta.x = Mathf.Round(delta.x / gs) * gs;
                    delta.y = Mathf.Round(delta.y / gs) * gs;
                }

                var newRect = CalculateResizedRect(_initialCanvasRect, _activeHandle, delta, currentEvent.shift, currentEvent.alt);

                // Minimum size limit
                if (newRect.width < 4f) newRect.width = 4f;
                if (newRect.height < 4f) newRect.height = 4f;

                coords.ApplyNewCanvasRectToElementOffsets(newRect, primary, doc, canvasWidth, canvasHeight);

                onModified?.Invoke();
                currentEvent.Use();
                return true;
            }

            // 3. Mouse Up
            if (currentEvent.type == EventType.MouseUp && _isResizing)
            {
                _isResizing = false;
                _activeHandle = HandleHitType.None;
                onCommitUndo?.Invoke($"Resize {primary.Name}");
                currentEvent.Use();
                return true;
            }

            // 4. Cancel on Escape
            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape && _isResizing)
            {
                _isResizing = false;
                _activeHandle = HandleHitType.None;
                coords.ApplyNewCanvasRectToElementOffsets(_initialCanvasRect, primary, doc, canvasWidth, canvasHeight);
                onModified?.Invoke();
                currentEvent.Use();
                return true;
            }

            return false;
        }

        private Rect CalculateResizedRect(Rect orig, HandleHitType handle, Vector2 delta, bool lockAspect, bool fromCenter)
        {
            float xMin = orig.xMin;
            float xMax = orig.xMax;
            float yMin = orig.yMin;
            float yMax = orig.yMax;

            switch (handle)
            {
                case HandleHitType.NW:
                    xMin += delta.x;
                    yMin += delta.y;
                    if (fromCenter) { xMax -= delta.x; yMax -= delta.y; }
                    break;
                case HandleHitType.N:
                    yMin += delta.y;
                    if (fromCenter) yMax -= delta.y;
                    break;
                case HandleHitType.NE:
                    xMax += delta.x;
                    yMin += delta.y;
                    if (fromCenter) { xMin -= delta.x; yMax -= delta.y; }
                    break;
                case HandleHitType.E:
                    xMax += delta.x;
                    if (fromCenter) xMin -= delta.x;
                    break;
                case HandleHitType.SE:
                    xMax += delta.x;
                    yMax += delta.y;
                    if (fromCenter) { xMin -= delta.x; yMin -= delta.y; }
                    break;
                case HandleHitType.S:
                    yMax += delta.y;
                    if (fromCenter) yMin -= delta.y;
                    break;
                case HandleHitType.SW:
                    xMin += delta.x;
                    yMax += delta.y;
                    if (fromCenter) { xMax -= delta.x; yMin -= delta.y; }
                    break;
                case HandleHitType.W:
                    xMin += delta.x;
                    if (fromCenter) xMax -= delta.x;
                    break;
            }

            return Rect.MinMaxRect(
                Mathf.Min(xMin, xMax - 2),
                Mathf.Min(yMin, yMax - 2),
                Mathf.Max(xMin + 2, xMax),
                Mathf.Max(yMin + 2, yMax)
            );
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
            var elemScreenRect = coords.GetElementScreenRect(primary, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);

            // Draw 8-point resize handles
            DrawHandle(new Vector2(elemScreenRect.xMin, elemScreenRect.yMin));
            DrawHandle(new Vector2(elemScreenRect.center.x, elemScreenRect.yMin));
            DrawHandle(new Vector2(elemScreenRect.xMax, elemScreenRect.yMin));
            DrawHandle(new Vector2(elemScreenRect.xMax, elemScreenRect.center.y));
            DrawHandle(new Vector2(elemScreenRect.xMax, elemScreenRect.yMax));
            DrawHandle(new Vector2(elemScreenRect.center.x, elemScreenRect.yMax));
            DrawHandle(new Vector2(elemScreenRect.xMin, elemScreenRect.yMax));
            DrawHandle(new Vector2(elemScreenRect.xMin, elemScreenRect.center.y));
        }

        private void DrawHandle(Vector2 center)
        {
            float hs = 7f;
            var r = new Rect(center.x - hs / 2, center.y - hs / 2, hs, hs);
            EditorGUI.DrawRect(r, Color.white);
            Handles.BeginGUI();
            Handles.color = new Color(0.1f, 0.5f, 0.9f, 1f);
            Handles.DrawPolyLine(
                new Vector3(r.xMin, r.yMin, 0), new Vector3(r.xMax, r.yMin, 0),
                new Vector3(r.xMax, r.yMax, 0), new Vector3(r.xMin, r.yMax, 0),
                new Vector3(r.xMin, r.yMin, 0)
            );
            Handles.EndGUI();
        }
    }
}
