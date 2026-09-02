using System;
using System.IO;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Core.Serialization;
using UnityEditor;
using UnityEngine;

namespace RustCUIBuilder.Editor.CodeSync
{
    /// <summary>
    /// Live Code Synchronization view displaying bidirectional generated Oxide C# Plugin code
    /// and raw CUI JSON with export and import workflows.
    /// </summary>
    public class CuiCodeSyncView
    {
        public enum CodeViewTab
        {
            CSharpCode,
            CuiJson,
            ExportSettings
        }

        private CodeViewTab _currentTab = CodeViewTab.CSharpCode;
        private Vector2 _scrollPos;
        private string _generatedCode = "";
        private string _generatedJson = "";
        private string _importInputText = "";
        private bool _isImportMode = false;

        private readonly CodeGeneratorOptions _codeOptions = new CodeGeneratorOptions();

        public void UpdateCode(CuiDocument doc)
        {
            if (doc == null)
            {
                _generatedCode = "";
                _generatedJson = "[]";
                return;
            }

            _generatedCode = CuiCodeGenerator.GeneratePluginCode(doc, _codeOptions);
            _generatedJson = CuiJsonSerializer.SerializeDocument(doc, true);
        }

        public void Draw(Rect rect, CuiDocument doc, Action onModified)
        {
            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginVertical("box");

            // Tab bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Toggle(_currentTab == CodeViewTab.CSharpCode && !_isImportMode, "Oxide C# Code", EditorStyles.toolbarButton))
            {
                _currentTab = CodeViewTab.CSharpCode;
                _isImportMode = false;
            }
            if (GUILayout.Toggle(_currentTab == CodeViewTab.CuiJson && !_isImportMode, "CUI JSON", EditorStyles.toolbarButton))
            {
                _currentTab = CodeViewTab.CuiJson;
                _isImportMode = false;
            }
            if (GUILayout.Toggle(_currentTab == CodeViewTab.ExportSettings && !_isImportMode, "Options", EditorStyles.toolbarButton))
            {
                _currentTab = CodeViewTab.ExportSettings;
                _isImportMode = false;
            }
            if (GUILayout.Toggle(_isImportMode, "Import CUI", EditorStyles.toolbarButton))
            {
                _isImportMode = true;
            }

            GUILayout.FlexibleSpace();

            if (!_isImportMode && _currentTab != CodeViewTab.ExportSettings)
            {
                if (GUILayout.Button("Copy", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    EditorGUIUtility.systemCopyBuffer = _currentTab == CodeViewTab.CSharpCode ? _generatedCode : _generatedJson;
                    Debug.Log("[RustCUIBuilder] Code copied to clipboard!");
                }

                if (GUILayout.Button("Export...", EditorStyles.toolbarButton, GUILayout.Width(65)))
                {
                    ExportToFile();
                }
            }

            EditorGUILayout.EndHorizontal();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (_isImportMode)
            {
                DrawImportView(doc, onModified);
            }
            else if (_currentTab == CodeViewTab.ExportSettings)
            {
                DrawExportSettingsView(doc);
            }
            else
            {
                string textToDisplay = _currentTab == CodeViewTab.CSharpCode ? _generatedCode : _generatedJson;
                var style = new GUIStyle(EditorStyles.textArea)
                {
                    font = Font.CreateDynamicFontFromOSFont("Consolas", 12),
                    fontSize = 12,
                    wordWrap = false
                };
                EditorGUILayout.TextArea(textToDisplay, style, GUILayout.ExpandHeight(true));
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawExportSettingsView(CuiDocument doc)
        {
            EditorGUILayout.LabelField("Code Generator & Export Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            _codeOptions.MethodName = EditorGUILayout.TextField("Method Name", _codeOptions.MethodName);
            _codeOptions.AccessModifier = EditorGUILayout.TextField("Access Modifier", _codeOptions.AccessModifier);
            _codeOptions.PlayerParamType = EditorGUILayout.TextField("Player Param Type", _codeOptions.PlayerParamType);
            _codeOptions.PlayerVariableName = EditorGUILayout.TextField("Player Var Name", _codeOptions.PlayerVariableName);
            _codeOptions.CustomArguments = EditorGUILayout.TextField("Extra Arguments", _codeOptions.CustomArguments);
            _codeOptions.ChatCommandName = EditorGUILayout.TextField("Chat Command Hook", _codeOptions.ChatCommandName);

            EditorGUILayout.Space(6);
            _codeOptions.IncludeMethodWrapper = EditorGUILayout.Toggle("Wrap in Method", _codeOptions.IncludeMethodWrapper);
            _codeOptions.IncludeCommandHook = EditorGUILayout.Toggle("Include [ChatCommand]", _codeOptions.IncludeCommandHook);
            _codeOptions.IncludeUnloadHook = EditorGUILayout.Toggle("Include Unload() Cleanup", _codeOptions.IncludeUnloadHook);
            _codeOptions.UseHighLevelPresets = EditorGUILayout.Toggle("Use CuiPanel / CuiButton", _codeOptions.UseHighLevelPresets);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Pre-Code Injection (run before building UI):");
            _codeOptions.PreCode = EditorGUILayout.TextArea(_codeOptions.PreCode, GUILayout.Height(50));

            EditorGUILayout.LabelField("Post-Code Injection (run after AddUi):");
            _codeOptions.PostCode = EditorGUILayout.TextArea(_codeOptions.PostCode, GUILayout.Height(50));

            if (EditorGUI.EndChangeCheck())
            {
                UpdateCode(doc);
            }
        }

        private void DrawImportView(CuiDocument doc, Action onModified)
        {
            EditorGUILayout.LabelField("Paste Existing Rust CUI JSON to Import Hierarchy", EditorStyles.boldLabel);
            _importInputText = EditorGUILayout.TextArea(_importInputText, GUILayout.Height(200));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Paste From Clipboard", GUILayout.Height(26)))
            {
                _importInputText = EditorGUIUtility.systemCopyBuffer;
            }

            if (GUILayout.Button("Import into Canvas", GUILayout.Height(26)))
            {
                var result = CuiParser.ParseJson(_importInputText);
                if (result.Success && result.Document != null)
                {
                    doc.Elements.Clear();
                    foreach (var elem in result.Document.Elements)
                    {
                        doc.AddElement(elem);
                    }
                    _isImportMode = false;
                    UpdateCode(doc);
                    onModified?.Invoke();
                    Debug.Log($"[RustCUIBuilder] Successfully imported {doc.Elements.Count} CUI elements!");
                }
                else
                {
                    string errors = string.Join("\n", result.Errors);
                    EditorUtility.DisplayDialog("Import Failed", $"Failed to parse CUI JSON:\n{errors}", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ExportToFile()
        {
            string ext = _currentTab == CodeViewTab.CSharpCode ? "cs" : "json";
            string path = EditorUtility.SaveFilePanel("Export Rust CUI Code", "", $"MyCuiPlugin.{ext}", ext);
            if (!string.IsNullOrEmpty(path))
            {
                string content = _currentTab == CodeViewTab.CSharpCode ? _generatedCode : _generatedJson;
                File.WriteAllText(path, content);
                Debug.Log($"[RustCUIBuilder] Saved export to: {path}");
            }
        }
    }
}
