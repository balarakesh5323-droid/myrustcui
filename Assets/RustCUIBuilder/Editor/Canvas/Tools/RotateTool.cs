using System;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas.Tools
{
    /// <summary>
    /// Interactive element rotation tool around its pivot point.
    /// Maps directly to verified CuiRectTransformComponent.Rotation property.
    /// </summary>
    public class RotateTool : ICanvasTool
    {
        public CanvasToolMode ToolMode => CanvasToolMode.Rotate;
        public string ToolName => "Rotate";

        private bool _isRotating;
        private float _initialRotation;
        private float _initialAngle;

        public void OnToolActivate() { _isRotating = false; }
        public void OnToolDeactivate() { _isRotating = false; }

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
            var pivotScreen = coords.GetPivotScreenPoint(primary, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);

            // 1. Mouse Down
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
            {
                var diff = currentEvent.mousePosition - pivotScreen;
                _isRotating = true;
                _initialRotation = rectComp.Rotation;
                _initialAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

                currentEvent.Use();
                return true;
            }

            // 2. Mouse Drag
            if (currentEvent.type == EventType.MouseDrag && _isRotating)
            {
                var diff = currentEvent.mousePosition - pivotScreen;
                float currentAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                float deltaAngle = currentAngle - _initialAngle;

                float newRot = _initialRotation + deltaAngle;
                if (currentEvent.shift)
                {
                    // Snap to 15 degree increments
                    newRot = Mathf.Round(newRot / 15f) * 15f;
                }

                rectComp.Rotation = (newRot % 360f + 360f) % 360f;
                onModified?.Invoke();
                currentEvent.Use();
                return true;
            }

            // 3. Mouse Up
            if (currentEvent.type == EventType.MouseUp && _isRotating)
            {
                _isRotating = false;
                onCommitUndo?.Invoke($"Rotate {primary.Name}");
                currentEvent.Use();
                return true;
            }

            // 4. Cancel on Escape
            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Escape && _isRotating)
            {
                _isRotating = false;
                rectComp.Rotation = _initialRotation;
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
            var primary = doc?.PrimarySelectedElement;
            if (primary == null || primary.IsHidden) return;

            var coords = RustCanvasCoordinates.Instance;
            var pivotScreen = coords.GetPivotScreenPoint(primary, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);
            var elemScreenRect = coords.GetElementScreenRect(primary, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);

            Handles.BeginGUI();
            Handles.color = new Color(1f, 0.7f, 0.2f, 0.8f);
            Handles.DrawWireDisc(pivotScreen, Vector3.forward, Mathf.Max(elemScreenRect.width, elemScreenRect.height) * 0.65f);
            Handles.EndGUI();
        }
    }
}
