using UnityEngine;
using UnityEditor;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas
{
    /// <summary>
    /// Real-time diagnostic HUD for debugging canvas viewport boundaries,
    /// clipping, mouse coordinates, active tool, control ID, and hit targets.
    /// </summary>
    public static class CanvasDebugOverlay
    {
        public static bool IsEnabled { get; set; } = false;

        public static void DrawDebugHUD(
            Rect localViewportRect,
            Rect globalViewportRect,
            Vector2 mouseWindowPos,
            Vector2 mouseViewportPos,
            Vector2 mouseCanvasPos,
            Vector2 mouseRustNorm,
            string activeToolName,
            string hoveredElementName,
            int selectedCount,
            int activeControlId,
            EventType eventType,
            float zoom,
            Vector2 pan,
            float canvasWidth,
            float canvasHeight,
            Vector2 windowSize)
        {
            if (!IsEnabled) return;

            // 1. Draw Viewport Boundary (Green / Cyan)
            Handles.BeginGUI();
            Handles.color = new Color(0.2f, 0.95f, 0.4f, 0.95f);
            Handles.DrawPolyLine(
                new Vector3(1, 1, 0),
                new Vector3(localViewportRect.width - 1, 1, 0),
                new Vector3(localViewportRect.width - 1, localViewportRect.height - 1, 0),
                new Vector3(1, localViewportRect.height - 1, 0),
                new Vector3(1, 1, 0)
            );

            // 2. Draw Virtual Rust Canvas Boundary (Orange)
            var coords = RustCanvasCoordinates.Instance;
            var screenCanvas = coords.CanvasToScreen(new Rect(0, 0, canvasWidth, canvasHeight), localViewportRect, pan, zoom);
            Handles.color = new Color(1.0f, 0.55f, 0.15f, 0.95f);
            Handles.DrawPolyLine(
                new Vector3(screenCanvas.xMin, screenCanvas.yMin, 0),
                new Vector3(screenCanvas.xMax, screenCanvas.yMin, 0),
                new Vector3(screenCanvas.xMax, screenCanvas.yMax, 0),
                new Vector3(screenCanvas.xMin, screenCanvas.yMax, 0),
                new Vector3(screenCanvas.xMin, screenCanvas.yMin, 0)
            );
            Handles.EndGUI();

            // 3. Draw HUD Information Box
            var hudRect = new Rect(24, 24, 320, 200);
            EditorGUI.DrawRect(hudRect, new Color(0.08f, 0.09f, 0.12f, 0.94f));

            Handles.BeginGUI();
            Handles.color = new Color(0.2f, 0.8f, 1.0f, 0.85f);
            Handles.DrawPolyLine(
                new Vector3(hudRect.xMin, hudRect.yMin, 0),
                new Vector3(hudRect.xMax, hudRect.yMin, 0),
                new Vector3(hudRect.xMax, hudRect.yMax, 0),
                new Vector3(hudRect.xMin, hudRect.yMax, 0),
                new Vector3(hudRect.xMin, hudRect.yMin, 0)
            );
            Handles.EndGUI();

            GUILayout.BeginArea(hudRect);
            EditorGUILayout.LabelField("🔧 Canvas Viewport Debug HUD", EditorStyles.boldLabel);

            EditorGUILayout.LabelField($"Window Size: {windowSize.x:0} × {windowSize.y:0}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Canvas Viewport: [{globalViewportRect.x:0}, {globalViewportRect.y:0}, {globalViewportRect.width:0}, {globalViewportRect.height:0}]", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Rust Canvas Design: {canvasWidth:0} × {canvasHeight:0}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Zoom: {zoom * 100:0}% | Pan: ({pan.x:0}, {pan.y:0})", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Mouse Window: ({mouseWindowPos.x:0.0}, {mouseWindowPos.y:0.0})", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Mouse Viewport: ({mouseViewportPos.x:0.0}, {mouseViewportPos.y:0.0})", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Mouse Canvas: ({mouseCanvasPos.x:0.0}, {mouseCanvasPos.y:0.0})", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Rust Norm: ({mouseRustNorm.x:0.000}, {mouseRustNorm.y:0.000})", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Active Tool: {activeToolName} | Hover: {(string.IsNullOrEmpty(hoveredElementName) ? "<None>" : hoveredElementName)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Selected: {selectedCount} | Event: {eventType}", EditorStyles.miniLabel);

            GUILayout.EndArea();
        }
    }
}
