using System;
using System.Collections.Generic;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas.Services
{
    /// <summary>
    /// Master keyboard nudge service.
    /// Supports precision pixel movements: Standard 1px, Shift 10px, Alt 0.1px.
    /// </summary>
    public static class CanvasNudgeService
    {
        public static bool ProcessNudgeEvent(
            Event currentEvent,
            CuiDocument doc,
            float canvasW,
            float canvasH,
            Action onModified,
            Action<string> onCommitUndo)
        {
            if (doc == null || doc.SelectedElements.Count == 0) return false;
            if (currentEvent.type != EventType.KeyDown) return false;

            Vector2 dir = Vector2.zero;
            switch (currentEvent.keyCode)
            {
                case KeyCode.LeftArrow: dir = new Vector2(-1f, 0f); break;
                case KeyCode.RightArrow: dir = new Vector2(1f, 0f); break;
                case KeyCode.UpArrow: dir = new Vector2(0f, -1f); break;
                case KeyCode.DownArrow: dir = new Vector2(0f, 1f); break;
                default: return false;
            }

            float step = 1f;
            if (currentEvent.shift) step = 10f;
            else if (currentEvent.alt) step = 0.1f;

            Vector2 deltaCanvas = dir * step;
            var coords = RustCanvasCoordinates.Instance;

            foreach (var elem in doc.SelectedElements)
            {
                if (elem.IsLocked) continue;
                var r = coords.GetElementCanvasRect(elem, doc, canvasW, canvasH);
                var newR = new Rect(r.x + deltaCanvas.x, r.y + deltaCanvas.y, r.width, r.height);
                coords.ApplyNewCanvasRectToElementOffsets(newR, elem, doc, canvasW, canvasH);
            }

            onCommitUndo?.Invoke($"Nudge {step}px");
            onModified?.Invoke();
            currentEvent.Use();
            return true;
        }
    }
}
