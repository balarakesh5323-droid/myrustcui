using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas.Tools
{
    /// <summary>
    /// Interactive element move tool supporting multi-element group movement,
    /// real-time grid and guide snapping, and atomic single-undo transaction on release.
    /// </summary>
    public class MoveTool : ICanvasTool
    {
        public CanvasToolMode ToolMode => CanvasToolMode.Move;
        public string ToolName => "Move";

        private bool _isDragging;
        private Vector2 _dragStartScreen;
        private Vector2 _dragStartCanvas;
        private readonly Dictionary<string, (Vector2 offsetMin, Vector2 offsetMax)> _initialOffsets = new Dictionary<string, (Vector2, Vector2)>();

        public void OnToolActivate() { _isDragging = false; }
        public void OnToolDeactivate() { _isDragging = false; }

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

            var coords = RustCanvasCoordinates.Instance;

            // 1. Mouse Down
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
            {
                var hitElem = CanvasHitTester.HitTestElements(currentEvent.mousePosition, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);
                if (hitElem != null && !hitElem.IsLocked)
                {
                    if (!doc.IsSelected(hitElem.Id))
                    {
                        if (!currentEvent.shift && !currentEvent.control) doc.Select(hitElem.Id, false);
                        else doc.Select(hitElem.Id, true);
                    }

                    _isDragging = true;
                    _dragStartScreen = currentEvent.mousePosition;
                    _dragStartCanvas = coords.ScreenToCanvas(currentEvent.mousePosition, viewportRect, pan, zoom);

                    _initialOffsets.Clear();
                    foreach (var sel in doc.SelectedElements)
                    {
                        if (sel.IsLocked) continue;
                        var r = sel.GetComponent<CuiRectTransformComponent>() ?? new CuiRectTransformComponent();
                        _initialOffsets[sel.Id] = (
                            RustCanvasScaler.ParseVector2(r.OffsetMin, Vector2.zero),
                            RustCanvasScaler.ParseVector2(r.OffsetMax, Vector2.zero)
                        );
                    }

                    currentEvent.Use();
                    return true;
                }
            }

            // 2. Mouse Drag
            if (currentEvent.type == EventType.MouseDrag && _isDragging)
            {
                var curCanvas = coords.ScreenToCanvas(currentEvent.mousePosition, viewportRect, pan, zoom);
                var delta = curCanvas - _dragStartCanvas;

                if (guideSystem != null && guideSystem.SnapToGrid)
                {
                    float gs = guideSystem.GridSize;
                    delta.x = Mathf.Round(delta.x / gs) * gs;
                    delta.y = Mathf.Round(delta.y / gs) * gs;
                }

                foreach (var sel in doc.SelectedElements)
                {
                    if (!_initialOffsets.TryGetValue(sel.Id, out var orig)) continue;
                    var r = sel.GetComponent<CuiRectTransformComponent>();
                    if (r == null) continue;

                    // Note: Rust Y is inverted relative to Canvas GUI Y
                    var newMin = new Vector2(orig.offsetMin.x + delta.x, orig.offsetMin.y - delta.y);
                    var newMax = new Vector2(orig.offsetMax.x + delta.x, orig.offsetMax.y - delta.y);

                    r.OffsetMin = RustCanvasScaler.FormatVector2(newMin, "0.#");
                    r.OffsetMax = RustCanvasScaler.FormatVector2(newMax, "0.#");
                }

                onModified?.Invoke();
                currentEvent.Use();
                return true;
            }

            // 3. Mouse Up
            if (currentEvent.type == EventType.MouseUp && _isDragging)
            {
                _isDragging = false;
                onCommitUndo?.Invoke("Move Element(s)");
                currentEvent.Use();
                return true;
            }

            // 4. Cancel on Escape
            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape && _isDragging)
            {
                _isDragging = false;
                foreach (var sel in doc.SelectedElements)
                {
                    if (!_initialOffsets.TryGetValue(sel.Id, out var orig)) continue;
                    var r = sel.GetComponent<CuiRectTransformComponent>();
                    if (r != null)
                    {
                        r.OffsetMin = RustCanvasScaler.FormatVector2(orig.offsetMin, "0.#");
                        r.OffsetMax = RustCanvasScaler.FormatVector2(orig.offsetMax, "0.#");
                    }
                }
                onModified?.Invoke();
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
            // Move tool uses standard selection bounding box
        }
    }
}
