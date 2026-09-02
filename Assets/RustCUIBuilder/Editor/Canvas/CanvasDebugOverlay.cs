using UnityEngine;
using UnityEditor;
using RustCUIBuilder.Runtime.Core.Models;

namespace RustCUIBuilder.Editor.Canvas
{
    /// <summary>
    /// Real-time diagnostic HUD for debugging canvas interaction,
    /// mouse coordinates, active tool, control ID, and hit targets.
    /// </summary>
    public static class CanvasDebugOverlay
    {
        public static bool IsEnabled { get; set; } = false;

        public static void DrawDebugHUD(
            Rect viewRect,
            Vector2 mouseScreenPos,
            Vector2 mouseCanvasPos,
            Vector2 mouseRustNorm,
            string activeToolName,
            string hoveredElementName,
            int selectedCount,
            int activeControlId,
            EventType eventType,
            float zoom,
            Vector2 pan)
        {
            if (!IsEnabled) return;

            var hudRect = new Rect(viewRect.x + 24, viewRect.y + 24, 300, 160);
            EditorGUI.DrawRect(hudRect, new Color(0.08f, 0.09f, 0.12f, 0.92f));

            Handles.BeginGUI();
            Handles.color = new Color(0.2f, 0.8f, 1.0f, 0.8f);
            Handles.DrawPolyLine(
                new Vector3(hudRect.xMin, hudRect.yMin, 0),
                new Vector3(hudRect.xMax, hudRect.yMin, 0),
                new Vector3(hudRect.xMax, hudRect.yMax, 0),
                new Vector3(hudRect.xMin, hudRect.yMax, 0),
                new Vector3(hudRect.xMin, hudRect.yMin, 0)
            );
            Handles.EndGUI();

            GUILayout.BeginArea(hudRect);
            EditorGUILayout.LabelField("🔧 Canvas Interaction Debug", EditorStyles.boldLabel);

            EditorGUILayout.LabelField($"Screen Mouse: {mouseScreenPos.x:0.0}, {mouseScreenPos.y:0.0}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Canvas Mouse: {mouseCanvasPos.x:0.0}, {mouseCanvasPos.y:0.0}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Rust Norm: ({mouseRustNorm.x:0.000}, {mouseRustNorm.y:0.000})", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Zoom: {zoom * 100:0}% | Pan: ({pan.x:0}, {pan.y:0})", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Active Tool: {activeToolName}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Hovered: {(string.IsNullOrEmpty(hoveredElementName) ? "<None>" : hoveredElementName)}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Selected Elements: {selectedCount} | Ctrl ID: {activeControlId}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Event: {eventType}", EditorStyles.miniLabel);

            GUILayout.EndArea();
        }
    }
}
