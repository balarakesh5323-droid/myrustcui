using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;
using RustCUIBuilder.Editor.Canvas.Services;

namespace RustCUIBuilder.Editor.Canvas.Tools
{
    /// <summary>
    /// Professional 8-handle interactive free resize tool supporting:
    /// 1. Free, unrestricted multi-directional edge and corner resizing.
    /// 2. Single-element and multi-element collective bounding-box scaling.
    /// 3. Shift: Uniform aspect-ratio locking.
    /// 4. Alt: Center-relative symmetrical expansion/contraction.
    /// 5. Snap to Grid & Real-time magnetic smart guide snapping.
    /// 6. Live dimension HUD pills & atomic single-action undo transactions.
    /// </summary>
    public class ResizeTool : ICanvasTool
    {
        public CanvasToolMode ToolMode => CanvasToolMode.Resize;
        public string ToolName => "Resize";

        public bool IsResizing => _isResizing;

        private bool _isResizing;
        private HandleHitType _activeHandle = HandleHitType.None;
        private Vector2 _dragStartScreen;
        private Vector2 _dragStartCanvas;
        private Rect _initialSelectionBounds;
        private float _initialAspectRatio;
        private readonly Dictionary<string, Rect> _initialElementCanvasRects = new Dictionary<string, Rect>();

        public void OnToolActivate() { _isResizing = false; }
        public void OnToolDeactivate()
        {
            _isResizing = false;
            CanvasSnapService.ActiveGuides.Clear();
        }

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

            var selected = doc.SelectedElements.Where(e => !e.IsLocked && !e.IsHidden).ToList();
            if (selected.Count == 0) return false;

            var coords = RustCanvasCoordinates.Instance;

            // 1. Mouse Down
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
            {
                var hit = CanvasHitTester.HitTestSelectionHandles(
                    currentEvent.mousePosition, selected, doc,
                    viewportRect, pan, zoom, canvasWidth, canvasHeight,
                    false, false);

                if (hit.HitType >= HandleHitType.NW && hit.HitType <= HandleHitType.EdgeE)
                {
                    _isResizing = true;
                    _activeHandle = hit.HitType;
                    _dragStartScreen = currentEvent.mousePosition;
                    _dragStartCanvas = coords.ScreenToCanvas(currentEvent.mousePosition, viewportRect, pan, zoom);

                    _initialElementCanvasRects.Clear();
                    float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;

                    foreach (var elem in selected)
                    {
                        var r = coords.GetElementCanvasRect(elem, doc, canvasWidth, canvasHeight);
                        _initialElementCanvasRects[elem.Id] = r;
                        minX = Mathf.Min(minX, r.xMin);
                        minY = Mathf.Min(minY, r.yMin);
                        maxX = Mathf.Max(maxX, r.xMax);
                        maxY = Mathf.Max(maxY, r.yMax);
                    }

                    _initialSelectionBounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
                    _initialAspectRatio = _initialSelectionBounds.height > 0 ? _initialSelectionBounds.width / _initialSelectionBounds.height : 1.0f;

                    currentEvent.Use();
                    return true;
                }
            }

            // 2. Mouse Drag
            if (currentEvent.type == EventType.MouseDrag && _isResizing)
            {
                var curCanvas = coords.ScreenToCanvas(currentEvent.mousePosition, viewportRect, pan, zoom);
                var rawDelta = curCanvas - _dragStartCanvas;

                bool lockAspect = currentEvent.shift;
                bool fromCenter = currentEvent.alt;

                if (guideSystem != null && (guideSystem.SnapToGrid || currentEvent.control))
                {
                    float gs = guideSystem.GridSize;
                    rawDelta.x = Mathf.Round(rawDelta.x / gs) * gs;
                    rawDelta.y = Mathf.Round(rawDelta.y / gs) * gs;
                }

                var newBounds = CalculateResizedRect(_initialSelectionBounds, _activeHandle, rawDelta, lockAspect, fromCenter, _initialAspectRatio);

                // Smart Snap Calculation
                if (guideSystem != null && selected.Count == 1)
                {
                    CanvasSnapService.CalculateSnap(newBounds, selected[0], doc, canvasWidth, canvasHeight, guideSystem, zoom, out var snapDelta);
                    if (snapDelta != Vector2.zero)
                    {
                        newBounds = CalculateResizedRect(_initialSelectionBounds, _activeHandle, rawDelta + snapDelta, lockAspect, fromCenter, _initialAspectRatio);
                    }
                }

                // Minimum size limit (1px)
                if (newBounds.width < 1f) newBounds.width = 1f;
                if (newBounds.height < 1f) newBounds.height = 1f;

                // Apply new bounds to elements
                if (selected.Count == 1)
                {
                    coords.ApplyNewCanvasRectToElementOffsets(newBounds, selected[0], doc, canvasWidth, canvasHeight);
                }
                else if (_initialSelectionBounds.width > 0 && _initialSelectionBounds.height > 0)
                {
                    // Scale all elements relative to initial selection bounding box
                    foreach (var elem in selected)
                    {
                        if (!_initialElementCanvasRects.TryGetValue(elem.Id, out var origRect)) continue;

                        float normMinX = (origRect.xMin - _initialSelectionBounds.xMin) / _initialSelectionBounds.width;
                        float normMaxX = (origRect.xMax - _initialSelectionBounds.xMin) / _initialSelectionBounds.width;
                        float normMinY = (origRect.yMin - _initialSelectionBounds.yMin) / _initialSelectionBounds.height;
                        float normMaxY = (origRect.yMax - _initialSelectionBounds.yMin) / _initialSelectionBounds.height;

                        var scaledElemRect = Rect.MinMaxRect(
                            newBounds.xMin + normMinX * newBounds.width,
                            newBounds.yMin + normMinY * newBounds.height,
                            newBounds.xMin + normMaxX * newBounds.width,
                            newBounds.yMin + normMaxY * newBounds.height
                        );

                        coords.ApplyNewCanvasRectToElementOffsets(scaledElemRect, elem, doc, canvasWidth, canvasHeight);
                    }
                }

                onModified?.Invoke();
                currentEvent.Use();
                return true;
            }

            // 3. Mouse Up
            if (currentEvent.type == EventType.MouseUp && _isResizing)
            {
                _isResizing = false;
                _activeHandle = HandleHitType.None;
                CanvasSnapService.ActiveGuides.Clear();
                onCommitUndo?.Invoke(selected.Count == 1 ? $"Resize {selected[0].Name}" : $"Resize {selected.Count} Elements");
                currentEvent.Use();
                return true;
            }

            // 4. Cancel on Escape
            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape && _isResizing)
            {
                _isResizing = false;
                _activeHandle = HandleHitType.None;
                CanvasSnapService.ActiveGuides.Clear();

                foreach (var elem in selected)
                {
                    if (_initialElementCanvasRects.TryGetValue(elem.Id, out var origRect))
                    {
                        coords.ApplyNewCanvasRectToElementOffsets(origRect, elem, doc, canvasWidth, canvasHeight);
                    }
                }

                onModified?.Invoke();
                currentEvent.Use();
                return true;
            }

            return false;
        }

        private Rect CalculateResizedRect(Rect orig, HandleHitType handle, Vector2 delta, bool lockAspect, bool fromCenter, float aspect)
        {
            float xMin = orig.xMin;
            float xMax = orig.xMax;
            float yMin = orig.yMin;
            float yMax = orig.yMax;

            if (lockAspect && aspect > 0)
            {
                switch (handle)
                {
                    case HandleHitType.NW:
                    case HandleHitType.SE:
                        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) delta.y = delta.x / aspect;
                        else delta.x = delta.y * aspect;
                        break;
                    case HandleHitType.NE:
                    case HandleHitType.SW:
                        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) delta.y = -delta.x / aspect;
                        else delta.x = -delta.y * aspect;
                        break;
                    case HandleHitType.N:
                    case HandleHitType.S:
                    case HandleHitType.EdgeN:
                    case HandleHitType.EdgeS:
                        delta.x = delta.y * aspect;
                        break;
                    case HandleHitType.E:
                    case HandleHitType.W:
                    case HandleHitType.EdgeE:
                    case HandleHitType.EdgeW:
                        delta.y = delta.x / aspect;
                        break;
                }
            }

            switch (handle)
            {
                case HandleHitType.NW:
                    xMin += delta.x;
                    yMin += delta.y;
                    if (fromCenter) { xMax -= delta.x; yMax -= delta.y; }
                    break;
                case HandleHitType.N:
                case HandleHitType.EdgeN:
                    yMin += delta.y;
                    if (fromCenter) yMax -= delta.y;
                    break;
                case HandleHitType.NE:
                    xMax += delta.x;
                    yMin += delta.y;
                    if (fromCenter) { xMin -= delta.x; yMax -= delta.y; }
                    break;
                case HandleHitType.E:
                case HandleHitType.EdgeE:
                    xMax += delta.x;
                    if (fromCenter) xMin -= delta.x;
                    break;
                case HandleHitType.SE:
                    xMax += delta.x;
                    yMax += delta.y;
                    if (fromCenter) { xMin -= delta.x; yMin -= delta.y; }
                    break;
                case HandleHitType.S:
                case HandleHitType.EdgeS:
                    yMax += delta.y;
                    if (fromCenter) yMin -= delta.y;
                    break;
                case HandleHitType.SW:
                    xMin += delta.x;
                    yMax += delta.y;
                    if (fromCenter) { xMax -= delta.x; yMin -= delta.y; }
                    break;
                case HandleHitType.W:
                case HandleHitType.EdgeW:
                    xMin += delta.x;
                    if (fromCenter) xMax -= delta.x;
                    break;
            }

            return Rect.MinMaxRect(
                Mathf.Min(xMin, xMax - 1),
                Mathf.Min(yMin, yMax - 1),
                Mathf.Max(xMin + 1, xMax),
                Mathf.Max(yMin + 1, yMax)
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
            var selected = doc?.SelectedElements.Where(e => !e.IsHidden).ToList();
            if (selected == null || selected.Count == 0) return;

            var coords = RustCanvasCoordinates.Instance;
            var boundsScreenRect = CanvasHitTester.GetSelectionScreenRect(selected, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);

            // 1. Draw Selection Outline
            Handles.BeginGUI();
            Handles.color = new Color(0.2f, 0.75f, 1.0f, 0.95f);
            Handles.DrawPolyLine(
                new Vector3(boundsScreenRect.xMin, boundsScreenRect.yMin, 0),
                new Vector3(boundsScreenRect.xMax, boundsScreenRect.yMin, 0),
                new Vector3(boundsScreenRect.xMax, boundsScreenRect.yMax, 0),
                new Vector3(boundsScreenRect.xMin, boundsScreenRect.yMax, 0),
                new Vector3(boundsScreenRect.xMin, boundsScreenRect.yMin, 0)
            );
            Handles.EndGUI();

            // 2. Draw 8-point resize handles
            DrawHandle(new Vector2(boundsScreenRect.xMin, boundsScreenRect.yMin));
            DrawHandle(new Vector2(boundsScreenRect.center.x, boundsScreenRect.yMin));
            DrawHandle(new Vector2(boundsScreenRect.xMax, boundsScreenRect.yMin));
            DrawHandle(new Vector2(boundsScreenRect.xMax, boundsScreenRect.center.y));
            DrawHandle(new Vector2(boundsScreenRect.xMax, boundsScreenRect.yMax));
            DrawHandle(new Vector2(boundsScreenRect.center.x, boundsScreenRect.yMax));
            DrawHandle(new Vector2(boundsScreenRect.xMin, boundsScreenRect.yMax));
            DrawHandle(new Vector2(boundsScreenRect.xMin, boundsScreenRect.center.y));

            // 3. Register Interactive Mouse Cursor Rects
            CanvasHitTester.AddResizeCursorRects(boundsScreenRect);

            // 4. Live Dimension HUD during active resizing
            if (_isResizing)
            {
                var curCanvasBounds = coords.ScreenToCanvas(boundsScreenRect, viewportRect, pan, zoom);
                float dw = curCanvasBounds.width - _initialSelectionBounds.width;
                float dh = curCanvasBounds.height - _initialSelectionBounds.height;

                string label = $"{curCanvasBounds.width:0.#} × {curCanvasBounds.height:0.#} px (Δ {dw:+0.#;-0.#;0}, {dh:+0.#;-0.#;0})";
                var labelSize = EditorStyles.miniBoldLabel.CalcSize(new GUIContent(label));
                var hudRect = new Rect(boundsScreenRect.center.x - labelSize.x / 2 - 8, boundsScreenRect.yMax + 10, labelSize.x + 16, 20);

                EditorGUI.DrawRect(hudRect, new Color(0.12f, 0.14f, 0.18f, 0.92f));
                Handles.BeginGUI();
                Handles.color = new Color(0.2f, 0.75f, 1.0f, 0.8f);
                Handles.DrawPolyLine(
                    new Vector3(hudRect.xMin, hudRect.yMin, 0), new Vector3(hudRect.xMax, hudRect.yMin, 0),
                    new Vector3(hudRect.xMax, hudRect.yMax, 0), new Vector3(hudRect.xMin, hudRect.yMax, 0),
                    new Vector3(hudRect.xMin, hudRect.yMin, 0)
                );
                Handles.EndGUI();

                var hudStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.9f, 0.95f, 1f, 1f) }
                };
                GUI.Label(hudRect, label, hudStyle);

                // Smart guides
                CanvasSnapService.DrawActiveGuides(viewportRect, pan, zoom);
            }
        }

        private void DrawHandle(Vector2 center)
        {
            float hs = 8f;
            var r = new Rect(center.x - hs / 2, center.y - hs / 2, hs, hs);
            EditorGUI.DrawRect(r, Color.white);
            Handles.BeginGUI();
            Handles.color = new Color(0.1f, 0.55f, 1.0f, 1f);
            Handles.DrawPolyLine(
                new Vector3(r.xMin, r.yMin, 0), new Vector3(r.xMax, r.yMin, 0),
                new Vector3(r.xMax, r.yMax, 0), new Vector3(r.xMin, r.yMax, 0),
                new Vector3(r.xMin, r.yMin, 0)
            );
            Handles.EndGUI();
        }
    }
}
