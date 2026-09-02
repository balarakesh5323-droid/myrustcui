using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Editor.Canvas.Tools;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Discovery;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas
{
    public enum CanvasBackgroundMode
    {
        DarkGrid,
        RustInGame1,
        RustInGame2
    }

    /// <summary>
    /// Master interactive 2D canvas viewport editor for Rust CUI Builder.
    /// Integrates authoritative coordinates, modular tools (Select, Move, Resize, Rotate, Anchor, Pivot),
    /// multi-element alignment/distribution, zoom-to-cursor, rulers, draggable guides, and debug HUD.
    /// </summary>
    public class CuiCanvasEditorView
    {
        private Vector2 _panOffset = new Vector2(60f, 40f);
        private float _zoom = 0.55f;
        private bool _isPanning;
        private Vector2 _lastMousePos;
        private bool _hasFitted = false;

        public CanvasGuideSystem GuideSystem { get; } = new CanvasGuideSystem();
        public CanvasToolController ToolController { get; } = new CanvasToolController();

        public bool ShowRulers { get; set; } = true;
        public bool ShowGuides { get; set; } = true;
        public bool ShowAnchors { get; set; } = true;
        public bool ShowPivot { get; set; } = true;
        public CanvasBackgroundMode BackgroundMode { get; set; } = CanvasBackgroundMode.DarkGrid;

        public RustResolutionPreset CurrentPreset { get; set; } = RustResolutionPreset.Presets[3]; // 1920x1080 default

        private int _canvasControlId;
        private string _hoveredElementName = "";

        public void Draw(Rect viewRect, CuiDocument doc, Action onModified, Action<string> onCommitUndo = null)
        {
            float toolbarHeight = 26f;
            var topToolbarRect = new Rect(viewRect.x, viewRect.y, viewRect.width, toolbarHeight);
            var canvasAreaRect = new Rect(viewRect.x, viewRect.y + toolbarHeight, viewRect.width, viewRect.height - toolbarHeight);

            // Auto-fit canvas on first display with real window width
            if (!_hasFitted && canvasAreaRect.width > 300 && canvasAreaRect.height > 200)
            {
                FitCanvas(canvasAreaRect);
                _hasFitted = true;
            }

            _canvasControlId = GUIUtility.GetControlID(FocusType.Keyboard);

            // Dark canvas workspace background
            EditorGUI.DrawRect(canvasAreaRect, new Color(0.09f, 0.10f, 0.12f, 1f));

            var coords = RustCanvasCoordinates.Instance;
            float canvasW = CurrentPreset.Width;
            float canvasH = CurrentPreset.Height;

            // Handle Input & Dispatch to Tools
            HandleCanvasInput(canvasAreaRect, doc, onModified, onCommitUndo, canvasW, canvasH);

            // Calculate simulated screen frame
            var screenRect = coords.CanvasToScreen(new Rect(0, 0, canvasW, canvasH), canvasAreaRect, _panOffset, _zoom);

            // 1. Draw Rust Game Screen Frame & Background
            DrawScreenFrame(screenRect);

            // 2. Draw Grid inside screen bounds
            if (BackgroundMode == CanvasBackgroundMode.DarkGrid)
            {
                DrawGrid(screenRect);
            }

            // 3. Draw Elements (sorted root first)
            if (doc != null && doc.Elements != null)
            {
                foreach (var elem in doc.Elements)
                {
                    DrawElement(screenRect, elem, doc, canvasAreaRect, canvasW, canvasH);
                }
            }

            // 4. Draw Active Tool Overlay (Marquee, Handles, Anchors, Pivot, etc.)
            ToolController.DrawToolOverlay(canvasAreaRect, _panOffset, _zoom, canvasW, canvasH, doc);

            // 5. Draw Draggable Guides
            if (ShowGuides)
            {
                DrawGuides(canvasAreaRect, screenRect, canvasW, canvasH);
            }

            // 6. Draw Rulers
            if (ShowRulers)
            {
                DrawRulers(canvasAreaRect, screenRect, canvasW, canvasH);
            }

            // 7. Draw Top Tool Selector Bar
            DrawTopToolbar(topToolbarRect, doc, onModified, onCommitUndo, canvasAreaRect, canvasW, canvasH);

            // 8. Draw Bottom Right Canvas Controls (Zoom / Snap / BG)
            DrawBottomToolbar(canvasAreaRect, doc);

            // 9. Draw Debug HUD (if enabled)
            var mouseScreen = Event.current.mousePosition;
            var mouseCanvas = coords.ScreenToCanvas(mouseScreen, canvasAreaRect, _panOffset, _zoom);
            var mouseRust = coords.CanvasToRust(mouseCanvas, canvasW, canvasH);
            CanvasDebugOverlay.DrawDebugHUD(
                canvasAreaRect,
                mouseScreen,
                mouseCanvas,
                mouseRust,
                ToolController.ActiveTool?.ToolName ?? "None",
                _hoveredElementName,
                doc?.SelectedIds.Count ?? 0,
                _canvasControlId,
                Event.current.type,
                _zoom,
                _panOffset
            );
        }

        private void HandleCanvasInput(Rect viewRect, CuiDocument doc, Action onModified, Action<string> onCommitUndo, float canvasW, float canvasH)
        {
            var e = Event.current;
            if (!viewRect.Contains(e.mousePosition)) return;

            // Track hovered element for tooltips & debug
            var hit = CanvasHitTester.HitTestElements(e.mousePosition, doc, viewRect, _panOffset, _zoom, canvasW, canvasH);
            _hoveredElementName = hit != null ? hit.Name : "";

            // Hotkey F: Fit to view
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F && !e.control && !e.alt && !e.command && !e.shift)
            {
                FitCanvas(viewRect);
                e.Use();
                return;
            }

            // Pan: Middle mouse button or Alt + Left click
            if (e.type == EventType.MouseDown && (e.button == 2 || (e.button == 0 && e.alt)))
            {
                _isPanning = true;
                _lastMousePos = e.mousePosition;
                GUIUtility.keyboardControl = _canvasControlId;
                e.Use();
                return;
            }
            if (e.type == EventType.MouseDrag && _isPanning)
            {
                _panOffset += e.mousePosition - _lastMousePos;
                _lastMousePos = e.mousePosition;
                e.Use();
                return;
            }
            if (e.type == EventType.MouseUp && _isPanning)
            {
                _isPanning = false;
                e.Use();
                return;
            }

            // Zoom-to-Cursor: Mouse wheel
            if (e.type == EventType.ScrollWheel)
            {
                float zoomDelta = -e.delta.y * 0.04f;
                float oldZoom = _zoom;
                _zoom = Mathf.Clamp(_zoom + zoomDelta, 0.15f, 4.0f);

                // Invariant: mouse cursor remains on the same canvas coordinate
                var mouseRel = e.mousePosition - (viewRect.position + _panOffset);
                _panOffset -= mouseRel * (_zoom / oldZoom - 1f);

                e.Use();
                return;
            }

            // Keyboard Shortcuts (Delete, Duplicate, Nudge)
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
                {
                    DeleteSelected(doc, onModified, onCommitUndo);
                    e.Use();
                    return;
                }
                if (e.control && e.keyCode == KeyCode.D)
                {
                    DuplicateSelected(doc, onModified, onCommitUndo);
                    e.Use();
                    return;
                }
                if (e.control && e.keyCode == KeyCode.A)
                {
                    doc.SelectAll();
                    e.Use();
                    return;
                }

                // Arrow keys nudge position
                if (e.keyCode == KeyCode.LeftArrow || e.keyCode == KeyCode.RightArrow || e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow)
                {
                    float step = e.shift ? 10f : (GuideSystem.SnapToGrid ? GuideSystem.GridSize : 1f);
                    Vector2 delta = Vector2.zero;
                    if (e.keyCode == KeyCode.LeftArrow) delta.x = -step;
                    if (e.keyCode == KeyCode.RightArrow) delta.x = step;
                    if (e.keyCode == KeyCode.UpArrow) delta.y = step;
                    if (e.keyCode == KeyCode.DownArrow) delta.y = -step;

                    NudgeSelected(doc, delta, onModified, onCommitUndo);
                    e.Use();
                    return;
                }
            }

            // Dispatch to Active Tool
            ToolController.ProcessEvent(e, viewRect, _panOffset, _zoom, canvasW, canvasH, doc, GuideSystem, onModified, onCommitUndo);
        }

        private void DrawScreenFrame(Rect screenRect)
        {
            if (BackgroundMode == CanvasBackgroundMode.RustInGame1)
            {
                var bg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RustCUIBuilder/Resources/Backgrounds/RustBackground1.jpg");
                if (bg != null) GUI.DrawTexture(screenRect, bg, ScaleMode.ScaleAndCrop);
                else EditorGUI.DrawRect(screenRect, new Color(0.06f, 0.07f, 0.09f, 0.98f));
            }
            else if (BackgroundMode == CanvasBackgroundMode.RustInGame2)
            {
                var bg = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RustCUIBuilder/Resources/Backgrounds/RustBackground2.jpg");
                if (bg != null) GUI.DrawTexture(screenRect, bg, ScaleMode.ScaleAndCrop);
                else EditorGUI.DrawRect(screenRect, new Color(0.06f, 0.07f, 0.09f, 0.98f));
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

        private void DrawGrid(Rect screenRect)
        {
            Handles.BeginGUI();
            Color gridColor = new Color(0.22f, 0.24f, 0.28f, 0.35f);
            float step = GuideSystem.GridSize * _zoom;
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

        private void DrawElement(Rect screenRect, CuiElementNode elem, CuiDocument doc, Rect viewRect, float canvasW, float canvasH)
        {
            if (elem.IsHidden) return;

            var coords = RustCanvasCoordinates.Instance;
            var elemScreenRect = coords.GetElementScreenRect(elem, doc, viewRect, _panOffset, _zoom, canvasW, canvasH);

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

            // Selection Outline
            if (elem.IsSelected)
            {
                Handles.BeginGUI();
                Handles.color = new Color(0.2f, 0.75f, 1.0f, 0.95f); // Cyan selection
                Handles.DrawPolyLine(
                    new Vector3(elemScreenRect.xMin, elemScreenRect.yMin, 0),
                    new Vector3(elemScreenRect.xMax, elemScreenRect.yMin, 0),
                    new Vector3(elemScreenRect.xMax, elemScreenRect.yMax, 0),
                    new Vector3(elemScreenRect.xMin, elemScreenRect.yMax, 0),
                    new Vector3(elemScreenRect.xMin, elemScreenRect.yMin, 0)
                );
                Handles.EndGUI();
            }
            else
            {
                // Subtle boundary
                Handles.BeginGUI();
                Handles.color = new Color(0.35f, 0.45f, 0.55f, 0.2f);
                Handles.DrawPolyLine(
                    new Vector3(elemScreenRect.xMin, elemScreenRect.yMin, 0),
                    new Vector3(elemScreenRect.xMax, elemScreenRect.yMin, 0),
                    new Vector3(elemScreenRect.xMax, elemScreenRect.yMax, 0),
                    new Vector3(elemScreenRect.xMin, elemScreenRect.yMax, 0),
                    new Vector3(elemScreenRect.xMin, elemScreenRect.yMin, 0)
                );
                Handles.EndGUI();
            }
        }

        private void DrawGuides(Rect viewRect, Rect screenRect, float canvasW, float canvasH)
        {
            var coords = RustCanvasCoordinates.Instance;
            Handles.BeginGUI();
            Handles.color = new Color(0.3f, 0.85f, 1.0f, 0.75f); // Cyan guides

            foreach (var g in GuideSystem.Guides)
            {
                if (g.Orientation == GuideOrientation.Vertical)
                {
                    float screenX = coords.CanvasToScreen(new Vector2(g.CanvasPosition, 0), viewRect, _panOffset, _zoom).x;
                    Handles.DrawLine(new Vector3(screenX, viewRect.y, 0), new Vector3(screenX, viewRect.yMax, 0));
                }
                else
                {
                    float screenY = coords.CanvasToScreen(new Vector2(0, g.CanvasPosition), viewRect, _panOffset, _zoom).y;
                    Handles.DrawLine(new Vector3(viewRect.x, screenY, 0), new Vector3(viewRect.xMax, screenY, 0));
                }
            }
            Handles.EndGUI();
        }

        private void DrawRulers(Rect viewRect, Rect screenRect, float canvasW, float canvasH)
        {
            float rulerThickness = 16f;
            var topRulerRect = new Rect(viewRect.x + rulerThickness, viewRect.y, viewRect.width - rulerThickness, rulerThickness);
            var leftRulerRect = new Rect(viewRect.x, viewRect.y + rulerThickness, rulerThickness, viewRect.height - rulerThickness);
            var cornerRect = new Rect(viewRect.x, viewRect.y, rulerThickness, rulerThickness);

            EditorGUI.DrawRect(topRulerRect, new Color(0.12f, 0.13f, 0.16f, 0.98f));
            EditorGUI.DrawRect(leftRulerRect, new Color(0.12f, 0.13f, 0.16f, 0.98f));
            EditorGUI.DrawRect(cornerRect, new Color(0.10f, 0.11f, 0.13f, 1f));

            var rulerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 8,
                normal = { textColor = new Color(0.55f, 0.55f, 0.6f, 0.75f) }
            };

            Handles.BeginGUI();
            Handles.color = new Color(0.3f, 0.32f, 0.36f, 0.7f);

            // Top Ruler Marks
            for (float px = 0; px <= canvasW; px += 100)
            {
                float x = screenRect.x + px * _zoom;
                if (x >= topRulerRect.x && x <= topRulerRect.xMax)
                {
                    Handles.DrawLine(new Vector3(x, topRulerRect.yMax - 5, 0), new Vector3(x, topRulerRect.yMax, 0));
                    GUI.Label(new Rect(x + 2, topRulerRect.y, 40, rulerThickness), $"{px:0}", rulerStyle);
                }
            }

            // Left Ruler Marks
            for (float py = 0; py <= canvasH; py += 100)
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

        private void DrawTopToolbar(Rect topToolbarRect, CuiDocument doc, Action onModified, Action<string> onCommitUndo, Rect canvasAreaRect, float canvasW, float canvasH)
        {
            EditorGUI.DrawRect(topToolbarRect, new Color(0.12f, 0.13f, 0.15f, 1f));

            var contentRect = new Rect(topToolbarRect.x + 4, topToolbarRect.y + 2, topToolbarRect.width - 8, topToolbarRect.height - 4);
            GUILayout.BeginArea(contentRect);
            EditorGUILayout.BeginHorizontal();

            // Tools Segment
            if (GUILayout.Toggle(ToolController.ActiveMode == CanvasToolMode.Select, "Select (Q)", EditorStyles.toolbarButton, GUILayout.Width(68)))
                ToolController.ActiveMode = CanvasToolMode.Select;
            if (GUILayout.Toggle(ToolController.ActiveMode == CanvasToolMode.Move, "Move (W)", EditorStyles.toolbarButton, GUILayout.Width(64)))
                ToolController.ActiveMode = CanvasToolMode.Move;
            if (GUILayout.Toggle(ToolController.ActiveMode == CanvasToolMode.Rotate, "Rotate (E)", EditorStyles.toolbarButton, GUILayout.Width(68)))
                ToolController.ActiveMode = CanvasToolMode.Rotate;
            if (GUILayout.Toggle(ToolController.ActiveMode == CanvasToolMode.Resize, "Resize (R)", EditorStyles.toolbarButton, GUILayout.Width(68)))
                ToolController.ActiveMode = CanvasToolMode.Resize;
            if (GUILayout.Toggle(ToolController.ActiveMode == CanvasToolMode.Anchor, "Anchors (T)", EditorStyles.toolbarButton, GUILayout.Width(74)))
                ToolController.ActiveMode = CanvasToolMode.Anchor;
            if (GUILayout.Toggle(ToolController.ActiveMode == CanvasToolMode.Pivot, "Pivot (Y)", EditorStyles.toolbarButton, GUILayout.Width(62)))
                ToolController.ActiveMode = CanvasToolMode.Pivot;

            GUILayout.Space(10);

            // Alignment Tools
            var selected = doc?.SelectedElements;
            GUI.enabled = selected != null && selected.Count >= 2;

            if (GUILayout.Button("Left", EditorStyles.toolbarButton, GUILayout.Width(38)))
            {
                CanvasAlignmentEngine.AlignLeft(selected, doc, canvasW, canvasH);
                onCommitUndo?.Invoke("Align Left");
                onModified?.Invoke();
            }
            if (GUILayout.Button("Center", EditorStyles.toolbarButton, GUILayout.Width(48)))
            {
                CanvasAlignmentEngine.AlignCenter(selected, doc, canvasW, canvasH);
                onCommitUndo?.Invoke("Align Center");
                onModified?.Invoke();
            }
            if (GUILayout.Button("Right", EditorStyles.toolbarButton, GUILayout.Width(42)))
            {
                CanvasAlignmentEngine.AlignRight(selected, doc, canvasW, canvasH);
                onCommitUndo?.Invoke("Align Right");
                onModified?.Invoke();
            }
            if (GUILayout.Button("Top", EditorStyles.toolbarButton, GUILayout.Width(36)))
            {
                CanvasAlignmentEngine.AlignTop(selected, doc, canvasW, canvasH);
                onCommitUndo?.Invoke("Align Top");
                onModified?.Invoke();
            }
            if (GUILayout.Button("Bottom", EditorStyles.toolbarButton, GUILayout.Width(52)))
            {
                CanvasAlignmentEngine.AlignBottom(selected, doc, canvasW, canvasH);
                onCommitUndo?.Invoke("Align Bottom");
                onModified?.Invoke();
            }

            GUI.enabled = selected != null && selected.Count >= 3;
            if (GUILayout.Button("Dist H", EditorStyles.toolbarButton, GUILayout.Width(46)))
            {
                CanvasAlignmentEngine.DistributeHorizontally(selected, doc, canvasW, canvasH);
                onCommitUndo?.Invoke("Distribute Horizontally");
                onModified?.Invoke();
            }
            if (GUILayout.Button("Dist V", EditorStyles.toolbarButton, GUILayout.Width(46)))
            {
                CanvasAlignmentEngine.DistributeVertically(selected, doc, canvasW, canvasH);
                onCommitUndo?.Invoke("Distribute Vertically");
                onModified?.Invoke();
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            // Fit Canvas quick button
            if (GUILayout.Button("Fit (F)", EditorStyles.toolbarButton, GUILayout.Width(48)))
            {
                FitCanvas(canvasAreaRect);
            }

            // Debug Overlay Toggle
            CanvasDebugOverlay.IsEnabled = GUILayout.Toggle(CanvasDebugOverlay.IsEnabled, "Debug HUD", EditorStyles.toolbarButton, GUILayout.Width(75));

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawBottomToolbar(Rect viewRect, CuiDocument doc)
        {
            var barRect = new Rect(viewRect.xMax - 470, viewRect.yMax - 26, 460, 22);
            EditorGUI.DrawRect(barRect, new Color(0.12f, 0.13f, 0.15f, 0.95f));

            GUILayout.BeginArea(barRect);
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label($"Zoom: {Mathf.RoundToInt(_zoom * 100)}%", EditorStyles.miniLabel, GUILayout.Width(62));
            _zoom = GUILayout.HorizontalSlider(_zoom, 0.2f, 2.5f, GUILayout.Width(60));

            // Zoom Presets
            if (GUILayout.Button("Fit", EditorStyles.miniButton, GUILayout.Width(28)))
            {
                FitCanvas(viewRect);
            }
            if (GUILayout.Button("100%", EditorStyles.miniButton, GUILayout.Width(38)))
            {
                _zoom = 1.0f;
            }

            GuideSystem.SnapToGrid = GUILayout.Toggle(GuideSystem.SnapToGrid, "Snap", EditorStyles.miniButton, GUILayout.Width(42));
            ShowRulers = GUILayout.Toggle(ShowRulers, "Rulers", EditorStyles.miniButton, GUILayout.Width(46));
            ShowGuides = GUILayout.Toggle(ShowGuides, "Guides", EditorStyles.miniButton, GUILayout.Width(46));

            // Background Mode
            BackgroundMode = (CanvasBackgroundMode)EditorGUILayout.EnumPopup(BackgroundMode, EditorStyles.miniButton, GUILayout.Width(88));

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        public void FitCanvas(Rect viewRect)
        {
            if (viewRect.width < 100 || viewRect.height < 100) return;

            float availW = Mathf.Max(50f, viewRect.width - 60f);
            float availH = Mathf.Max(50f, viewRect.height - 60f);
            float targetZoomW = availW / CurrentPreset.Width;
            float targetZoomH = availH / CurrentPreset.Height;
            _zoom = Mathf.Clamp(Mathf.Min(targetZoomW, targetZoomH), 0.2f, 1.2f);

            float screenW = CurrentPreset.Width * _zoom;
            float screenH = CurrentPreset.Height * _zoom;
            _panOffset = new Vector2(
                Mathf.Max(10f, (viewRect.width - screenW) / 2f),
                Mathf.Max(10f, (viewRect.height - screenH) / 2f)
            );
        }

        private void DeleteSelected(CuiDocument doc, Action onModified, Action<string> onCommitUndo)
        {
            var selected = doc?.SelectedElements;
            if (selected == null || selected.Count == 0) return;

            foreach (var s in selected)
            {
                if (!s.IsLocked) doc.RemoveElement(s.Id);
            }
            onCommitUndo?.Invoke("Delete Element(s)");
            onModified?.Invoke();
        }

        private void DuplicateSelected(CuiDocument doc, Action onModified, Action<string> onCommitUndo)
        {
            var selected = doc?.SelectedElements;
            if (selected == null || selected.Count == 0) return;

            var newSelectedIds = new List<string>();
            foreach (var s in selected)
            {
                var clone = s.Clone(true, $"{s.Name}_Copy");
                doc.AddElement(clone);
                newSelectedIds.Add(clone.Id);
            }

            doc.ClearSelection();
            foreach (var id in newSelectedIds) doc.Select(id, true);

            onCommitUndo?.Invoke("Duplicate Element(s)");
            onModified?.Invoke();
        }

        private void NudgeSelected(CuiDocument doc, Vector2 delta, Action onModified, Action<string> onCommitUndo)
        {
            var selected = doc?.SelectedElements;
            if (selected == null || selected.Count == 0) return;

            foreach (var s in selected)
            {
                if (s.IsLocked) continue;
                var r = s.GetComponent<CuiRectTransformComponent>();
                if (r == null) continue;

                var min = RustCanvasScaler.ParseVector2(r.OffsetMin, Vector2.zero);
                var max = RustCanvasScaler.ParseVector2(r.OffsetMax, Vector2.zero);

                r.OffsetMin = RustCanvasScaler.FormatVector2(min + delta, "0.#");
                r.OffsetMax = RustCanvasScaler.FormatVector2(max + delta, "0.#");
            }

            onCommitUndo?.Invoke("Nudge Element(s)");
            onModified?.Invoke();
        }
    }
}
