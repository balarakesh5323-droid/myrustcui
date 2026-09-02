using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Editor.AssetBrowser;
using RustCUIBuilder.Editor.Canvas;
using RustCUIBuilder.Editor.CodeSync;
using RustCUIBuilder.Editor.DifferenceView;
using RustCUIBuilder.Editor.Hierarchy;
using RustCUIBuilder.Editor.Inspector;
using RustCUIBuilder.Editor.Snapshots;
using RustCUIBuilder.Editor.Toolbox;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Core.Project;
using RustCUIBuilder.Runtime.Core.Serialization;
using RustCUIBuilder.Runtime.Core.Validation;
using RustCUIBuilder.Runtime.Discovery;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Windows
{
    public class RustCuiBuilderWindow : EditorWindow
    {
        private enum LeftSidebarTab
        {
            Hierarchy,
            Toolbox
        }

        private enum RightBottomTab
        {
            CodeSync,
            AssetBrowser,
            Snapshots,
            Validation
        }

        private CuiDocument _document;
        private RustCuiProject _project;
        private CuiCommandHistory _history;
        private CuiValidationReport _lastValidationReport;
        private string _currentFilePath = "";
        private CuiDocument _lastSnapshotState;

        private CuiHierarchyView _hierarchyView;
        private CuiToolboxView _toolboxView;
        private CuiCanvasEditorView _canvasView;
        private CuiInspectorView _inspectorView;
        private CuiCodeSyncView _codeSyncView;
        private CuiAssetBrowserView _assetBrowserView;
        private CuiSnapshotManager _snapshotManager;
        private CuiDifferenceOverlayView _diffOverlayView;

        private LeftSidebarTab _leftSidebarTab = LeftSidebarTab.Hierarchy;
        private RightBottomTab _rightBottomTab = RightBottomTab.AssetBrowser;

        [MenuItem("Rust/CUI Builder (Visual Designer) %#r", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<RustCuiBuilderWindow>("Rust CUI Builder", true);
            window.minSize = new Vector2(960, 600);
            window.Show();
        }

        [MenuItem("Rust/Diagnostics/Run Canvas Tooling Diagnostics", priority = 200)]
        public static void RunCanvasToolingDiagnostics()
        {
            const float CanvasW = 1920f;
            const float CanvasH = 1080f;
            var coords = RustCanvasCoordinates.Instance;

            // 1. Alignment Test
            var doc1 = new CuiDocument();
            var e1 = CreateTestElement("E1", 100, 100, 200, 100, CanvasW, CanvasH);
            var e2 = CreateTestElement("E2", 300, 250, 150, 80, CanvasW, CanvasH);
            doc1.AddElement(e1); doc1.AddElement(e2);
            RustCUIBuilder.Editor.Canvas.Services.CanvasAlignmentService.AlignLeft(new List<CuiElementNode> { e1, e2 }, doc1, CanvasW, CanvasH, RustCUIBuilder.Editor.Canvas.Services.AlignmentTarget.SelectionBounds);
            if (Mathf.Abs(coords.GetElementCanvasRect(e1, doc1, CanvasW, CanvasH).xMin - 100f) > 0.5f ||
                Mathf.Abs(coords.GetElementCanvasRect(e2, doc1, CanvasW, CanvasH).xMin - 100f) > 0.5f)
                throw new Exception("AlignLeft test failed");

            // 2. Equal Spacing Test
            var doc2 = new CuiDocument();
            var se1 = CreateTestElement("SE1", 0, 100, 100, 50, CanvasW, CanvasH);
            var se2 = CreateTestElement("SE2", 150, 100, 100, 50, CanvasW, CanvasH);
            var se3 = CreateTestElement("SE3", 500, 100, 100, 50, CanvasW, CanvasH);
            doc2.AddElement(se1); doc2.AddElement(se2); doc2.AddElement(se3);
            RustCUIBuilder.Editor.Canvas.Services.CanvasDistributionService.EqualHorizontalSpacing(new List<CuiElementNode> { se1, se2, se3 }, doc2, CanvasW, CanvasH);
            var r1 = coords.GetElementCanvasRect(se1, doc2, CanvasW, CanvasH);
            var r2 = coords.GetElementCanvasRect(se2, doc2, CanvasW, CanvasH);
            var r3 = coords.GetElementCanvasRect(se3, doc2, CanvasW, CanvasH);
            if (Mathf.Abs(r2.xMin - 250f) > 0.5f || Mathf.Abs(r3.xMin - 500f) > 0.5f)
                throw new Exception("EqualHorizontalSpacing test failed");

            // 3. Group / Ungroup Test
            var doc3 = new CuiDocument();
            var ge1 = CreateTestElement("GE1", 200, 300, 100, 50, CanvasW, CanvasH);
            var ge2 = CreateTestElement("GE2", 350, 320, 120, 60, CanvasW, CanvasH);
            doc3.AddElement(ge1); doc3.AddElement(ge2);
            doc3.Select(ge1.Id, true); doc3.Select(ge2.Id, true);
            var g = RustCUIBuilder.Editor.Canvas.Services.CanvasHierarchyService.GroupSelection(doc3, CanvasW, CanvasH);
            if (g == null || ge1.Parent != g.Name) throw new Exception("GroupSelection test failed");
            RustCUIBuilder.Editor.Canvas.Services.CanvasHierarchyService.UngroupSelection(doc3, CanvasW, CanvasH);
            if (ge1.Parent != "Overlay") throw new Exception("UngroupSelection test failed");

            // 4. Layout Center in Parent Test
            var doc4 = new CuiDocument();
            var parent = CreateTestElement("Parent", 200, 200, 600, 400, CanvasW, CanvasH);
            var child = CreateTestElement("Child", 0, 0, 200, 100, CanvasW, CanvasH);
            child.Parent = "Parent";
            doc4.AddElement(parent); doc4.AddElement(child);
            RustCUIBuilder.Editor.Canvas.Services.CanvasLayoutService.CenterInParent(new List<CuiElementNode> { child }, doc4, CanvasW, CanvasH);
            var pRect = coords.GetElementCanvasRect(parent, doc4, CanvasW, CanvasH);
            var cRect = coords.GetElementCanvasRect(child, doc4, CanvasW, CanvasH);
            if (Mathf.Abs(pRect.center.x - cRect.center.x) > 0.5f || Mathf.Abs(pRect.center.y - cRect.center.y) > 0.5f)
                throw new Exception("CenterInParent test failed");

            // 5. Clipboard Duplicate Test
            var doc5 = new CuiDocument();
            var orig = CreateTestElement("Orig", 100, 100, 200, 80, CanvasW, CanvasH);
            doc5.AddElement(orig); doc5.Select(orig.Id);
            var dups = RustCUIBuilder.Editor.Canvas.Services.CanvasClipboardService.Duplicate(new List<CuiElementNode> { orig }, doc5, CanvasW, CanvasH);
            if (dups.Count != 1 || dups[0].Name == orig.Name) throw new Exception("Duplicate test failed");

            Debug.Log("[CanvasToolingDiagnostics] ALL 5 CORE CANVAS SERVICES (Alignment, Spacing, Grouping, Layout, Clipboard) PASSED 100%!");
        }

        private static CuiElementNode CreateTestElement(string name, float x, float y, float w, float h, float canvasW, float canvasH)
        {
            var elem = new CuiElementNode(name, "Overlay");
            elem.Components.Add(new CuiRectTransformComponent());
            RustCanvasCoordinates.Instance.ApplyNewCanvasRectToElementOffsets(new Rect(x, y, w, h), elem, null, canvasW, canvasH);
            return elem;
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Rust CUI Builder");
            minSize = new Vector2(960, 600);

            _history = new CuiCommandHistory();
            _hierarchyView = new CuiHierarchyView();
            _toolboxView = new CuiToolboxView();
            _canvasView = new CuiCanvasEditorView();
            _inspectorView = new CuiInspectorView();
            _codeSyncView = new CuiCodeSyncView();
            _assetBrowserView = new CuiAssetBrowserView();
            _snapshotManager = new CuiSnapshotManager();
            _diffOverlayView = new CuiDifferenceOverlayView();

            if (_document == null)
            {
                CreateNewDocument();
            }

            RustAssetDiscovery.ReindexAssets();

            _document.OnSelectionChanged += Repaint;
            _history.OnHistoryChanged += Repaint;
        }

        private void OnDisable()
        {
            if (_document != null)
            {
                _document.OnSelectionChanged -= Repaint;
            }
            if (_history != null)
            {
                _history.OnHistoryChanged -= Repaint;
            }
        }

        private void CreateNewDocument()
        {
            _document = new CuiDocument();
            _project = new RustCuiProject { ProjectName = "New CUI Project" };
            _project.FromDocument(_document);
            _currentFilePath = "";
            _history.Clear();
            _lastSnapshotState = null;

            // Default Canvas Structure
            var mainPanel = new CuiElementNode
            {
                Name = "MainPanel",
                Parent = "Overlay"
            };
            mainPanel.Components.Add(new CuiRectTransformComponent
            {
                AnchorMin = "0.2 0.2",
                AnchorMax = "0.8 0.8",
                OffsetMin = "0 0",
                OffsetMax = "0 0"
            });
            mainPanel.Components.Add(new CuiImageComponent
            {
                Color = "0.1 0.12 0.16 0.95",
                Sprite = "assets/content/ui/ui.background.tile.psd"
            });
            mainPanel.Components.Add(new CuiNeedsCursorComponent());

            var titleText = new CuiElementNode
            {
                Name = "TitleText",
                Parent = "MainPanel"
            };
            titleText.Components.Add(new CuiRectTransformComponent
            {
                AnchorMin = "0 0.88",
                AnchorMax = "1 1",
                OffsetMin = "16 0",
                OffsetMax = "-16 0"
            });
            titleText.Components.Add(new CuiTextComponent
            {
                Text = "RUST SERVER STORE",
                FontSize = 18,
                Font = "RobotoCondensed-Bold.ttf",
                Align = TextAnchor.MiddleLeft,
                Color = "1 1 1 1"
            });

            _document.AddElement(mainPanel);
            _document.AddElement(titleText);

            _lastSnapshotState = _document.Clone();
            OnDocumentModified();
        }

        private void OnDocumentModified()
        {
            _codeSyncView?.UpdateCode(_document);
            Validate();
            Repaint();
        }

        private void Validate()
        {
            _lastValidationReport = CuiValidator.ValidateDocument(_document);
        }

        private void RecordSnapshot(string actionName)
        {
            if (_document == null) return;
            var currentState = _document.Clone();
            if (_lastSnapshotState != null)
            {
                _history.Record(new DocumentSnapshotCommand(actionName, _document, _lastSnapshotState, currentState));
            }
            _lastSnapshotState = currentState.Clone();
        }

        private void OnGUI()
        {
            float menuBarHeight = 22f;
            float statusBarHeight = 22f;
            float contentHeight = Mathf.Max(100f, position.height - menuBarHeight - statusBarHeight);

            // 1. Top Menu Bar
            DrawMainMenuBar(new Rect(0, 0, position.width, menuBarHeight));

            // 2. Main 3-Column Content Layout (Left Sidebar, Center Clipped Viewport, Right Inspector)
            DrawMainLayout(new Rect(0, menuBarHeight, position.width, contentHeight));

            // 3. Status Bar
            DrawStatusBar(new Rect(0, position.height - statusBarHeight, position.width, statusBarHeight));

            // 4. Global Hotkeys
            HandleGlobalHotkeys();
        }

        private void DrawMainMenuBar(Rect barRect)
        {
            GUILayout.BeginArea(barRect);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // File Menu Dropdown
            if (GUILayout.Button("File", EditorStyles.toolbarDropDown, GUILayout.Width(45)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("New Project"), false, CreateNewDocument);
                menu.AddItem(new GUIContent("Open Project (.rustcui)..."), false, OpenProjectFile);
                menu.AddItem(new GUIContent("Save Project (.rustcui)"), false, SaveProjectFile);
                menu.AddItem(new GUIContent("Save Project As..."), false, SaveProjectFileAs);
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Configure Rust Game Path..."), false, ConfigureRustPath);
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Import CUI JSON..."), false, ImportJsonFile);
                menu.AddItem(new GUIContent("Export CUI JSON..."), false, ExportJsonFile);
                menu.AddItem(new GUIContent("Export Oxide C# Plugin (.cs)..."), false, ExportCSharpFile);
                menu.ShowAsContext();
            }

            // Edit Menu
            if (GUILayout.Button("Edit", EditorStyles.toolbarDropDown, GUILayout.Width(45)))
            {
                var menu = new GenericMenu();
                if (_history.CanUndo) menu.AddItem(new GUIContent("Undo (Ctrl+Z)"), false, UndoAction);
                else menu.AddDisabledItem(new GUIContent("Undo (Ctrl+Z)"));

                if (_history.CanRedo) menu.AddItem(new GUIContent("Redo (Ctrl+Y)"), false, RedoAction);
                else menu.AddDisabledItem(new GUIContent("Redo (Ctrl+Y)"));

                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Select All (Ctrl+A)"), false, () => _document.SelectAll());
                menu.AddItem(new GUIContent("Deselect All"), false, () => _document.ClearSelection());
                menu.AddItem(new GUIContent("Delete Selected (Del)"), false, DeleteSelectedElement);
                menu.ShowAsContext();
            }

            GUILayout.Space(8);

            // Undo / Redo buttons
            GUI.enabled = _history.CanUndo;
            if (GUILayout.Button("↶ Undo", EditorStyles.toolbarButton, GUILayout.Width(55))) UndoAction();
            GUI.enabled = _history.CanRedo;
            if (GUILayout.Button("↷ Redo", EditorStyles.toolbarButton, GUILayout.Width(55))) RedoAction();
            GUI.enabled = true;

            GUILayout.Space(12);

            // Resolution Presets Dropdown
            GUILayout.Label("Screen:", EditorStyles.miniLabel);
            var presetNames = RustResolutionPreset.Presets.Select(p => p.Name).ToArray();
            int curResIdx = RustResolutionPreset.Presets.IndexOf(_canvasView.CurrentPreset);
            if (curResIdx < 0) curResIdx = 3;
            int newResIdx = EditorGUILayout.Popup(curResIdx, presetNames, EditorStyles.toolbarDropDown, GUILayout.Width(150));
            if (newResIdx != curResIdx && newResIdx >= 0 && newResIdx < RustResolutionPreset.Presets.Count)
            {
                _canvasView.CurrentPreset = RustResolutionPreset.Presets[newResIdx];
            }

            GUILayout.Space(8);

            // Difference Overlay Controls
            _diffOverlayView.DrawToolbarControls();

            GUILayout.FlexibleSpace();

            // Rust Game Path Indicator
            var install = SteamDiscovery.DiscoverRustInstallation();
            string rustStatus = install.IsValid ? $"✓ Rust Found ({install.DiscoveredItemIconCount} items)" : "⚠ Rust Not Detected";
            var statusColor = install.IsValid ? new Color(0.4f, 0.9f, 0.4f) : new Color(1f, 0.5f, 0.3f);
            var prevCol = GUI.contentColor;
            GUI.contentColor = statusColor;
            GUILayout.Label(rustStatus, EditorStyles.miniLabel);
            GUI.contentColor = prevCol;

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawMainLayout(Rect contentRect)
        {
            float leftPanelWidth = Mathf.Clamp(contentRect.width * 0.18f, 220f, 280f);
            float rightPanelWidth = Mathf.Clamp(contentRect.width * 0.24f, 280f, 380f);
            float centerWidth = Mathf.Max(200f, contentRect.width - leftPanelWidth - rightPanelWidth);

            // 1. Left Column (Tabbed Hierarchy & Toolbox)
            var leftColumnRect = new Rect(contentRect.x, contentRect.y, leftPanelWidth, contentRect.height);
            DrawLeftSidebar(leftColumnRect);

            // 2. Center Column (Canvas Visual Editor with Hard-Clipped Viewport)
            var canvasRect = new Rect(contentRect.x + leftPanelWidth, contentRect.y, centerWidth, contentRect.height);
            _canvasView.Draw(canvasRect, _document, () => OnDocumentModified(), (action) => { RecordSnapshot(action); OnDocumentModified(); });

            if (_diffOverlayView.IsEnabled)
            {
                _diffOverlayView.DrawCanvasOverlay(canvasRect);
            }

            // 3. Right Column (Inspector Top + Tabbed Bottom)
            float inspectorHeight = contentRect.height * 0.52f;
            float rightBottomHeight = contentRect.height - inspectorHeight;

            var inspectorRect = new Rect(contentRect.x + leftPanelWidth + centerWidth, contentRect.y, rightPanelWidth, inspectorHeight);
            var rightBottomRect = new Rect(contentRect.x + leftPanelWidth + centerWidth, contentRect.y + inspectorHeight, rightPanelWidth, rightBottomHeight);

            _inspectorView.Draw(inspectorRect, _document, () => { RecordSnapshot("Property Edit"); OnDocumentModified(); });
            DrawRightBottomTabs(rightBottomRect);
        }

        private void DrawLeftSidebar(Rect rect)
        {
            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginVertical("box");

            // Segmented Tab Switcher
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Toggle(_leftSidebarTab == LeftSidebarTab.Hierarchy, "Hierarchy", EditorStyles.toolbarButton))
                _leftSidebarTab = LeftSidebarTab.Hierarchy;
            if (GUILayout.Toggle(_leftSidebarTab == LeftSidebarTab.Toolbox, "Toolbox / Primitives", EditorStyles.toolbarButton))
                _leftSidebarTab = LeftSidebarTab.Toolbox;
            EditorGUILayout.EndHorizontal();

            var innerRect = new Rect(0, 22, rect.width, rect.height - 24);

            if (_leftSidebarTab == LeftSidebarTab.Hierarchy)
            {
                _hierarchyView.Draw(innerRect, _document, () => { RecordSnapshot("Hierarchy Edit"); OnDocumentModified(); });
            }
            else
            {
                _toolboxView.Draw(innerRect, _document, () => { RecordSnapshot("Add Primitive/Template"); OnDocumentModified(); });
            }

            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawRightBottomTabs(Rect rect)
        {
            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Toggle(_rightBottomTab == RightBottomTab.CodeSync, "Live Code", EditorStyles.toolbarButton)) _rightBottomTab = RightBottomTab.CodeSync;
            if (GUILayout.Toggle(_rightBottomTab == RightBottomTab.AssetBrowser, "Rust Assets", EditorStyles.toolbarButton)) _rightBottomTab = RightBottomTab.AssetBrowser;
            if (GUILayout.Toggle(_rightBottomTab == RightBottomTab.Snapshots, "Snapshots", EditorStyles.toolbarButton)) _rightBottomTab = RightBottomTab.Snapshots;

            string valTitle = _lastValidationReport != null && _lastValidationReport.ErrorCount > 0
                ? $"Diagnostics ({_lastValidationReport.ErrorCount}✕)"
                : "Diagnostics (✓)";
            if (GUILayout.Toggle(_rightBottomTab == RightBottomTab.Validation, valTitle, EditorStyles.toolbarButton)) _rightBottomTab = RightBottomTab.Validation;

            EditorGUILayout.EndHorizontal();

            var innerRect = new Rect(0, 20, rect.width, rect.height - 24);

            switch (_rightBottomTab)
            {
                case RightBottomTab.CodeSync:
                    _codeSyncView.Draw(innerRect, _document, () => { RecordSnapshot("Code Sync Import"); OnDocumentModified(); });
                    break;
                case RightBottomTab.AssetBrowser:
                    _assetBrowserView.Draw(innerRect, _document, () => { RecordSnapshot("Asset Applied"); OnDocumentModified(); });
                    break;
                case RightBottomTab.Snapshots:
                    _snapshotManager.Draw(innerRect, _project, _document, () => { RecordSnapshot("Snapshot Restored"); OnDocumentModified(); });
                    break;
                case RightBottomTab.Validation:
                    DrawValidationDiagnostics(innerRect);
                    break;
            }

            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawValidationDiagnostics(Rect rect)
        {
            GUILayout.BeginArea(rect);
            if (_lastValidationReport == null || _lastValidationReport.IsValid)
            {
                EditorGUILayout.HelpBox("✓ All CUI validation checks passed! Document structure is valid for Rust/Oxide.", MessageType.Info);
            }
            else
            {
                var errors = _lastValidationReport.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
                var warnings = _lastValidationReport.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();

                if (errors.Count > 0)
                {
                    EditorGUILayout.LabelField($"Errors ({errors.Count}):", EditorStyles.boldLabel);
                    foreach (var err in errors)
                    {
                        EditorGUILayout.HelpBox($"✕ [{err.ElementName}] {err.Message}", MessageType.Error);
                    }
                }

                if (warnings.Count > 0)
                {
                    EditorGUILayout.LabelField($"Warnings ({warnings.Count}):", EditorStyles.boldLabel);
                    foreach (var warn in warnings)
                    {
                        EditorGUILayout.HelpBox($"⚠ [{warn.ElementName}] {warn.Message}", MessageType.Warning);
                    }
                }
            }
            GUILayout.EndArea();
        }

        private void DrawStatusBar(Rect statusRect)
        {
            EditorGUI.DrawRect(statusRect, new Color(0.13f, 0.14f, 0.16f, 1f));

            GUILayout.BeginArea(statusRect);
            EditorGUILayout.BeginHorizontal();

            string projectLabel = string.IsNullOrEmpty(_currentFilePath) ? _project.ProjectName : Path.GetFileName(_currentFilePath);
            GUILayout.Label($"Project: {projectLabel} | Elements: {_document?.Elements?.Count ?? 0}", EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            var selected = _document?.PrimarySelectedElement;
            string selInfo = selected != null ? $"Selected: {selected.Name} (Parent: {selected.Parent})" : "None Selected";
            GUILayout.Label(selInfo, EditorStyles.miniLabel);

            GUILayout.Space(20);

            string valSummary = (_lastValidationReport == null || _lastValidationReport.IsValid)
                ? "✓ CUI Valid"
                : $"✕ {_lastValidationReport.ErrorCount} Errors, {_lastValidationReport.WarningCount} Warnings";
            GUILayout.Label(valSummary, EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void HandleGlobalHotkeys()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.control && e.keyCode == KeyCode.Z)
                {
                    UndoAction();
                    e.Use();
                }
                else if (e.control && (e.keyCode == KeyCode.Y || (e.shift && e.keyCode == KeyCode.Z)))
                {
                    RedoAction();
                    e.Use();
                }
                else if (e.control && e.keyCode == KeyCode.S)
                {
                    if (string.IsNullOrEmpty(_currentFilePath)) SaveProjectFileAs();
                    else SaveProjectFile();
                    e.Use();
                }
                else if (e.control && e.keyCode == KeyCode.N)
                {
                    CreateNewDocument();
                    e.Use();
                }
                else if (e.control && e.keyCode == KeyCode.O)
                {
                    OpenProjectFile();
                    e.Use();
                }
            }
        }

        private void UndoAction()
        {
            if (_history != null && _history.CanUndo)
            {
                _history.Undo();
                OnDocumentModified();
            }
        }

        private void RedoAction()
        {
            if (_history != null && _history.CanRedo)
            {
                _history.Redo();
                OnDocumentModified();
            }
        }

        private void DeleteSelectedElement()
        {
            var selected = _document?.SelectedElements;
            if (selected != null && selected.Count > 0)
            {
                foreach (var s in selected)
                {
                    if (!s.IsLocked) _document.RemoveElement(s.Id);
                }
                RecordSnapshot("Delete Element(s)");
                OnDocumentModified();
            }
        }

        private void OpenProjectFile()
        {
            string path = EditorUtility.OpenFilePanel("Open Rust CUI Project", "", "rustcui");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    _project = JsonUtility.FromJson<RustCuiProject>(json);
                    _document = _project.ToDocument();
                    _currentFilePath = path;
                    _history.Clear();
                    _lastSnapshotState = _document.Clone();
                    OnDocumentModified();
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Error Opening Project", ex.Message, "OK");
                }
            }
        }

        private void SaveProjectFile()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveProjectFileAs();
                return;
            }

            try
            {
                _project.FromDocument(_document);
                string json = JsonUtility.ToJson(_project, true);
                File.WriteAllText(_currentFilePath, json);
                ShowNotification(new GUIContent("Project Saved!"));
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Error Saving Project", ex.Message, "OK");
            }
        }

        private void SaveProjectFileAs()
        {
            string path = EditorUtility.SaveFilePanel("Save Rust CUI Project", "", _project.ProjectName, "rustcui");
            if (!string.IsNullOrEmpty(path))
            {
                _currentFilePath = path;
                _project.ProjectName = Path.GetFileNameWithoutExtension(path);
                SaveProjectFile();
            }
        }

        private void ImportJsonFile()
        {
            string path = EditorUtility.OpenFilePanel("Import CUI JSON", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var parseResult = CuiParser.ParseJson(json);
                    if (parseResult != null && parseResult.Document != null)
                    {
                        _document = parseResult.Document;
                        _project.FromDocument(_document);
                        RecordSnapshot("Import JSON");
                        OnDocumentModified();
                    }
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Error Importing JSON", ex.Message, "OK");
                }
            }
        }

        private void ExportJsonFile()
        {
            string path = EditorUtility.SaveFilePanel("Export CUI JSON", "", "cui_layout", "json");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string json = CuiJsonSerializer.SerializeDocument(_document, true);
                    File.WriteAllText(path, json);
                    EditorUtility.RevealInFinder(path);
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Error Exporting JSON", ex.Message, "OK");
                }
            }
        }

        private void ExportCSharpFile()
        {
            string defaultName = _project.ProjectName.Replace(" ", "") + "Plugin";
            string path = EditorUtility.SaveFilePanel("Export Oxide C# Plugin", "", defaultName, "cs");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    string pluginName = Path.GetFileNameWithoutExtension(path);
                    string code = CuiCodeGenerator.GeneratePluginCode(_document, new CodeGeneratorOptions
                    {
                        ChatCommandName = pluginName.ToLowerInvariant(),
                        UseHighLevelPresets = true
                    });
                    File.WriteAllText(path, code);
                    EditorUtility.RevealInFinder(path);
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Error Exporting C# Plugin", ex.Message, "OK");
                }
            }
        }

        private void ConfigureRustPath()
        {
            string path = EditorUtility.OpenFolderPanel("Select Rust Game Directory", "", "");
            if (!string.IsNullOrEmpty(path))
            {
                RustBundleManager.Reload();
                RustAssetDiscovery.ReindexAssets();
                Repaint();
            }
        }
    }
}
