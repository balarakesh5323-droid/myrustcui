using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;

namespace RustCUIBuilder.Editor.Canvas.Tools
{
    /// <summary>
    /// Master controller for all interactive canvas tools.
    /// Manages active tool state, hotkey switching, and event delegation.
    /// </summary>
    public class CanvasToolController
    {
        private readonly Dictionary<CanvasToolMode, ICanvasTool> _tools = new Dictionary<CanvasToolMode, ICanvasTool>();
        private CanvasToolMode _activeMode = CanvasToolMode.Select;

        public CanvasToolMode ActiveMode
        {
            get => _activeMode;
            set
            {
                if (_activeMode != value)
                {
                    if (_tools.TryGetValue(_activeMode, out var oldTool)) oldTool.OnToolDeactivate();
                    _activeMode = value;
                    if (_tools.TryGetValue(_activeMode, out var newTool)) newTool.OnToolActivate();
                }
            }
        }

        public ICanvasTool ActiveTool => _tools.TryGetValue(_activeMode, out var tool) ? tool : null;

        public CanvasToolController()
        {
            RegisterTool(new SelectTool());
            RegisterTool(new MoveTool());
            RegisterTool(new ResizeTool());
            RegisterTool(new RotateTool());
            RegisterTool(new AnchorTool());
            RegisterTool(new PivotTool());
        }

        private void RegisterTool(ICanvasTool tool)
        {
            _tools[tool.ToolMode] = tool;
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
            // Handle Tool Hotkeys (Q, W, E, R, T, Y) when canvas has focus and no modifiers are pressed
            if (currentEvent.type == EventType.KeyDown && !currentEvent.control && !currentEvent.alt && !currentEvent.command && !currentEvent.shift)
            {
                if (currentEvent.keyCode == KeyCode.Q) { ActiveMode = CanvasToolMode.Select; currentEvent.Use(); return true; }
                if (currentEvent.keyCode == KeyCode.W) { ActiveMode = CanvasToolMode.Move; currentEvent.Use(); return true; }
                if (currentEvent.keyCode == KeyCode.E) { ActiveMode = CanvasToolMode.Rotate; currentEvent.Use(); return true; }
                if (currentEvent.keyCode == KeyCode.R) { ActiveMode = CanvasToolMode.Resize; currentEvent.Use(); return true; }
                if (currentEvent.keyCode == KeyCode.T) { ActiveMode = CanvasToolMode.Anchor; currentEvent.Use(); return true; }
                if (currentEvent.keyCode == KeyCode.Y) { ActiveMode = CanvasToolMode.Pivot; currentEvent.Use(); return true; }
            }

            // In Select/Move mode, if clicking on a resize handle, dynamically delegate to ResizeTool
            if (_activeMode == CanvasToolMode.Select || _activeMode == CanvasToolMode.Move)
            {
                var primary = doc?.PrimarySelectedElement;
                if (primary != null && !primary.IsLocked && currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
                {
                    var hit = CanvasHitTester.HitTestHandles(
                        currentEvent.mousePosition, primary, doc,
                        viewportRect, pan, zoom, canvasWidth, canvasHeight,
                        false, false);

                    if (hit.HitType >= HandleHitType.NW && hit.HitType <= HandleHitType.W)
                    {
                        if (_tools.TryGetValue(CanvasToolMode.Resize, out var resizer))
                        {
                            return resizer.ProcessEvent(currentEvent, viewportRect, pan, zoom, canvasWidth, canvasHeight, doc, guideSystem, onModified, onCommitUndo);
                        }
                    }
                }
            }

            if (ActiveTool != null)
            {
                return ActiveTool.ProcessEvent(currentEvent, viewportRect, pan, zoom, canvasWidth, canvasHeight, doc, guideSystem, onModified, onCommitUndo);
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
            if (ActiveTool != null)
            {
                ActiveTool.DrawToolOverlay(viewportRect, pan, zoom, canvasWidth, canvasHeight, doc);
            }
        }
    }
}
