using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;

namespace RustCUIBuilder.Editor.Canvas.Tools
{
    /// <summary>
    /// Master controller for all interactive canvas tools.
    /// Manages active tool state, hotkey switching, unified transform handle rendering,
    /// and seamless event delegation between Select, Move, and Resize workflows.
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
            // 1. If ResizeTool is actively dragging/resizing, keep routing directly to it
            if (_tools.TryGetValue(CanvasToolMode.Resize, out var resizerTool) && resizerTool is ResizeTool resizer && resizer.IsResizing)
            {
                return resizer.ProcessEvent(currentEvent, viewportRect, pan, zoom, canvasWidth, canvasHeight, doc, guideSystem, onModified, onCommitUndo);
            }

            // 2. Handle Tool Hotkeys (Q, W, E, R, T, Y) when no modifiers are pressed
            if (currentEvent.type == EventType.KeyDown && !currentEvent.control && !currentEvent.alt && !currentEvent.command && !currentEvent.shift)
            {
                if (currentEvent.keyCode == KeyCode.Q) { ActiveMode = CanvasToolMode.Select; currentEvent.Use(); return true; }
                if (currentEvent.keyCode == KeyCode.W) { ActiveMode = CanvasToolMode.Move; currentEvent.Use(); return true; }
                if (currentEvent.keyCode == KeyCode.E) { ActiveMode = CanvasToolMode.Rotate; currentEvent.Use(); return true; }
                if (currentEvent.keyCode == KeyCode.R) { ActiveMode = CanvasToolMode.Resize; currentEvent.Use(); return true; }
                if (currentEvent.keyCode == KeyCode.T) { ActiveMode = CanvasToolMode.Anchor; currentEvent.Use(); return true; }
                if (currentEvent.keyCode == KeyCode.Y) { ActiveMode = CanvasToolMode.Pivot; currentEvent.Use(); return true; }
            }

            // 3. Seamless Resize Handle Interception in Select and Move modes
            if (_activeMode == CanvasToolMode.Select || _activeMode == CanvasToolMode.Move)
            {
                var selected = doc?.SelectedElements.Where(e => !e.IsLocked && !e.IsHidden).ToList();
                if (selected != null && selected.Count > 0 && currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
                {
                    var hit = CanvasHitTester.HitTestSelectionHandles(
                        currentEvent.mousePosition, selected, doc,
                        viewportRect, pan, zoom, canvasWidth, canvasHeight,
                        false, false);

                    if (hit.HitType >= HandleHitType.NW && hit.HitType <= HandleHitType.EdgeE)
                    {
                        if (_tools.TryGetValue(CanvasToolMode.Resize, out var resizeInstance))
                        {
                            return resizeInstance.ProcessEvent(currentEvent, viewportRect, pan, zoom, canvasWidth, canvasHeight, doc, guideSystem, onModified, onCommitUndo);
                        }
                    }
                }
            }

            // 4. Dispatch to Active Tool
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
            if (_activeMode == CanvasToolMode.Select || _activeMode == CanvasToolMode.Move || _activeMode == CanvasToolMode.Resize)
            {
                // Draw selection marquee if SelectTool is active and dragging
                if (_activeMode == CanvasToolMode.Select && _tools.TryGetValue(CanvasToolMode.Select, out var selectTool))
                {
                    selectTool.DrawToolOverlay(viewportRect, pan, zoom, canvasWidth, canvasHeight, doc);
                }

                // Draw Move overlays (snap guides, dimension HUD) if MoveTool is active and dragging
                if (_activeMode == CanvasToolMode.Move && _tools.TryGetValue(CanvasToolMode.Move, out var moveTool))
                {
                    moveTool.DrawToolOverlay(viewportRect, pan, zoom, canvasWidth, canvasHeight, doc);
                }

                // Draw standard 8-handle transform gizmo and cursor rects on selected elements
                if (_tools.TryGetValue(CanvasToolMode.Resize, out var resizer))
                {
                    resizer.DrawToolOverlay(viewportRect, pan, zoom, canvasWidth, canvasHeight, doc);
                }
            }
            else if (ActiveTool != null)
            {
                ActiveTool.DrawToolOverlay(viewportRect, pan, zoom, canvasWidth, canvasHeight, doc);
            }
        }
    }
}
