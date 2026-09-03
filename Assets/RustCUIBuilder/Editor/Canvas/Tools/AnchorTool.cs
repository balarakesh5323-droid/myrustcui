using System;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;
using RustCUIBuilder.Runtime.Discovery;

namespace RustCUIBuilder.Editor.Canvas.Tools
{
    /// <summary>
    /// Interactive anchor manipulation tool. Exposes 4 anchor pins (AnchorMin / AnchorMax)
    /// allowing visual dragging of anchor points relative to the parent bounding box.
    /// </summary>
    public class AnchorTool : ICanvasTool
    {
        public CanvasToolMode ToolMode => CanvasToolMode.Anchor;
        public string ToolName => "Anchors";

        private bool _isDraggingAnchor;
        public bool IsDragging => _isDraggingAnchor;
        public bool IsInteracting => _isDraggingAnchor;
        private HandleHitType _activeAnchorPin = HandleHitType.None;
        private Vector2 _initialAnchorMin;
        private Vector2 _initialAnchorMax;

        public void OnToolActivate() { _isDraggingAnchor = false; }
        public void OnToolDeactivate() { _isDraggingAnchor = false; }

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
            if (doc == null) return false;
            if (!viewportRect.Contains(currentEvent.mousePosition) && !_isDraggingAnchor) return false;

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
                    true, false);

                if (hit.HitType >= HandleHitType.AnchorNW && hit.HitType <= HandleHitType.AnchorSE)
                {
                    _isDraggingAnchor = true;
                    _activeAnchorPin = hit.HitType;
                    _initialAnchorMin = RustCanvasScaler.ParseVector2(rectComp.AnchorMin, Vector2.zero);
                    _initialAnchorMax = RustCanvasScaler.ParseVector2(rectComp.AnchorMax, Vector2.one);

                    currentEvent.Use();
                    return true;
                }
            }

            // 2. Mouse Drag
            if (currentEvent.type == EventType.MouseDrag && _isDraggingAnchor)
            {
                Rect parentRect = new Rect(0, 0, canvasWidth, canvasHeight);
                if (!string.IsNullOrEmpty(primary.Parent) && Array.IndexOf(RustAssetDiscovery.VerifiedLayers, primary.Parent) < 0)
                {
                    var parentElem = doc.FindByName(primary.Parent);
                    if (parentElem != null) parentRect = coords.GetElementCanvasRect(parentElem, doc, canvasWidth, canvasHeight);
                }

                var mouseCanvas = coords.ScreenToCanvas(currentEvent.mousePosition, viewportRect, pan, zoom);
                float normX = parentRect.width > 0 ? Mathf.Clamp01((mouseCanvas.x - parentRect.x) / parentRect.width) : 0f;
                float normY = parentRect.height > 0 ? Mathf.Clamp01(1.0f - ((mouseCanvas.y - parentRect.y) / parentRect.height)) : 0f;

                if (currentEvent.control)
                {
                    normX = Mathf.Round(normX * 20f) / 20f;
                    normY = Mathf.Round(normY * 20f) / 20f;
                }

                var curMin = _initialAnchorMin;
                var curMax = _initialAnchorMax;

                switch (_activeAnchorPin)
                {
                    case HandleHitType.AnchorNW:
                        curMin.x = normX;
                        curMax.y = normY;
                        break;
                    case HandleHitType.AnchorNE:
                        curMax.x = normX;
                        curMax.y = normY;
                        break;
                    case HandleHitType.AnchorSW:
                        curMin.x = normX;
                        curMin.y = normY;
                        break;
                    case HandleHitType.AnchorSE:
                        curMax.x = normX;
                        curMin.y = normY;
                        break;
                }

                rectComp.AnchorMin = $"{curMin.x:0.###} {curMin.y:0.###}";
                rectComp.AnchorMax = $"{curMax.x:0.###} {curMax.y:0.###}";

                onModified?.Invoke();
                currentEvent.Use();
                return true;
            }

            // 3. Mouse Up
            if (currentEvent.type == EventType.MouseUp && _isDraggingAnchor)
            {
                _isDraggingAnchor = false;
                onCommitUndo?.Invoke($"Adjust Anchors for {primary.Name}");
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
            var anchorPoints = coords.GetAnchorScreenPoints(primary, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);

            if (anchorPoints.Length == 4)
            {
                Handles.BeginGUI();
                Handles.color = new Color(0.95f, 0.4f, 0.2f, 0.9f); // Rust Orange

                // Draw dashed lines between anchors
                Handles.DrawDottedLine(anchorPoints[0], anchorPoints[1], 3f);
                Handles.DrawDottedLine(anchorPoints[1], anchorPoints[3], 3f);
                Handles.DrawDottedLine(anchorPoints[3], anchorPoints[2], 3f);
                Handles.DrawDottedLine(anchorPoints[2], anchorPoints[0], 3f);

                // Draw Anchor Pin Triangles
                foreach (var pt in anchorPoints)
                {
                    DrawAnchorPin(pt);
                }
                Handles.EndGUI();
            }
        }

        private void DrawAnchorPin(Vector2 pt)
        {
            float s = 6f;
            Vector3[] triangle = new[]
            {
                new Vector3(pt.x, pt.y - s, 0),
                new Vector3(pt.x + s, pt.y + s, 0),
                new Vector3(pt.x - s, pt.y + s, 0),
                new Vector3(pt.x, pt.y - s, 0)
            };
            Handles.DrawPolyLine(triangle);
        }
    }
}
