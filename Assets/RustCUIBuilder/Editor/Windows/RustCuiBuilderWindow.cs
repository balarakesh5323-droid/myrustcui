using System;
using System.IO;
using RustCUIBuilder.Editor.AssetBrowser;
using RustCUIBuilder.Editor.Canvas;
using RustCUIBuilder.Editor.CodeSync;
using RustCUIBuilder.Editor.Hierarchy;
using RustCUIBuilder.Editor.Inspector;
using RustCUIBuilder.Editor.Toolbox;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Core.Serialization;
using RustCUIBuilder.Runtime.Core.Validation;
using RustCUIBuilder.Runtime.Discovery;
using RustCUIBuilder.Runtime.Rendering.Canvas;
using UnityEditor;
using UnityEngine;

namespace RustCUIBuilder.Editor.Windows
{
    /// <summary>
    /// Master dockable Editor Window for the Rust Oxide CUI Visual Builder.
    /// Integrates Hierarchy, Interactive Canvas, Property Inspector, Toolbox, Asset Browser,
    /// Code Sync, and real-time Validation Diagnostics.
    /// </summary>
    public class RustCuiBuilderWindow : EditorWindow
    {
        private CuiDocument _document;
        private readonly CuiCommandHistory _history = new CuiCommandHistory();

        private CuiCanvasEditorView _canvasView;
        private CuiHierarchyView _hierarchyView;
        private CuiInspectorView _inspectorView;
        private CuiToolboxView _toolboxView;
        private CuiAssetBrowserView _assetBrowserView;
        private CuiCodeSyncView _codeSyncView;

        private enum RightBottomTab
        {
            CodeSync,
            ItemBrowser,
            Validation
        }

        private RightBottomTab _rightBottomTab = RightBottomTab.CodeSync;
        private CuiValidationReport _lastValidationReport;
        private string _currentFilePath = "";

        [MenuItem("Rust/CUI Builder (Visual Designer) %#r")]
        public static void ShowWindow()
        {
            var window = GetWindow<RustCuiBuilderWindow>("Rust CUI Builder");
            window.minSize = new Vector2(1000, 650);
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
            _document = new CuiDocument { ProjectName = "MyRustCUI" };

            // Add default root panel
            var rootPanel = new CuiElementNode("MainPanel", "Overlay");
            rootPanel.Components.Add(new CuiImageComponent
            {
                Color = "0.08 0.09 0.12 0.94",
                Sprite = "assets/content/ui/ui.background.tile.psd"
            });
            rootPanel.Components.Add(new CuiRectTransformComponent
            {
                AnchorMin = "0.2 0.2",
                AnchorMax = "0.8 0.8"
            });
            rootPanel.Components.Add(new CuiNeedsCursorComponent());

            // Add Header
            var header = new CuiElementNode("HeaderLabel", "MainPanel");
            header.Components.Add(new CuiTextComponent
            {
                Text = "<b>RUST SERVER MENU</b>",
                FontSize = 18,
                Align = TextAnchor.MiddleCenter,
                Color = "0.95 0.95 0.98 1.0"
            });
            header.Components.Add(new CuiRectTransformComponent
            {
                AnchorMin = "0.05 0.88",
                AnchorMax = "0.95 0.98"
            });

            // Add Close Button
            var closeBtn = new CuiElementNode("CloseButton", "MainPanel");
            closeBtn.Components.Add(new CuiButtonComponent
            {
                Color = "0.75 0.2 0.2 0.9",
                Close = "MainPanel",
                Command = "myui.close"
            });
            closeBtn.Components.Add(new CuiTextComponent
            {
                Text = "✕",
                FontSize = 14,
                Align = TextAnchor.MiddleCenter
            });
            closeBtn.Components.Add(new CuiRectTransformComponent
            {
                AnchorMin = "0.90 0.88",
                AnchorMax = "0.97 0.96"
            });

            _document.AddElement(rootPanel);
            _document.AddElement(header);
            _document.AddElement(closeBtn);

            _document.Select(rootPanel.Id);
            _history.Clear();
            _codeSyncView?.UpdateCode(_document);
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

        private void OnGUI()
        {
            HandleGlobalShortcuts();

            float toolbarHeight = 24f;
            float statusBarHeight = 20f;
            var toolbarRect = new Rect(0, 0, position.width, toolbarHeight);
            var statusRect = new Rect(0, position.height - statusBarHeight, position.width, statusBarHeight);
            var contentRect = new Rect(0, toolbarHeight, position.width, position.height - toolbarHeight - statusBarHeight);

            DrawTopToolbar(toolbarRect);
            DrawMainPanes(contentRect);
            DrawStatusBar(statusRect);
        }

        private void HandleGlobalShortcuts()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                // Ctrl + Z (Undo)
                if (e.control && e.keyCode == KeyCode.Z)
                {
                    _history.Undo();
                    e.Use();
                }
                // Ctrl + Y (Redo)
                else if (e.control && e.keyCode == KeyCode.Y)
                {
                    _history.Redo();
                    e.Use();
                }
                // Ctrl + S (Save)
                else if (e.control && e.keyCode == KeyCode.S)
                {
                    SaveProject();
                    e.Use();
                }
                // Ctrl + D (Duplicate)
                else if (e.control && e.keyCode == KeyCode.D)
                {
                    var selected = _document.PrimarySelectedElement;
                    if (selected != null)
                    {
                        var dup = selected.Clone(true);
                        _document.AddElement(dup);
                        _document.Select(dup.Id);
                        e.Use();
                    }
                }
                // Delete
                else if (e.keyCode == KeyCode.Delete)
                {
                    var selected = _document.PrimarySelectedElement;
                    if (selected != null)
                    {
                        _document.RemoveElement(selected.Id, true);
                        e.Use();
                    }
                }
            }
        }

        private void DrawTopToolbar(Rect rect)
        {
            GUILayout.BeginArea(rect, EditorStyles.toolbar);
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("Rust CUI Builder", EditorStyles.boldLabel, GUILayout.Width(110));

            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                if (EditorUtility.DisplayDialog("New Project", "Create a new CUI project?", "Yes", "No"))
                {
                    CreateNewDocument();
                }
            }

            if (GUILayout.Button("Open...", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                OpenProject();
            }

            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(45)))
            {
                SaveProject();
            }

            GUILayout.Space(10);

            // Undo / Redo
            GUI.enabled = _history.CanUndo;
            if (GUILayout.Button("↶ Undo", EditorStyles.toolbarButton, GUILayout.Width(50))) _history.Undo();
            GUI.enabled = _history.CanRedo;
            if (GUILayout.Button("↷ Redo", EditorStyles.toolbarButton, GUILayout.Width(50))) _history.Redo();
            GUI.enabled = true;

            GUILayout.Space(10);

            // Resolution Presets Dropdown
            GUILayout.Label("Resolution:", EditorStyles.miniLabel, GUILayout.Width(65));
            int currentResIdx = RustResolutionPreset.Presets.IndexOf(_canvasView.CurrentPreset);
            if (currentResIdx < 0) currentResIdx = 3;

            string[] resNames = RustResolutionPreset.Presets.ConvertAll(p => p.Name).ToArray();
            int nextResIdx = EditorGUILayout.Popup(currentResIdx, resNames, EditorStyles.toolbarDropDown, GUILayout.Width(220));
            if (nextResIdx != currentResIdx && nextResIdx >= 0 && nextResIdx < RustResolutionPreset.Presets.Count)
            {
                _canvasView.CurrentPreset = RustResolutionPreset.Presets[nextResIdx];
            }

            GUILayout.FlexibleSpace();

            // Rust Discovery Status Indicator
            string installStatus = RustAssetDiscovery.IsIndexed ? $"✓ Rust Linked ({RustAssetDiscovery.TotalItemCount} Items)" : "⚠ Rust Not Linked";
            GUILayout.Label(installStatus, EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawMainPanes(Rect rect)
        {
            float leftWidth = 240f;
            float rightWidth = 340f;
            float centerWidth = rect.width - leftWidth - rightWidth;

            var leftRect = new Rect(rect.x, rect.y, leftWidth, rect.height);
            var centerRect = new Rect(rect.x + leftWidth, rect.y, centerWidth, rect.height);
            var rightRect = new Rect(rect.x + leftWidth + centerWidth, rect.y, rightWidth, rect.height);

            // Left Pane (Hierarchy + Toolbox)
            float leftSplit = rect.height * 0.55f;
            var hierarchyRect = new Rect(leftRect.x, leftRect.y, leftRect.width, leftSplit);
            var toolboxRect = new Rect(leftRect.x, leftRect.y + leftSplit, leftRect.width, leftRect.height - leftSplit);

            _hierarchyView.Draw(hierarchyRect, _document, OnDocumentModified);
            _toolboxView.Draw(toolboxRect, _document, OnDocumentModified);

            // Center Pane (Interactive Canvas)
            _canvasView.Draw(centerRect, _document, OnDocumentModified);

            // Right Pane (Inspector + Bottom Tab)
            float rightSplit = rect.height * 0.52f;
            var inspectorRect = new Rect(rightRect.x, rightRect.y, rightRect.width, rightSplit);
            var bottomTabRect = new Rect(rightRect.x, rightRect.y + rightSplit, rightRect.width, rightRect.height - rightSplit);

            _inspectorView.Draw(inspectorRect, _document, OnDocumentModified);

            // Right Bottom Tabs
            DrawRightBottomTabs(bottomTabRect);
        }

        private void DrawRightBottomTabs(Rect rect)
        {
            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginVertical("box");

            // Tab Buttons
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Toggle(_rightBottomTab == RightBottomTab.CodeSync, "Live Code / Export", EditorStyles.toolbarButton)) _rightBottomTab = RightBottomTab.CodeSync;
            if (GUILayout.Toggle(_rightBottomTab == RightBottomTab.ItemBrowser, "Rust Assets", EditorStyles.toolbarButton)) _rightBottomTab = RightBottomTab.ItemBrowser;

            string diagTitle = _lastValidationReport != null && _lastValidationReport.ErrorCount > 0 ? $"Diagnostics ({_lastValidationReport.ErrorCount} ✕)" : "Diagnostics (✓)";
            if (GUILayout.Toggle(_rightBottomTab == RightBottomTab.Validation, diagTitle, EditorStyles.toolbarButton)) _rightBottomTab = RightBottomTab.Validation;
            EditorGUILayout.EndHorizontal();

            var innerRect = new Rect(0, 24, rect.width, rect.height - 24);

            switch (_rightBottomTab)
            {
                case RightBottomTab.CodeSync:
                    _codeSyncView.Draw(innerRect, _document, OnDocumentModified);
                    break;
                case RightBottomTab.ItemBrowser:
                    _assetBrowserView.Draw(innerRect, _document, OnDocumentModified);
                    break;
                case RightBottomTab.Validation:
                    DrawValidationView(innerRect);
                    break;
            }

            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawValidationView(Rect rect)
        {
            GUILayout.BeginArea(rect);
            if (_lastValidationReport == null || _lastValidationReport.Diagnostics.Count == 0)
            {
                EditorGUILayout.HelpBox("✓ Document structure and Rust CUI semantic validation passed with 0 errors.", MessageType.Info);
            }
            else
            {
                foreach (var diag in _lastValidationReport.Diagnostics)
                {
                    MessageType mType = diag.Severity == DiagnosticSeverity.Error ? MessageType.Error :
                                       (diag.Severity == DiagnosticSeverity.Warning ? MessageType.Warning : MessageType.Info);
                    EditorGUILayout.HelpBox($"[{diag.RuleId}] {diag.ElementName}: {diag.Message}", mType);
                }
            }
            GUILayout.EndArea();
        }

        private void DrawStatusBar(Rect rect)
        {
            GUILayout.BeginArea(rect, EditorStyles.toolbar);
            EditorGUILayout.BeginHorizontal();

            int elemCount = _document != null ? _document.Elements.Count : 0;
            var selected = _document?.PrimarySelectedElement;
            string selInfo = selected != null ? $"Selected: {selected.Name} ({selected.Parent})" : "No Selection";

            GUILayout.Label($"Elements: {elemCount}  |  {selInfo}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            string validStatus = _lastValidationReport != null && _lastValidationReport.IsValid ? "✓ Valid CUI" : $"⚠ {_lastValidationReport?.ErrorCount ?? 0} Errors";
            GUILayout.Label(validStatus, EditorStyles.miniBoldLabel);

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void SaveProject()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                _currentFilePath = EditorUtility.SaveFilePanel("Save CUI Project", "", $"{_document.ProjectName}.cui", "cui");
            }

            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                string json = CuiJsonSerializer.SerializeDocument(_document, true);
                File.WriteAllText(_currentFilePath, json);
                Debug.Log($"[RustCUIBuilder] Project saved to: {_currentFilePath}");
            }
        }

        private void OpenProject()
        {
            string path = EditorUtility.OpenFilePanel("Open CUI Project", "", "cui,json");
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var result = CuiParser.ParseJson(json, Path.GetFileNameWithoutExtension(path));
                if (result.Success && result.Document != null)
                {
                    _document = result.Document;
                    _currentFilePath = path;
                    _document.OnDocumentModified += OnDocumentModified;
                    _document.OnSelectionChanged += Repaint;
                    OnDocumentModified();
                    Debug.Log($"[RustCUIBuilder] Loaded project with {_document.Elements.Count} elements.");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error Loading Project", string.Join("\n", result.Errors), "OK");
                }
            }
        }
    }
}
