using System;
using System.Collections.Generic;
using System.Linq;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Discovery;
using RustCUIBuilder.Runtime.Rendering.Canvas;
using UnityEditor;
using UnityEngine;

namespace RustCUIBuilder.Editor.Canvas
{
    public enum CanvasBackgroundMode
    {
        DarkGrid,
        RustInGame1,
        RustInGame2
    }

    /// <summary>
    /// Professional interactive visual 2D canvas editor for Rust CUI Builder.
    /// Supports pan, zoom, grid, snapping, pixel rulers, coordinate tooltips,
    /// in-game background preview, and visual bounding-box / anchor handle manipulation.
    /// </summary>
    public class CuiCanvasEditorView
    {
        private Vector2 _panOffset = new Vector2(80f, 60f);
        private float _zoom = 0.55f;
        private bool _isPanning;
        private Vector2 _lastMousePos;

        public bool SnapToGrid { get; set; } = true;
        public int GridSize { get; set; } = 16;
        public bool ShowRulers { get; set; } = true;
        public bool ShowAnchors { get; set; } = true;
        public bool ShowGuides { get; set; } = true;
        public CanvasBackgroundMode BackgroundMode { get; set; } = CanvasBackgroundMode.DarkGrid;

        public RustResolutionPreset CurrentPreset { get; set; } = RustResolutionPreset.Presets[3]; // 1920x1080 default

        private string _draggingElementId;
        private DragHandleType _currentDragHandle = DragHandleType.None;
        private Vector2 _dragStartMousePos;
        private Vector2 _dragStartOffsetMin;
        private Vector2 _dragStartOffsetMax;

        private enum DragHandleType
        {
            None,
            Body,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,
            AnchorMin,
            AnchorMax
        }

        public void Draw(Rect viewRect, CuiDocument doc, Action onModified)
        {
            // Dark viewport background
            EditorGUI.DrawRect(viewRect, new Color(0.11f, 0.12f, 0.14f, 1f));

            HandleInput(viewRect, doc, onModified);

            // Calculate simulated screen rectangle
            float screenW = CurrentPreset.Width * _zoom;
            float screenH = CurrentPreset.Height * _zoom;
            var screenRect = new Rect(viewRect.x + _panOffset.x, viewRect.y + _panOffset.y, screenW, screenH);

            // Draw Rust Game Screen Frame & In-game Background
            DrawScreenFrame(screenRect);

            // Draw Grid inside screen bounds
            if (BackgroundMode == CanvasBackgroundMode.DarkGrid)
            {
                DrawGrid(screenRect);
            }

            // Draw Elements from Document (sorted root first)
            if (doc != null && doc.Elements != null)
            {
                foreach (var elem in doc.Elements)
                {
                    DrawElement(screenRect, elem, doc, onModified);
                }
            }

            // Draw Rulers
            if (ShowRulers)
            {
                DrawRulers(viewRect, screenRect);
            }

            // Draw Canvas Controls Overlay (Bottom Right)
            DrawCanvasToolbar(viewRect);
        }

        private void HandleInput(Rect viewRect, CuiDocument doc, Action onModified)
        {
            var e = Event.current;
            if (!viewRect.Contains(e.mousePosition)) return;

            // Pan: Middle mouse button or Alt + Left click
            if (e.type == EventType.MouseDown && (e.button == 2 || (e.button == 0 && e.alt)))
            {
                _isPanning = true;
                _lastMousePos = e.mousePosition;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _isPanning)
            {
                _panOffset += e.mousePosition - _lastMousePos;
                _lastMousePos = e.mousePosition;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _isPanning)
            {
                _isPanning = false;
                e.Use();
            }

            // Zoom: Scroll wheel
            if (e.type == EventType.ScrollWheel)
            {
                float zoomDelta = -e.delta.y * 0.04f;
                float oldZoom = _zoom;
                _zoom = Mathf.Clamp(_zoom + zoomDelta, 0.15f, 3.0f);

                // Zoom toward mouse position
                var mouseRel = e.mousePosition - (viewRect.position + _panOffset);
                _panOffset -= mouseRel * (_zoom / oldZoom - 1f);

                e.Use();
            }

            // Drag handling release
            if (e.type == EventType.MouseUp && _draggingElementId != null)
            {
                _draggingElementId = null;
                _currentDragHandle = DragHandleType.None;
                onModified?.Invoke();
                e.Use();
            }
        }

        private void DrawGrid(Rect screenRect)
        {
            Handles.BeginGUI();
            Color gridColor = new Color(0.22f, 0.24f, 0.28f, 0.35f);
            float step = GridSize * _zoom;
            if (step < 6f) step *= 4f;

            Handles.color = gridColor;

            for (float x = screenRect.x; x <= screenRect.xMax; x += step)
            {
                Handles.DrawLine(new Vector3(x, screenRect.y, 0), new Vector3(x, screenRect.yMax, 0));
            }
            for (float y = screenRect.y; y <= screenRect.yMax; y += step)
            {
                Handles.DrawLine(new Vector3(screenRect.x, y, 0), new Vector3(screenRect.xMax, y, 0));
            }

            Handles.EndGUI();
        }

        private void DrawScreenFrame(Rect screenRect)
        {
            if (BackgroundMode == CanvasBackgroundMode.RustInGame1)
            {
                var bg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RustCUIBuilder/Resources/Backgrounds/RustBackground1.jpg");
                if (bg != null) GUI.DrawTexture(screenRect, bg, ScaleMode.ScaleAndCrop);
                else EditorGUI.DrawRect(screenRect, new Color(0.06f, 0.07f, 0.09f, 0.95f));
            }
            else if (BackgroundMode == CanvasBackgroundMode.RustInGame2)
            {
                var bg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RustCUIBuilder/Resources/Backgrounds/RustBackground2.jpg");
                if (bg != null) GUI.DrawTexture(screenRect, bg, ScaleMode.ScaleAndCrop);
                else EditorGUI.DrawRect(screenRect, new Color(0.06f, 0.07f, 0.09f, 0.95f));
            }
            else
            {
                EditorGUI.DrawRect(screenRect, new Color(0.06f, 0.07f, 0.09f, 0.98f));
            }

            // Outer border with Rust orange accent
            Handles.BeginGUI();
            Handles.color = new Color(0.85f, 0.35f, 0.15f, 0.9f);
            Handles.DrawPolyLine(
                new Vector3(screenRect.xMin, screenRect.yMin, 0),
                new Vector3(screenRect.xMax, screenRect.yMin, 0),
                new Vector3(screenRect.xMax, screenRect.yMax, 0),
                new Vector3(screenRect.xMin, screenRect.yMax, 0),
                new Vector3(screenRect.xMin, screenRect.yMin, 0)
            );
            Handles.EndGUI();

            // Resolution badge
            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                normal = { textColor = new Color(0.85f, 0.85f, 0.9f, 0.8f) }
            };
            GUI.Label(new Rect(screenRect.x + 8, screenRect.y + 6, 320, 18), $"{CurrentPreset.Name} ({CurrentPreset.Width}x{CurrentPreset.Height})", labelStyle);
        }

        public static Rect ComputeElementRect(CuiElementNode elem, CuiDocument doc, Rect screenRect, float zoom)
        {
            if (elem == null) return screenRect;

            Rect parentRect = screenRect;
            if (!string.IsNullOrEmpty(elem.Parent) && Array.IndexOf(RustAssetDiscovery.VerifiedLayers, elem.Parent) < 0)
            {
                var parentElem = doc.FindByName(elem.Parent);
                if (parentElem != null && parentElem != elem)
                {
                    parentRect = ComputeElementRect(parentElem, doc, screenRect, zoom);
                }
            }

            var rectComp = elem.GetComponent<CuiRectTransformComponent>() ?? new CuiRectTransformComponent();
            Vector2 anchorMin = RustCanvasScaler.ParseVector2(rectComp.AnchorMin, Vector2.zero);
            Vector2 anchorMax = RustCanvasScaler.ParseVector2(rectComp.AnchorMax, Vector2.one);
            Vector2 offsetMin = RustCanvasScaler.ParseVector2(rectComp.OffsetMin, Vector2.zero);
            Vector2 offsetMax = RustCanvasScaler.ParseVector2(rectComp.OffsetMax, Vector2.zero);

            float xMin = parentRect.x + parentRect.width * anchorMin.x + offsetMin.x * zoom;
            float xMax = parentRect.x + parentRect.width * anchorMax.x + offsetMax.x * zoom;
            float yMin = parentRect.y + parentRect.height * (1f - anchorMax.y) - offsetMax.y * zoom;
            float yMax = parentRect.y + parentRect.height * (1f - anchorMin.y) - offsetMin.y * zoom;

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private void DrawElement(Rect screenRect, CuiElementNode elem, CuiDocument doc, Action onModified)
        {
            var elemScreenRect = ComputeElementRect(elem, doc, screenRect, _zoom);

            // Element Graphics
            var img = elem.GetComponent<CuiImageComponent>();
            var raw = elem.GetComponent<CuiRawImageComponent>();
            var text = elem.GetComponent<CuiTextComponent>();
            var btn = elem.GetComponent<CuiButtonComponent>();

            Color fillColor = new Color(0.2f, 0.2f, 0.25f, 0.4f);
            Sprite elemSprite = null;

            if (img != null)
            {
                fillColor = CuiColorExtensions.ToUnityColor(img.Color, Color.white);
                if (img.ItemId != 0)
                {
                    var item = RustAssetDiscovery.FindItemById(img.ItemId);
                    if (item != null) elemSprite = RustAssetDiscovery.LoadItemIcon(item);
                }
                else if (!string.IsNullOrEmpty(img.Sprite))
                {
                    elemSprite = RustAssetDiscovery.GetSpriteByPath(img.Sprite);
                }
            }
            else if (btn != null)
            {
                fillColor = CuiColorExtensions.ToUnityColor(btn.Color, new Color(0.2f, 0.5f, 0.3f, 0.8f));
                if (!string.IsNullOrEmpty(btn.Material))
                {
                    elemSprite = RustAssetDiscovery.GetSpriteByPath(btn.Material);
                }
            }
            else if (raw != null)
            {
                fillColor = CuiColorExtensions.ToUnityColor(raw.Color, fillColor);
            }

            if (elemSprite != null && elemSprite.texture != null)
            {
                var prevCol = GUI.color;
                GUI.color = fillColor;
                GUI.DrawTexture(elemScreenRect, elemSprite.texture, ScaleMode.ScaleToFit);
                GUI.color = prevCol;
            }
            else
            {
                EditorGUI.DrawRect(elemScreenRect, fillColor);
            }

            // Element Text
            if (text != null && !string.IsNullOrEmpty(text.Text))
            {
                var textStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = Mathf.Max(9, Mathf.RoundToInt(text.FontSize * _zoom)),
                    normal = { textColor = CuiColorExtensions.ToUnityColor(text.Color, Color.white) },
                    alignment = text.Align
                };
                GUI.Label(elemScreenRect, text.Text, textStyle);
            }

            // Selection Outline and Gizmos
            if (elem.IsSelected)
            {
                var rectComp = elem.GetComponent<CuiRectTransformComponent>() ?? new CuiRectTransformComponent();
                DrawSelectedGizmo(elemScreenRect, elem, rectComp, onModified);
            }
            else
            {
                // Subtle boundary
                Handles.BeginGUI();
                Handles.color = new Color(0.35f, 0.45f, 0.55f, 0.25f);
                Handles.DrawPolyLine(
                    new Vector3(elemScreenRect.xMin, elemScreenRect.yMin, 0),
                    new Vector3(elemScreenRect.xMax, elemScreenRect.yMin, 0),
                    new Vector3(elemScreenRect.xMax, elemScreenRect.yMax, 0),
                    new Vector3(elemScreenRect.xMin, elemScreenRect.yMax, 0),
                    new Vector3(elemScreenRect.xMin, elemScreenRect.yMin, 0)
                );
                Handles.EndGUI();
            }

            // Click selection
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && elemScreenRect.Contains(e.mousePosition))
            {
                doc.Select(elem.Id, e.shift || e.control);
                _draggingElementId = elem.Id;
                _currentDragHandle = DragHandleType.Body;
                _dragStartMousePos = e.mousePosition;

                var rectComp = elem.GetComponent<CuiRectTransformComponent>();
                if (rectComp != null)
                {
                    _dragStartOffsetMin = RustCanvasScaler.ParseVector2(rectComp.OffsetMin, Vector2.zero);
                    _dragStartOffsetMax = RustCanvasScaler.ParseVector2(rectComp.OffsetMax, Vector2.zero);
                }
                e.Use();
            }
        }

        private void DrawSelectedGizmo(Rect elemScreenRect, CuiElementNode elem, CuiRectTransformComponent rect, Action onModified)
        {
            Handles.BeginGUI();
            Handles.color = new Color(0.2f, 0.75f, 1.0f, 0.95f); // Cyan selection

            // Thick boundary
            Handles.DrawPolyLine(
                new Vector3(elemScreenRect.xMin, elemScreenRect.yMin, 0),
                new Vector3(elemScreenRect.xMax, elemScreenRect.yMin, 0),
                new Vector3(elemScreenRect.xMax, elemScreenRect.yMax, 0),
                new Vector3(elemScreenRect.xMin, elemScreenRect.yMax, 0),
                new Vector3(elemScreenRect.xMin, elemScreenRect.yMin, 0)
            );

            // Corner Handles
            float handleSize = 6f;
            EditorGUI.DrawRect(new Rect(elemScreenRect.xMin - handleSize / 2, elemScreenRect.yMin - handleSize / 2, handleSize, handleSize), Color.cyan);
            EditorGUI.DrawRect(new Rect(elemScreenRect.xMax - handleSize / 2, elemScreenRect.yMin - handleSize / 2, handleSize, handleSize), Color.cyan);
            EditorGUI.DrawRect(new Rect(elemScreenRect.xMin - handleSize / 2, elemScreenRect.yMax - handleSize / 2, handleSize, handleSize), Color.cyan);
            EditorGUI.DrawRect(new Rect(elemScreenRect.xMax - handleSize / 2, elemScreenRect.yMax - handleSize / 2, handleSize, handleSize), Color.cyan);

            Handles.EndGUI();

            // Dragging Body Movement
            var e = Event.current;
            if (_draggingElementId == elem.Id && _currentDragHandle == DragHandleType.Body && e.type == EventType.MouseDrag)
            {
                var delta = (e.mousePosition - _dragStartMousePos) / _zoom;
                if (SnapToGrid)
                {
                    delta.x = Mathf.Round(delta.x / GridSize) * GridSize;
                    delta.y = Mathf.Round(delta.y / GridSize) * GridSize;
                }

                var newOffsetMin = new Vector2(_dragStartOffsetMin.x + delta.x, _dragStartOffsetMin.y - delta.y);
                var newOffsetMax = new Vector2(_dragStartOffsetMax.x + delta.x, _dragStartOffsetMax.y - delta.y);

                rect.OffsetMin = RustCanvasScaler.FormatVector2(newOffsetMin, "0.#");
                rect.OffsetMax = RustCanvasScaler.FormatVector2(newOffsetMax, "0.#");

                onModified?.Invoke();
                e.Use();
            }

            // Measurement Tooltip
            float realW = elemScreenRect.width / _zoom;
            float realH = elemScreenRect.height / _zoom;
            string dimText = $"<b>{elem.Name}</b> | W: {realW:0}px  H: {realH:0}px";
            var tipStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 10,
                richText = true,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(elemScreenRect.x, elemScreenRect.y - 22, 220, 20), dimText, tipStyle);
        }

        private void DrawRulers(Rect viewRect, Rect screenRect)
        {
            float rulerThickness = 18f;
            var topRulerRect = new Rect(viewRect.x + rulerThickness, viewRect.y, viewRect.width - rulerThickness, rulerThickness);
            var leftRulerRect = new Rect(viewRect.x, viewRect.y + rulerThickness, rulerThickness, viewRect.height - rulerThickness);
            var cornerRect = new Rect(viewRect.x, viewRect.y, rulerThickness, rulerThickness);

            EditorGUI.DrawRect(topRulerRect, new Color(0.16f, 0.17f, 0.20f, 0.95f));
            EditorGUI.DrawRect(leftRulerRect, new Color(0.16f, 0.17f, 0.20f, 0.95f));
            EditorGUI.DrawRect(cornerRect, new Color(0.13f, 0.14f, 0.16f, 1f));

            var rulerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 8,
                normal = { textColor = new Color(0.55f, 0.55f, 0.6f, 0.75f) }
            };

            Handles.BeginGUI();
            Handles.color = new Color(0.3f, 0.32f, 0.36f, 0.7f);

            // Top Ruler Marks
            for (float px = 0; px <= CurrentPreset.Width; px += 100)
            {
                float x = screenRect.x + px * _zoom;
                if (x >= topRulerRect.x && x <= topRulerRect.xMax)
                {
                    Handles.DrawLine(new Vector3(x, topRulerRect.yMax - 5, 0), new Vector3(x, topRulerRect.yMax, 0));
                    GUI.Label(new Rect(x + 2, topRulerRect.y, 40, rulerThickness), $"{px:0}", rulerStyle);
                }
            }

            // Left Ruler Marks
            for (float py = 0; py <= CurrentPreset.Height; py += 100)
            {
                float y = screenRect.y + py * _zoom;
                if (y >= leftRulerRect.y && y <= leftRulerRect.yMax)
                {
                    Handles.DrawLine(new Vector3(leftRulerRect.xMax - 5, y, 0), new Vector3(leftRulerRect.xMax, y, 0));
                    GUI.Label(new Rect(leftRulerRect.x + 1, y - 9, rulerThickness, 14), $"{py:0}", rulerStyle);
                }
            }

            Handles.EndGUI();
        }

        private void DrawCanvasToolbar(Rect viewRect)
        {
            var barRect = new Rect(viewRect.xMax - 430, viewRect.yMax - 30, 420, 24);
            EditorGUI.DrawRect(barRect, new Color(0.13f, 0.14f, 0.16f, 0.92f));

            GUILayout.BeginArea(barRect);
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label($"Zoom: {Mathf.RoundToInt(_zoom * 100)}%", EditorStyles.miniLabel, GUILayout.Width(62));
            _zoom = GUILayout.HorizontalSlider(_zoom, 0.2f, 2.0f, GUILayout.Width(65));

            if (GUILayout.Button("Fit", EditorStyles.miniButton, GUILayout.Width(28)))
            {
                _zoom = 0.55f;
                _panOffset = new Vector2(60, 40);
            }

            SnapToGrid = GUILayout.Toggle(SnapToGrid, "Snap", EditorStyles.miniButton, GUILayout.Width(42));
            ShowRulers = GUILayout.Toggle(ShowRulers, "Rulers", EditorStyles.miniButton, GUILayout.Width(46));

            // Background Mode
            BackgroundMode = (CanvasBackgroundMode)EditorGUILayout.EnumPopup(BackgroundMode, EditorStyles.miniButton, GUILayout.Width(95));

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
