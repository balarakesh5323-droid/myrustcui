using System;
using System.Collections.Generic;
using System.Linq;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Discovery;
using UnityEditor;
using UnityEngine;

namespace RustCUIBuilder.Editor.Hierarchy
{
    /// <summary>
    /// Visual hierarchy tree view with search, layer grouping, drag-drop ordering,
    /// duplicate, delete, lock, and visibility controls.
    /// </summary>
    public class CuiHierarchyView
    {
        private Vector2 _scrollPos;
        private string _searchFilter = "";
        private readonly HashSet<string> _collapsedLayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public void Draw(Rect rect, CuiDocument doc, Action onModified)
        {
            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginVertical("box");

            // Header Toolbar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Hierarchy", EditorStyles.boldLabel, GUILayout.Width(70));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("+ Element", EditorStyles.toolbarButton, GUILayout.Width(65)))
            {
                var newElem = new CuiElementNode("Element", "Overlay");
                newElem.Components.Add(new CuiRectTransformComponent());
                doc.AddElement(newElem);
                doc.Select(newElem.Id);
                onModified?.Invoke();
            }
            EditorGUILayout.EndHorizontal();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (doc != null && doc.Elements != null)
            {
                // Group by Root Layer
                var rootLayers = RustAssetDiscovery.VerifiedLayers;
                foreach (var layer in rootLayers)
                {
                    DrawLayerGroup(layer, doc, onModified);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawLayerGroup(string layerName, CuiDocument doc, Action onModified)
        {
            var layerChildren = doc.Elements.Where(e => string.Equals(e.Parent, layerName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (layerChildren.Count == 0 && !string.IsNullOrEmpty(_searchFilter)) return;

            bool isCollapsed = _collapsedLayers.Contains(layerName);

            EditorGUILayout.BeginHorizontal();
            string foldoutLabel = $"{(isCollapsed ? "►" : "▼")} [{layerName}] ({layerChildren.Count})";
            if (GUILayout.Button(foldoutLabel, EditorStyles.label, GUILayout.Height(18)))
            {
                if (isCollapsed) _collapsedLayers.Remove(layerName);
                else _collapsedLayers.Add(layerName);
            }

            // Quick add to this layer
            if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                var newElem = new CuiElementNode($"{layerName}_Item", layerName);
                newElem.Components.Add(new CuiRectTransformComponent());
                doc.AddElement(newElem);
                doc.Select(newElem.Id);
                onModified?.Invoke();
            }
            EditorGUILayout.EndHorizontal();

            if (!isCollapsed)
            {
                EditorGUI.indentLevel++;
                foreach (var elem in layerChildren)
                {
                    DrawElementTreeItem(elem, doc, onModified, 1);
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawElementTreeItem(CuiElementNode elem, CuiDocument doc, Action onModified, int depth)
        {
            // Search filter
            if (!string.IsNullOrEmpty(_searchFilter) && !elem.Name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase).Equals(-1))
            {
                // Match
            }
            else if (!string.IsNullOrEmpty(_searchFilter))
            {
                return;
            }

            bool isSelected = doc.SelectedIds.Contains(elem.Id);

            var rowRect = EditorGUILayout.BeginHorizontal();
            if (isSelected)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.2f, 0.45f, 0.7f, 0.4f));
            }

            // Indentation
            GUILayout.Space(depth * 14);

            // Visibility Icon
            string visIcon = elem.IsHidden ? "[-]" : "[o]";
            if (GUILayout.Button(visIcon, EditorStyles.label, GUILayout.Width(22)))
            {
                elem.IsHidden = !elem.IsHidden;
                onModified?.Invoke();
            }

            // Lock Icon
            string lockIcon = elem.IsLocked ? "[L]" : "[ ]";
            if (GUILayout.Button(lockIcon, EditorStyles.label, GUILayout.Width(20)))
            {
                elem.IsLocked = !elem.IsLocked;
                onModified?.Invoke();
            }

            // Element Name / Select Button
            string typeIcon = GetTypeIcon(elem);
            var nameStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = isSelected ? Color.cyan : (elem.IsHidden ? Color.gray : Color.white) }
            };

            if (GUILayout.Button($"{typeIcon} {elem.Name}", nameStyle))
            {
                doc.Select(elem.Id, Event.current.shift || Event.current.control);
            }

            // Action Context Menu
            if (GUILayout.Button("...", EditorStyles.miniButton, GUILayout.Width(22)))
            {
                ShowContextMenu(elem, doc, onModified);
            }

            EditorGUILayout.EndHorizontal();

            // Draw Children
            var children = doc.GetChildrenOf(elem.Name);
            if (children.Count > 0 && elem.IsExpanded)
            {
                foreach (var child in children)
                {
                    DrawElementTreeItem(child, doc, onModified, depth + 1);
                }
            }
        }

        private void ShowContextMenu(CuiElementNode elem, CuiDocument doc, Action onModified)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Add Child Element"), false, () =>
            {
                var child = new CuiElementNode($"{elem.Name}_Child", elem.Name);
                child.Components.Add(new CuiRectTransformComponent());
                doc.AddElement(child);
                doc.Select(child.Id);
                onModified?.Invoke();
            });

            menu.AddItem(new GUIContent("Duplicate (Ctrl+D)"), false, () =>
            {
                var dup = elem.Clone(true, $"{elem.Name}_Copy");
                doc.AddElement(dup);
                doc.Select(dup.Id);
                onModified?.Invoke();
            });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Delete (Del)"), false, () =>
            {
                doc.RemoveElement(elem.Id, true);
                onModified?.Invoke();
            });

            menu.ShowAsContext();
        }

        private string GetTypeIcon(CuiElementNode elem)
        {
            if (elem.HasComponent<CuiButtonComponent>()) return "[Btn]";
            if (elem.HasComponent<CuiInputFieldComponent>()) return "[Input]";
            if (elem.HasComponent<CuiTextComponent>()) return "[Txt]";
            if (elem.HasComponent<CuiImageComponent>()) return "[Img]";
            if (elem.HasComponent<CuiRawImageComponent>()) return "[Raw]";
            if (elem.HasComponent<CuiCountdownComponent>()) return "[Timer]";
            if (elem.HasComponent<CuiScrollViewComponent>()) return "[Scroll]";
            return "[Box]";
        }
    }
}
