using System;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;

namespace RustCUIBuilder.Editor.Canvas.Tools
{
    public enum CanvasToolMode
    {
        Select,
        Move,
        Resize,
        Rotate,
        Anchor,
        Pivot,
        Pan
    }

    /// <summary>
    /// Contract for interactive canvas tools in Rust CUI Builder.
    /// Manages event processing, hit testing, handle manipulation, and model updates.
    /// </summary>
    public interface ICanvasTool
    {
        CanvasToolMode ToolMode { get; }
        string ToolName { get; }
        bool IsInteracting { get; }

        void OnToolActivate();
        void OnToolDeactivate();

        bool ProcessEvent(
            Event currentEvent,
            Rect viewportRect,
            Vector2 pan,
            float zoom,
            float canvasWidth,
            float canvasHeight,
            CuiDocument doc,
            CanvasGuideSystem guideSystem,
            Action onModified,
            Action<string> onCommitUndo);

        void DrawToolOverlay(
            Rect viewportRect,
            Vector2 pan,
            float zoom,
            float canvasWidth,
            float canvasHeight,
            CuiDocument doc);
    }
}
