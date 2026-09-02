using System;
using System.IO;
using System.Linq;
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
using UnityEditor;
using UnityEngine;

namespace RustCUIBuilder.Editor.Windows
{
    /// <summary>
    /// Master professional IDE-style Editor Window for Rust Oxide CUI Visual Builder.
    /// Integrates Hierarchy, Interactive Canvas, Property Inspector, Toolbox, Asset Browser,
    /// Code Sync, Snapshots, Difference Overlay, and real-time Validation Diagnostics.
    /// </summary>
    public class RustCuiBuilderWindow : EditorWindow
    {
        private CuiDocument _document;
        private RustCuiProject _project = new RustCuiProject();
        private readonly CuiCommandHistory _history = new CuiCommandHistory();
        private CuiDocument _lastSnapshotState;

        private CuiCanvasEditorView _canvasView;
        private CuiHierarchyView _hierarchyView;
        private CuiInspectorView _inspectorView;
        private CuiToolboxView _toolboxView;
        private CuiAssetBrowserView _assetBrowserView;
        private CuiCodeSyncView _codeSyncView;
        private CuiSnapshotManager _snapshotManager;
        private CuiDifferenceOverlayView _diffOverlayView;

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

        private LeftSidebarTab _leftSidebarTab = LeftSidebarTab.Hierarchy;
        private RightBottomTab _rightBottomTab = RightBottomTab.CodeSync;
        private CuiValidationReport _lastValidationReport;
        private string _currentFilePath = "";

        [MenuItem("Rust/CUI Builder (Visual Designer) %#r")]
        public static void ShowWindow()
        {
            var window = GetWindow<RustCuiBuilderWindow>("Rust CUI Builder");
            window.minSize = new Vector2(1100, 680);
            window.Show();
        }

        private void OnEnable()
        {
            _canvasView = new CuiCanvasEditorView();
            _hierarchyView = new CuiHierarchyView();
            _inspectorView = new CuiInspectorView();
            _toolboxView = new CuiToolboxView();
            _assetBrowserView = new CuiAssetBrowserView();
            _codeSyncView = new CuiCodeSyncView();
            _snapshotManager = new CuiSnapshotManager();
            _diffOverlayView = new CuiDifferenceOverlayView();

            if (_document == null)
            {
                CreateNewDocument();
            }

            _document.OnDocumentModified += OnDocumentModified;
            _document.OnSelectionChanged += Repaint;
            _history.OnHistoryChanged += Repaint;

            RustAssetDiscovery.ReindexAssets();
            Validate();
        }

        private void OnDisable()
        {
            if (_document != null)
            {
                _document.OnDocumentModified -= OnDocumentModified;
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
            DrawMainMenuBar();
            DrawMainLayout();
            DrawStatusBar();
            HandleGlobalHotkeys();
        }

        private void DrawMainMenuBar()
        {
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
        }

        private void DrawMainLayout()
        {
            float totalWidth = position.width;
            float totalHeight = position.height - 44;

            float leftPanelWidth = 260f;
            float rightPanelWidth = 340f;
            float centerWidth = totalWidth - leftPanelWidth - rightPanelWidth;

            // 1. Left Column (Tabbed Hierarchy & Toolbox)
            var leftColumnRect = new Rect(0, 20, leftPanelWidth, totalHeight);
            DrawLeftSidebar(leftColumnRect);

            // 2. Center Column (Canvas Visual Editor)
            var canvasRect = new Rect(leftPanelWidth, 20, centerWidth, totalHeight);
            _canvasView.Draw(canvasRect, _document, () => { RecordSnapshot("Canvas Drag/Resize"); OnDocumentModified(); });

            if (_diffOverlayView.IsEnabled)
            {
                _diffOverlayView.DrawCanvasOverlay(canvasRect);
            }

            // 3. Right Column (Inspector Top + Tabbed Bottom)
            float inspectorHeight = totalHeight * 0.55f;
            float rightBottomHeight = totalHeight - inspectorHeight;

            var inspectorRect = new Rect(leftPanelWidth + centerWidth, 20, rightPanelWidth, inspectorHeight);
            var rightBottomRect = new Rect(leftPanelWidth + centerWidth, 20 + inspectorHeight, rightPanelWidth, rightBottomHeight);

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

        private void DrawStatusBar()
        {
            var statusRect = new Rect(0, position.height - 22, position.width, 22);
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
                else if (e.keyCode == KeyCode.Delete)
                {
                    DeleteSelectedElement();
                    e.Use();
                }
                else if (e.control && e.keyCode == KeyCode.S)
                {
                    SaveProjectFile();
                    e.Use();
                }
            }
        }

        private void UndoAction()
        {
            _history.Undo();
            OnDocumentModified();
        }

        private void RedoAction()
        {
            _history.Redo();
            OnDocumentModified();
        }

        private void DeleteSelectedElement()
        {
            var selected = _document?.PrimarySelectedElement;
            if (selected != null)
            {
                _document.RemoveElement(selected.Id);
                RecordSnapshot($"Delete {selected.Name}");
                OnDocumentModified();
            }
        }

        private void SaveProjectFile()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveProjectFileAs();
            }
            else
            {
                _project.FromDocument(_document);
                _project.SaveToFile(_currentFilePath);
            }
        }

        private void SaveProjectFileAs()
        {
            string path = EditorUtility.SaveFilePanel("Save Rust CUI Project", "", $"{_project.ProjectName}.rustcui", "rustcui");
            if (!string.IsNullOrEmpty(path))
            {
                _currentFilePath = path;
                _project.ProjectName = Path.GetFileNameWithoutExtension(path);
                _project.FromDocument(_document);
                _project.SaveToFile(path);
            }
        }

        private void OpenProjectFile()
        {
            string path = EditorUtility.OpenFilePanel("Open Rust CUI Project", "", "rustcui");
            if (!string.IsNullOrEmpty(path))
            {
                var proj = RustCuiProject.LoadFromFile(path);
                if (proj != null)
                {
                    _project = proj;
                    _currentFilePath = path;
                    _document = proj.ToDocument();
                    _document.OnDocumentModified += OnDocumentModified;
                    _document.OnSelectionChanged += Repaint;
                    _history.Clear();
                    _lastSnapshotState = _document.Clone();
                    OnDocumentModified();
                }
            }
        }

        private void ImportJsonFile()
        {
            string path = EditorUtility.OpenFilePanel("Import CUI JSON", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                string json = File.ReadAllText(path);
                var result = CuiParser.ParseJson(json);
                if (result.Success && result.Document != null)
                {
                    _document.Elements.Clear();
                    foreach (var elem in result.Document.Elements)
                    {
                        _document.AddElement(elem);
                    }
                    _lastSnapshotState = _document.Clone();
                    OnDocumentModified();
                }
            }
        }

        private void ExportJsonFile()
        {
            string path = EditorUtility.SaveFilePanel("Export CUI JSON", "", "CuiLayout.json", "json");
            if (!string.IsNullOrEmpty(path))
            {
                string json = CuiJsonSerializer.SerializeDocument(_document, true);
                File.WriteAllText(path, json);
                Debug.Log($"[RustCUIBuilder] Exported CUI JSON to: {path}");
            }
        }

        private void ExportCSharpFile()
        {
            string path = EditorUtility.SaveFilePanel("Export Oxide Plugin C#", "", "MyCuiPlugin.cs", "cs");
            if (!string.IsNullOrEmpty(path))
            {
                string code = CuiCodeGenerator.GeneratePluginCode(_document);
                File.WriteAllText(path, code);
                Debug.Log($"[RustCUIBuilder] Exported Oxide Plugin C# to: {path}");
            }
        }

        private void ConfigureRustPath()
        {
            string current = SteamDiscovery.GetCustomRustPath();
            if (string.IsNullOrEmpty(current))
            {
                var detected = SteamDiscovery.DiscoverRustInstallation();
                if (detected.IsValid) current = detected.RustRootPath;
            }
            string selected = EditorUtility.OpenFolderPanel("Select Rust Game Directory", current, "");
            if (!string.IsNullOrEmpty(selected))
            {
                SteamDiscovery.SetCustomRustPath(selected);
                RustAssetDiscovery.ReindexAssets();
                Debug.Log($"[RustCUIBuilder] Configured custom Rust game path: {selected}");
            }
        }
    }
}
