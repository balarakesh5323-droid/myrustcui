using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Discovery;
using RustCUIBuilder.Runtime.Rendering.Canvas;
using RustCUIBuilder.Editor.Canvas.Tools;
using RustCUIBuilder.Editor.Canvas.Services;
using RustCUIBuilder.Editor.Canvas.QuickActions;

namespace RustCUIBuilder.Editor.Canvas
{
    public enum CanvasBackgroundMode
    {
        DarkGrid,
        RustInGame1,
        RustInGame2,
        TransparentChecker,
        SolidBlack
    }

    /// <summary>
    /// Master interactive 2D canvas viewport editor for Rust CUI Builder.
    /// Provides an authoritative hard-clipped viewport, zoom-to-cursor, pan,
    /// professional alignment, distribution, hierarchy, layout presets, Figma-style Alt distance measurements,
    /// smart guides, rulers, quick action toolbar, and rich right-click context menus.
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
        public CanvasQuickActionBar QuickActionBar { get; } = new CanvasQuickActionBar();

        public bool ShowRulers { get; set; } = true;
        public bool ShowGuides { get; set; } = true;
        public bool ShowAnchors { get; set; } = true;
        public bool ShowPivot { get; set; } = true;
        public CanvasBackgroundMode BackgroundMode { get; set; } = CanvasBackgroundMode.DarkGrid;

        public RustResolutionPreset CurrentPreset { get; set; } = RustResolutionPreset.Presets[3]; // 1920x1080 default

        private int _canvasControlId;
        private string _hoveredElementName = "";

        public Rect LastViewportRect { get; private set; }
        public float CurrentZoom => _zoom;
        public Vector2 CurrentPan => _panOffset;

        public void Draw(Rect containerRect, CuiDocument doc, Action onModified, Action<string> onCommitUndo = null)
        {
            float topToolbarHeight = 48f; // Double height for Tools + Quick Actions
            float bottomToolbarHeight = 24f;

            var topToolbarRect = new Rect(containerRect.x, containerRect.y, containerRect.width, topToolbarHeight);
            var bottomToolbarRect = new Rect(containerRect.x, containerRect.yMax - bottomToolbarHeight, containerRect.width, bottomToolbarHeight);
            var viewportRect = new Rect(containerRect.x, containerRect.y + topToolbarHeight, containerRect.width, containerRect.height - topToolbarHeight - bottomToolbarHeight);

            LastViewportRect = viewportRect;

            if (viewportRect.width < 50 || viewportRect.height < 50) return;

            // 1. Draw Top Toolbars Header (Outside Viewport)
            DrawTopToolbar(topToolbarRect, doc, onModified, onCommitUndo, viewportRect);

            // 2. Draw Bottom Controls Toolbar (Outside Viewport)
            DrawBottomToolbar(bottomToolbarRect, viewportRect);

            // 3. Draw Hard-Clipped Canvas Viewport
            DrawClippedViewport(viewportRect, doc, onModified, onCommitUndo);
        }

        private void DrawClippedViewport(Rect viewportRect, CuiDocument doc, Action onModified, Action<string> onCommitUndo)
        {
            var localViewportRect = new Rect(0, 0, viewportRect.width, viewportRect.height);

            if (!_hasFitted && viewportRect.width > 200 && viewportRect.height > 150)
            {
                FitCanvas(localViewportRect);
                _hasFitted = true;
            }

            _canvasControlId = GUIUtility.GetControlID(FocusType.Keyboard);

            // Hard Clip Group - Strictly confines all rendering and mouse interaction to viewportRect
            GUI.BeginGroup(viewportRect);

            EditorGUI.DrawRect(localViewportRect, new Color(0.08f, 0.09f, 0.11f, 1f));

            var coords = RustCanvasCoordinates.Instance;
            float canvasW = CurrentPreset.Width;
            float canvasH = CurrentPreset.Height;

            // Handle Input & Dispatch to Tools / Shortcuts
            HandleCanvasInput(localViewportRect, viewportRect, doc, onModified, onCommitUndo, canvasW, canvasH);

            var screenRect = coords.CanvasToScreen(new Rect(0, 0, canvasW, canvasH), localViewportRect, _panOffset, _zoom);

            // A. Draw Screen Frame
            DrawScreenFrame(screenRect);

            // B. Draw Grid
            if (BackgroundMode == CanvasBackgroundMode.DarkGrid)
            {
                DrawGrid(screenRect, localViewportRect);
            }

            // C. Draw Elements
            if (doc != null && doc.Elements != null)
            {
                foreach (var elem in doc.Elements)
                {
                    DrawElement(screenRect, elem, doc, localViewportRect, canvasW, canvasH);
                }
            }

            // D. Draw Active Tool Overlay (Marquee, Resize Handles, Anchors)
            ToolController.DrawToolOverlay(localViewportRect, _panOffset, _zoom, canvasW, canvasH, doc);

            // E. Draw Smart Guides & Figma-Style Alt Distance Measurements
            if (Event.current.alt && doc?.PrimarySelectedElement != null)
            {
                CanvasMeasurementService.DrawMeasurements(doc.PrimarySelectedElement, Event.current.mousePosition, doc, localViewportRect, _panOffset, _zoom, canvasW, canvasH);
            }

            // F. Draw User Guides
            if (ShowGuides)
            {
                DrawGuides(localViewportRect, screenRect, canvasW, canvasH);
            }

            // G. Draw Rulers
            if (ShowRulers)
            {
                DrawRulers(localViewportRect, screenRect, canvasW, canvasH);
            }

            // H. Debug HUD
            if (CanvasDebugOverlay.IsEnabled)
            {
                var mouseViewport = Event.current.mousePosition;
                var mouseCanvas = coords.ScreenToCanvas(mouseViewport, localViewportRect, _panOffset, _zoom);
                var mouseRust = coords.CanvasToRust(mouseCanvas, canvasW, canvasH);
                var mouseWindow = mouseViewport + viewportRect.position;

                CanvasDebugOverlay.DrawDebugHUD(
                    localViewportRect,
                    viewportRect,
                    mouseWindow,
                    mouseViewport,
                    mouseCanvas,
                    mouseRust,
                    ToolController.ActiveTool?.ToolName ?? "None",
                    _hoveredElementName,
                    doc?.SelectedIds.Count ?? 0,
                    _canvasControlId,
                    Event.current.type,
                    _zoom,
                    _panOffset,
                    canvasW,
                    canvasH,
                    new Vector2(Screen.width, Screen.height)
                );
            }

            GUI.EndGroup();
        }

        private void HandleCanvasInput(Rect localViewportRect, Rect globalViewportRect, CuiDocument doc, Action onModified, Action<string> onCommitUndo, float canvasW, float canvasH)
        {
            var e = Event.current;

            // Universal interaction check: allows drag/up events through to any active tool even when mouse exits viewport
            bool isActiveToolInteracting = _isPanning || ToolController.IsAnyToolInteracting;

            // When clicking inside canvas viewport, steal keyboard focus from any active inspector textfields
            if (e.type == EventType.MouseDown && localViewportRect.Contains(e.mousePosition))
            {
                GUIUtility.keyboardControl = _canvasControlId;
            }

            if (!localViewportRect.Contains(e.mousePosition) && !isActiveToolInteracting) return;

            var coords = RustCanvasCoordinates.Instance;

            // Hovered element tracking
            var hit = CanvasHitTester.HitTestElements(e.mousePosition, doc, localViewportRect, _panOffset, _zoom, canvasW, canvasH);
            _hoveredElementName = hit != null ? hit.Name : "";

            // Right-Click Context Menu
            if ((e.type == EventType.ContextClick) || (e.type == EventType.MouseDown && e.button == 1))
            {
                ShowCanvasContextMenu(e.mousePosition, hit, doc, localViewportRect, onModified, onCommitUndo, canvasW, canvasH);
                e.Use();
                return;
            }

            // Hotkey F: Fit to view
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F && !e.control && !e.alt && !e.command && !e.shift)
            {
                FitCanvas(localViewportRect);
                e.Use();
                return;
            }

            // Pan: Middle Mouse or Space + Left Mouse
            if (e.type == EventType.MouseDown && (e.button == 2 || (e.button == 0 && (e.alt || KeyCode.Space == e.keyCode))))
            {
                _isPanning = true;
                _lastMousePos = e.mousePosition;
                GUIUtility.keyboardControl = _canvasControlId;
                GUIUtility.hotControl = _canvasControlId;
                e.Use();
                return;
            }
            if (e.type == EventType.MouseDrag && _isPanning)
            {
                _panOffset += e.mousePosition - _lastMousePos;
                _lastMousePos = e.mousePosition;
                ClampPan(localViewportRect, canvasW, canvasH);
                e.Use();
                return;
            }
            if ((e.rawType == EventType.MouseUp || e.type == EventType.MouseUp) && _isPanning)
            {
                _isPanning = false;
                if (GUIUtility.hotControl == _canvasControlId) GUIUtility.hotControl = 0;
                e.Use();
                return;
            }

            // Zoom-to-Cursor: Mouse wheel
            if (e.type == EventType.ScrollWheel)
            {
                Vector2 canvasPointUnderMouse = coords.ScreenToCanvas(e.mousePosition, localViewportRect, _panOffset, _zoom);
                float zoomDelta = -e.delta.y * 0.05f;
                _zoom = Mathf.Clamp(_zoom + zoomDelta, 0.15f, 4.0f);
                _panOffset = e.mousePosition - canvasPointUnderMouse * _zoom;
                ClampPan(localViewportRect, canvasW, canvasH);
                e.Use();
                return;
            }

            // Global Canvas Shortcuts (Copy, Cut, Paste, Duplicate, Delete, Group, Ungroup, Nudge, Order)
            if (CanvasShortcutService.ProcessGlobalShortcuts(e, doc, canvasW, canvasH, onModified, onCommitUndo))
            {
                return;
            }

            // Dispatch to Active Tool
            ToolController.ProcessEvent(e, localViewportRect, _panOffset, _zoom, canvasW, canvasH, doc, GuideSystem, onModified, onCommitUndo);

            // Manage hotControl capture so mouse drag & mouse up events are guaranteed to reach the active tool
            if (e.type == EventType.MouseDown && (ToolController.IsAnyToolInteracting || _isPanning))
            {
                GUIUtility.hotControl = _canvasControlId;
            }
            else if (e.rawType == EventType.MouseUp || e.type == EventType.MouseUp)
            {
                if (GUIUtility.hotControl == _canvasControlId)
                {
                    GUIUtility.hotControl = 0;
                }
            }
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                if (GUIUtility.hotControl == _canvasControlId)
                {
                    GUIUtility.hotControl = 0;
                }
            }
        }

        private void ShowCanvasContextMenu(
            Vector2 mousePos,
            CuiElementNode hitElem,
            CuiDocument doc,
            Rect localViewportRect,
            Action onModified,
            Action<string> onCommitUndo,
            float canvasW,
            float canvasH)
        {
            var coords = RustCanvasCoordinates.Instance;
            var menu = new GenericMenu();
            var mouseCanvas = coords.ScreenToCanvas(mousePos, localViewportRect, _panOffset, _zoom);

            if (hitElem != null)
            {
                if (!doc.IsSelected(hitElem.Id)) doc.Select(hitElem.Id, false);
                var selected = doc.SelectedElements.ToList();

                menu.AddItem(new GUIContent("Cut (Ctrl+X)"), false, () =>
                {
                    onCommitUndo?.Invoke("Cut Selection");
                    CanvasClipboardService.Cut(selected, doc);
                    onModified?.Invoke();
                });
                menu.AddItem(new GUIContent("Copy (Ctrl+C)"), false, () =>
                {
                    CanvasClipboardService.Copy(selected, doc);
                });
                menu.AddItem(new GUIContent("Paste (Ctrl+V)"), CanvasClipboardService.HasClipboardData, () =>
                {
                    onCommitUndo?.Invoke("Paste");
                    CanvasClipboardService.Paste(doc, canvasW, canvasH, mouseCanvas);
                    onModified?.Invoke();
                });
                menu.AddItem(new GUIContent("Duplicate (Ctrl+D)"), false, () =>
                {
                    onCommitUndo?.Invoke("Duplicate");
                    CanvasClipboardService.Duplicate(selected, doc, canvasW, canvasH);
                    onModified?.Invoke();
                });
                menu.AddItem(new GUIContent("Delete (Del)"), false, () =>
                {
                    onCommitUndo?.Invoke("Delete Selection");
                    foreach (var elem in selected) doc.RemoveElement(elem.Id);
                    onModified?.Invoke();
                });

                menu.AddSeparator("");

                menu.AddItem(new GUIContent("Group (Ctrl+G)"), selected.Count >= 2, () =>
                {
                    onCommitUndo?.Invoke("Group");
                    CanvasHierarchyService.GroupSelection(doc, canvasW, canvasH);
                    onModified?.Invoke();
                });
                menu.AddItem(new GUIContent("Ungroup (Ctrl+Shift+G)"), true, () =>
                {
                    onCommitUndo?.Invoke("Ungroup");
                    CanvasHierarchyService.UngroupSelection(doc, canvasW, canvasH);
                    onModified?.Invoke();
                });

                menu.AddSeparator("");

                // Alignment Submenu
                menu.AddItem(new GUIContent("Align/Left"), false, () => { onCommitUndo?.Invoke("Align Left"); CanvasAlignmentService.AlignLeft(selected, doc, canvasW, canvasH); onModified?.Invoke(); });
                menu.AddItem(new GUIContent("Align/Center Horizontally"), false, () => { onCommitUndo?.Invoke("Align Center H"); CanvasAlignmentService.AlignCenterH(selected, doc, canvasW, canvasH); onModified?.Invoke(); });
                menu.AddItem(new GUIContent("Align/Right"), false, () => { onCommitUndo?.Invoke("Align Right"); CanvasAlignmentService.AlignRight(selected, doc, canvasW, canvasH); onModified?.Invoke(); });
                menu.AddItem(new GUIContent("Align/Top"), false, () => { onCommitUndo?.Invoke("Align Top"); CanvasAlignmentService.AlignTop(selected, doc, canvasW, canvasH); onModified?.Invoke(); });
                menu.AddItem(new GUIContent("Align/Center Vertically"), false, () => { onCommitUndo?.Invoke("Align Center V"); CanvasAlignmentService.AlignCenterV(selected, doc, canvasW, canvasH); onModified?.Invoke(); });
                menu.AddItem(new GUIContent("Align/Bottom"), false, () => { onCommitUndo?.Invoke("Align Bottom"); CanvasAlignmentService.AlignBottom(selected, doc, canvasW, canvasH); onModified?.Invoke(); });

                // Spacing Submenu
                menu.AddItem(new GUIContent("Distribute/Equal Horizontal Spacing"), selected.Count >= 3, () => { onCommitUndo?.Invoke("Equal H Spacing"); CanvasDistributionService.EqualHorizontalSpacing(selected, doc, canvasW, canvasH); onModified?.Invoke(); });
                menu.AddItem(new GUIContent("Distribute/Equal Vertical Spacing"), selected.Count >= 3, () => { onCommitUndo?.Invoke("Equal V Spacing"); CanvasDistributionService.EqualVerticalSpacing(selected, doc, canvasW, canvasH); onModified?.Invoke(); });
                menu.AddItem(new GUIContent("Distribute/Make Same Width"), selected.Count >= 2, () => { onCommitUndo?.Invoke("Same Width"); CanvasDistributionService.MakeSameWidth(selected, doc, canvasW, canvasH); onModified?.Invoke(); });
                menu.AddItem(new GUIContent("Distribute/Make Same Height"), selected.Count >= 2, () => { onCommitUndo?.Invoke("Same Height"); CanvasDistributionService.MakeSameHeight(selected, doc, canvasW, canvasH); onModified?.Invoke(); });

                // Ordering Submenu
                menu.AddItem(new GUIContent("Order/Bring to Front (Ctrl+Shift+])"), false, () => { onCommitUndo?.Invoke("Bring to Front"); CanvasHierarchyService.BringToFront(selected, doc); onModified?.Invoke(); });
                menu.AddItem(new GUIContent("Order/Bring Forward (Ctrl+])"), false, () => { onCommitUndo?.Invoke("Bring Forward"); CanvasHierarchyService.BringForward(selected, doc); onModified?.Invoke(); });
                menu.AddItem(new GUIContent("Order/Send Backward (Ctrl+[)"), false, () => { onCommitUndo?.Invoke("Send Backward"); CanvasHierarchyService.SendBackward(selected, doc); onModified?.Invoke(); });
                menu.AddItem(new GUIContent("Order/Send to Back (Ctrl+Shift+[)"), false, () => { onCommitUndo?.Invoke("Send to Back"); CanvasHierarchyService.SendToBack(selected, doc); onModified?.Invoke(); });

                // Parent Layout Submenu
                menu.AddItem(new GUIContent("Layout/Center in Parent"), false, () => { onCommitUndo?.Invoke("Center in Parent"); CanvasLayoutService.CenterInParent(selected, doc, canvasW, canvasH); onModified?.Invoke(); });
                menu.AddItem(new GUIContent("Layout/Fill Parent"), false, () => { onCommitUndo?.Invoke("Fill Parent"); CanvasLayoutService.FillParent(selected, doc, canvasW, canvasH); onModified?.Invoke(); });
                menu.AddItem(new GUIContent("Layout/Stretch Horizontally"), false, () => { onCommitUndo?.Invoke("Stretch H"); CanvasLayoutService.StretchH(selected, doc, canvasW, canvasH); onModified?.Invoke(); });
                menu.AddItem(new GUIContent("Layout/Stretch Vertically"), false, () => { onCommitUndo?.Invoke("Stretch V"); CanvasLayoutService.StretchV(selected, doc, canvasW, canvasH); onModified?.Invoke(); });

                menu.AddSeparator("");
                menu.AddItem(new GUIContent(hitElem.IsLocked ? "Unlock" : "Lock"), false, () => { hitElem.IsLocked = !hitElem.IsLocked; onModified?.Invoke(); });
                menu.AddItem(new GUIContent(hitElem.IsHidden ? "Show" : "Hide"), false, () => { hitElem.IsHidden = !hitElem.IsHidden; onModified?.Invoke(); });
            }
            else
            {
                // Empty canvas space context menu
                menu.AddItem(new GUIContent("Paste (Ctrl+V)"), CanvasClipboardService.HasClipboardData, () =>
                {
                    onCommitUndo?.Invoke("Paste");
                    CanvasClipboardService.Paste(doc, canvasW, canvasH, mouseCanvas);
                    onModified?.Invoke();
                });
                menu.AddItem(new GUIContent("Select All (Ctrl+A)"), false, () =>
                {
                    doc.SelectAll();
                    onModified?.Invoke();
                });

                menu.AddSeparator("");

                menu.AddItem(new GUIContent("Create/Panel"), false, () =>
                {
                    var p = new CuiElementNode("Panel_" + Guid.NewGuid().ToString("N").Substring(0, 4), "Overlay");
                    p.Components.Add(new CuiRectTransformComponent());
                    p.Components.Add(new CuiImageComponent { Color = "0.15 0.16 0.2 0.9", Sprite = "assets/content/ui/ui.background.tile.psd" });
                    coords.ApplyNewCanvasRectToElementOffsets(new Rect(mouseCanvas.x, mouseCanvas.y, 250, 150), p, doc, canvasW, canvasH);
                    doc.AddElement(p);
                    doc.Select(p.Id);
                    onCommitUndo?.Invoke("Create Panel");
                    onModified?.Invoke();
                });
                menu.AddItem(new GUIContent("Create/Label (Text)"), false, () =>
                {
                    var l = new CuiElementNode("Label_" + Guid.NewGuid().ToString("N").Substring(0, 4), "Overlay");
                    l.Components.Add(new CuiRectTransformComponent());
                    l.Components.Add(new CuiTextComponent { Text = "<b>New Text Label</b>", FontSize = 14 });
                    coords.ApplyNewCanvasRectToElementOffsets(new Rect(mouseCanvas.x, mouseCanvas.y, 200, 32), l, doc, canvasW, canvasH);
                    doc.AddElement(l);
                    doc.Select(l.Id);
                    onCommitUndo?.Invoke("Create Label");
                    onModified?.Invoke();
                });
                menu.AddItem(new GUIContent("Create/Button"), false, () =>
                {
                    var b = new CuiElementNode("Button_" + Guid.NewGuid().ToString("N").Substring(0, 4), "Overlay");
                    b.Components.Add(new CuiRectTransformComponent());
                    b.Components.Add(new CuiButtonComponent { Command = "action.exec", Color = "0.2 0.6 0.3 0.9" });
                    b.Components.Add(new CuiTextComponent { Text = "<b>CLICK ME</b>", Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" });
                    coords.ApplyNewCanvasRectToElementOffsets(new Rect(mouseCanvas.x, mouseCanvas.y, 140, 40), b, doc, canvasW, canvasH);
                    doc.AddElement(b);
                    doc.Select(b.Id);
                    onCommitUndo?.Invoke("Create Button");
                    onModified?.Invoke();
                });
            }

            menu.ShowAsContext();
        }

        private void ClampPan(Rect viewportRect, float canvasW, float canvasH)
        {
            float screenW = canvasW * _zoom;
            float screenH = canvasH * _zoom;
            float minPanX = 80f - screenW;
            float maxPanX = viewportRect.width - 80f;
            float minPanY = 80f - screenH;
            float maxPanY = viewportRect.height - 80f;

            _panOffset.x = Mathf.Clamp(_panOffset.x, minPanX, maxPanX);
            _panOffset.y = Mathf.Clamp(_panOffset.y, minPanY, maxPanY);
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

            Handles.BeginGUI();
            Handles.color = new Color(0.95f, 0.45f, 0.15f, 0.95f);
            Handles.DrawPolyLine(
                new Vector3(screenRect.xMin, screenRect.yMin, 0),
                new Vector3(screenRect.xMax, screenRect.yMin, 0),
                new Vector3(screenRect.xMax, screenRect.yMax, 0),
                new Vector3(screenRect.xMin, screenRect.yMax, 0),
                new Vector3(screenRect.xMin, screenRect.yMin, 0)
            );
            Handles.EndGUI();

            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                normal = { textColor = new Color(0.9f, 0.9f, 0.95f, 0.85f) }
            };
            GUI.Label(new Rect(screenRect.x + 8, screenRect.y + 6, 320, 18), $"{CurrentPreset.Name} ({CurrentPreset.Width}x{CurrentPreset.Height})", labelStyle);
        }

        private void DrawGrid(Rect screenRect, Rect localViewportRect)
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

        private void DrawElement(Rect screenRect, CuiElementNode elem, CuiDocument doc, Rect localViewportRect, float canvasW, float canvasH)
        {
            if (elem.IsHidden) return;

            var coords = RustCanvasCoordinates.Instance;
            var elemScreenRect = coords.GetElementScreenRect(elem, doc, localViewportRect, _panOffset, _zoom, canvasW, canvasH);

            var rectComp = elem.GetComponent<CuiRectTransformComponent>();
            float rotation = rectComp?.Rotation ?? 0f;
            bool hasRotation = Mathf.Abs(rotation) > 0.001f;
            Matrix4x4 prevMatrix = GUI.matrix;
            if (hasRotation)
            {
                var pivotScreen = coords.GetPivotScreenPoint(elem, doc, localViewportRect, _panOffset, _zoom, canvasW, canvasH);
                GUIUtility.RotateAroundPivot(-rotation, pivotScreen);
            }

            var img = elem.GetComponent<CuiImageComponent>();
            var raw = elem.GetComponent<CuiRawImageComponent>();
            var text = elem.GetComponent<CuiTextComponent>();
            var btn = elem.GetComponent<CuiButtonComponent>();

            Color fillColor = new Color(0.2f, 0.2f, 0.25f, 0.4f);
            Sprite elemSprite = null;

            if (img != null)
            {
                fillColor = CuiColorExtensions.ToUnityColor(img.Color, fillColor);
                if (img.ItemId != 0)
                {
                    var item = RustAssetDiscovery.FindItemById(img.ItemId);
                    if (item != null) elemSprite = RustAssetDiscovery.LoadItemIcon(item);
                }
                else if (!string.IsNullOrEmpty(img.Sprite))
                {
                    elemSprite = RustAssetDiscovery.GetSpriteByPath(img.Sprite);
                }
                else if (!string.IsNullOrEmpty(img.Material))
                {
                    elemSprite = RustAssetDiscovery.GetSpriteByPath(img.Material);
                }
            }
            else if (btn != null)
            {
                fillColor = CuiColorExtensions.ToUnityColor(btn.Color, new Color(0.2f, 0.5f, 0.3f, 0.8f));
                if (!string.IsNullOrEmpty(btn.Sprite))
                {
                    elemSprite = RustAssetDiscovery.GetSpriteByPath(btn.Sprite);
                }
                else if (!string.IsNullOrEmpty(btn.Material))
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
                Rect uv = new Rect(
                    elemSprite.rect.x / elemSprite.texture.width,
                    elemSprite.rect.y / elemSprite.texture.height,
                    elemSprite.rect.width / elemSprite.texture.width,
                    elemSprite.rect.height / elemSprite.texture.height
                );
                GUI.DrawTextureWithTexCoords(elemScreenRect, elemSprite.texture, uv);
                GUI.color = prevCol;
            }
            else
            {
                EditorGUI.DrawRect(elemScreenRect, fillColor);
            }

            // Element Text with Rich Text & Authentic Font Support
            if (text != null && !string.IsNullOrEmpty(text.Text))
            {
                var textStyle = new GUIStyle(EditorStyles.label)
                {
                    richText = true,
                    wordWrap = true,
                    fontSize = Mathf.Max(9, Mathf.RoundToInt(text.FontSize * _zoom)),
                    normal = { textColor = CuiColorExtensions.ToUnityColor(text.Color, Color.white) },
                    alignment = text.Align
                };

                if (!string.IsNullOrEmpty(text.Font))
                {
                    var customFont = RustBundleManager.LoadFont(text.Font);
                    if (customFont != null) textStyle.font = customFont;
                }

                GUI.Label(elemScreenRect, text.Text, textStyle);
            }

            // Selection Outline
            if (elem.IsSelected)
            {
                Handles.BeginGUI();
                Handles.color = new Color(0.2f, 0.75f, 1.0f, 0.95f);
                Handles.DrawPolyLine(
                    new Vector3(elemScreenRect.xMin, elemScreenRect.yMin, 0),
                    new Vector3(elemScreenRect.xMax, elemScreenRect.yMin, 0),
                    new Vector3(elemScreenRect.xMax, elemScreenRect.yMax, 0),
                    new Vector3(elemScreenRect.xMin, elemScreenRect.yMax, 0),
                    new Vector3(elemScreenRect.xMin, elemScreenRect.yMin, 0)
                );
                Handles.EndGUI();
            }

            if (hasRotation)
            {
                GUI.matrix = prevMatrix;
            }
        }

        private void DrawGuides(Rect localViewportRect, Rect screenRect, float canvasW, float canvasH)
        {
            Handles.BeginGUI();
            Handles.color = new Color(0.1f, 0.85f, 0.95f, 0.65f);

            foreach (var g in GuideSystem.Guides)
            {
                if (g.Orientation == GuideOrientation.Vertical)
                {
                    float x = screenRect.x + g.CanvasPosition * _zoom;
                    if (x >= localViewportRect.x && x <= localViewportRect.xMax)
                    {
                        Handles.DrawLine(new Vector3(x, localViewportRect.y, 0), new Vector3(x, localViewportRect.yMax, 0));
                    }
                }
                else
                {
                    float y = screenRect.y + g.CanvasPosition * _zoom;
                    if (y >= localViewportRect.y && y <= localViewportRect.yMax)
                    {
                        Handles.DrawLine(new Vector3(localViewportRect.x, y, 0), new Vector3(localViewportRect.xMax, y, 0));
                    }
                }
            }

            Handles.EndGUI();
        }

        private void DrawRulers(Rect localViewportRect, Rect screenRect, float canvasW, float canvasH)
        {
            float rulerThickness = 18f;
            var topRulerRect = new Rect(localViewportRect.x + rulerThickness, localViewportRect.y, localViewportRect.width - rulerThickness, rulerThickness);
            var leftRulerRect = new Rect(localViewportRect.x, localViewportRect.y + rulerThickness, rulerThickness, localViewportRect.height - rulerThickness);

            EditorGUI.DrawRect(topRulerRect, new Color(0.14f, 0.15f, 0.18f, 0.95f));
            EditorGUI.DrawRect(leftRulerRect, new Color(0.14f, 0.15f, 0.18f, 0.95f));

            var rulerStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 8,
                normal = { textColor = new Color(0.6f, 0.65f, 0.7f, 0.85f) }
            };

            Handles.BeginGUI();
            Handles.color = new Color(0.3f, 0.32f, 0.36f, 0.7f);

            for (float px = 0; px <= canvasW; px += 100)
            {
                float x = screenRect.x + px * _zoom;
                if (x >= topRulerRect.x && x <= topRulerRect.xMax)
                {
                    Handles.DrawLine(new Vector3(x, topRulerRect.yMax - 5, 0), new Vector3(x, topRulerRect.yMax, 0));
                    GUI.Label(new Rect(x + 2, topRulerRect.y, 40, rulerThickness), $"{px:0}", rulerStyle);
                }
            }

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

        private void DrawTopToolbar(Rect topToolbarRect, CuiDocument doc, Action onModified, Action<string> onCommitUndo, Rect viewportRect)
        {
            EditorGUI.DrawRect(topToolbarRect, new Color(0.12f, 0.13f, 0.15f, 1f));

            float rowHeight = 24f;
            var row1Rect = new Rect(topToolbarRect.x + 4, topToolbarRect.y, topToolbarRect.width - 8, rowHeight);
            var row2Rect = new Rect(topToolbarRect.x + 4, topToolbarRect.y + rowHeight, topToolbarRect.width - 8, rowHeight);

            // Row 1: Tools & View Controls
            GUILayout.BeginArea(row1Rect);
            EditorGUILayout.BeginHorizontal();

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

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Fit (F)", EditorStyles.toolbarButton, GUILayout.Width(48)))
            {
                FitCanvas(new Rect(0, 0, viewportRect.width, viewportRect.height));
            }

            CanvasDebugOverlay.IsEnabled = GUILayout.Toggle(CanvasDebugOverlay.IsEnabled, "Debug HUD", EditorStyles.toolbarButton, GUILayout.Width(75));

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();

            // Row 2: 1-Click Quick Actions (Alignment, Spacing, Hierarchy, Layout Presets)
            float canvasW = CurrentPreset.Width;
            float canvasH = CurrentPreset.Height;
            QuickActionBar.Draw(row2Rect, doc, canvasW, canvasH, onModified, onCommitUndo);
        }

        private void DrawBottomToolbar(Rect bottomToolbarRect, Rect viewportRect)
        {
            EditorGUI.DrawRect(bottomToolbarRect, new Color(0.12f, 0.13f, 0.15f, 1f));

            GUILayout.BeginArea(bottomToolbarRect);
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label($"Zoom: {Mathf.RoundToInt(_zoom * 100)}%", EditorStyles.miniLabel, GUILayout.Width(62));
            _zoom = GUILayout.HorizontalSlider(_zoom, 0.2f, 2.5f, GUILayout.Width(60));

            if (GUILayout.Button("Fit", EditorStyles.miniButton, GUILayout.Width(28)))
            {
                FitCanvas(new Rect(0, 0, viewportRect.width, viewportRect.height));
            }
            if (GUILayout.Button("100%", EditorStyles.miniButton, GUILayout.Width(38)))
            {
                _zoom = 1.0f;
            }

            GuideSystem.SnapToGrid = GUILayout.Toggle(GuideSystem.SnapToGrid, "Snap", EditorStyles.miniButton, GUILayout.Width(42));
            ShowRulers = GUILayout.Toggle(ShowRulers, "Rulers", EditorStyles.miniButton, GUILayout.Width(46));
            ShowGuides = GUILayout.Toggle(ShowGuides, "Guides", EditorStyles.miniButton, GUILayout.Width(46));

            // Background Mode Dropdown
            BackgroundMode = (CanvasBackgroundMode)EditorGUILayout.EnumPopup(BackgroundMode, EditorStyles.miniButton, GUILayout.Width(88));

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        public void FitCanvas(Rect localViewportRect)
        {
            if (localViewportRect.width < 50 || localViewportRect.height < 50) return;

            float padding = 40f;
            float availW = Mathf.Max(50f, localViewportRect.width - padding * 2f);
            float availH = Mathf.Max(50f, localViewportRect.height - padding * 2f);

            float scaleX = availW / CurrentPreset.Width;
            float scaleY = availH / CurrentPreset.Height;
            _zoom = Mathf.Clamp(Mathf.Min(scaleX, scaleY), 0.2f, 2.0f);

            float screenW = CurrentPreset.Width * _zoom;
            float screenH = CurrentPreset.Height * _zoom;
            _panOffset = new Vector2(
                (localViewportRect.width - screenW) * 0.5f,
                (localViewportRect.height - screenH) * 0.5f
            );
        }
    }
}
