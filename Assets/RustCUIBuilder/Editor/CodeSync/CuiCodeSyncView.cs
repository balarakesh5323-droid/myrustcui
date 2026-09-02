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
            CuiJson
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
            if (GUILayout.Toggle(_isImportMode, "Import CUI", EditorStyles.toolbarButton))
            {
                _isImportMode = true;
            }

            GUILayout.FlexibleSpace();

            if (!_isImportMode)
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
