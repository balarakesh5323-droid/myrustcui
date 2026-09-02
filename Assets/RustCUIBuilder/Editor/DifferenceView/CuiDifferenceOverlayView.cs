using System;
using RustCUIBuilder.Runtime.Core.Models;
using UnityEditor;
using UnityEngine;

namespace RustCUIBuilder.Editor.DifferenceView
{
    /// <summary>
    /// Visual difference and screenshot overlay view.
    /// Allows developers to overlay reference game screenshots with variable opacity
    /// or side-by-side mode to reproduce in-game Rust UI with pixel perfection.
    /// </summary>
    public class CuiDifferenceOverlayView
    {
        public bool IsEnabled { get; set; } = false;
        public float Opacity { get; set; } = 0.5f;
        public Texture2D ReferenceImage { get; set; }

        public void DrawCanvasOverlay(Rect screenRect)
        {
            if (!IsEnabled || ReferenceImage == null) return;

            var prevColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Opacity);
            GUI.DrawTexture(screenRect, ReferenceImage, ScaleMode.StretchToFill);
            GUI.color = prevColor;
        }

        public void DrawToolbarControls()
        {
            EditorGUILayout.BeginHorizontal();
            IsEnabled = GUILayout.Toggle(IsEnabled, "Diff Overlay", EditorStyles.toolbarButton, GUILayout.Width(80));
            if (IsEnabled)
            {
                ReferenceImage = (Texture2D)EditorGUILayout.ObjectField(ReferenceImage, typeof(Texture2D), false, GUILayout.Width(100));
                GUILayout.Label("Opacity:", EditorStyles.miniLabel, GUILayout.Width(45));
                Opacity = GUILayout.HorizontalSlider(Opacity, 0.05f, 0.95f, GUILayout.Width(70));
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
