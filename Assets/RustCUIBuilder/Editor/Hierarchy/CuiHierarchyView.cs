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
            if (GUILayout.Button("+ Element", EditorStyles.toolbarButton, GUILayout.Width(68)))
            {
                var newElem = new CuiElementNode("Panel", "Overlay");
                newElem.Components.Add(new CuiRectTransformComponent
                {
                    AnchorMin = "0.3 0.3",
                    AnchorMax = "0.7 0.7"
                });
                newElem.Components.Add(new CuiImageComponent
                {
                    Color = "0.15 0.17 0.22 0.9",
                    Sprite = "assets/content/ui/ui.background.tile.psd"
                });
                doc.AddElement(newElem);
                doc.Select(newElem.Id);
                onModified?.Invoke();
            }
            EditorGUILayout.EndHorizontal();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (doc != null && doc.Elements != null)
            {
                // Active layers (layers with elements) + Common layers
                var rootLayers = RustAssetDiscovery.VerifiedLayers;
                foreach (var layer in rootLayers)
                {
                    var layerChildren = doc.Elements.Where(e => string.Equals(e.Parent, layer, StringComparison.OrdinalIgnoreCase)).ToList();
                    // Only draw layers that have children or the standard Overlay layer
                    if (layerChildren.Count > 0 || layer == "Overlay")
                    {
                        DrawLayerGroup(layer, layerChildren, doc, onModified);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawLayerGroup(string layerName, List<CuiElementNode> layerChildren, CuiDocument doc, Action onModified)
        {
            bool isCollapsed = _collapsedLayers.Contains(layerName);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            string arrow = isCollapsed ? "►" : "▼";
            if (GUILayout.Button($"{arrow} {layerName} ({layerChildren.Count})", EditorStyles.label, GUILayout.Height(18)))
            {
                if (isCollapsed) _collapsedLayers.Remove(layerName);
                else _collapsedLayers.Add(layerName);
            }

            // Quick add to this layer
            if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(22)))
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
                foreach (var elem in layerChildren)
                {
                    DrawElementTreeItem(elem, doc, onModified, 1);
                }
            }
        }

        private void DrawElementTreeItem(CuiElementNode elem, CuiDocument doc, Action onModified, int depth)
        {
            if (!string.IsNullOrEmpty(_searchFilter) && elem.Name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            bool isSelected = doc.IsSelected(elem.Id);

            var rowRect = EditorGUILayout.BeginHorizontal();
            if (isSelected)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.18f, 0.42f, 0.7f, 0.45f));
            }

            // Indentation
            GUILayout.Space(depth * 16);

            // Visibility Icon
            string visIcon = elem.IsHidden ? "⊘" : "👁";
            if (GUILayout.Button(visIcon, EditorStyles.miniLabel, GUILayout.Width(18)))
            {
                elem.IsHidden = !elem.IsHidden;
                onModified?.Invoke();
            }

            // Lock Icon
            string lockIcon = elem.IsLocked ? "🔒" : "🔓";
            if (GUILayout.Button(lockIcon, EditorStyles.miniLabel, GUILayout.Width(18)))
            {
                elem.IsLocked = !elem.IsLocked;
                onModified?.Invoke();
            }

            // Element Type Badge
            string typeBadge = GetElementTypeBadge(elem);
            var badgeCol = GetTypeColor(typeBadge);
            var prevCol = GUI.color;
            GUI.color = badgeCol;
            GUILayout.Label(typeBadge, EditorStyles.miniLabel, GUILayout.Width(46));
            GUI.color = prevCol;

            // Element Name (Click to select)
            var nameStyle = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
            if (GUILayout.Button(elem.Name, nameStyle))
            {
                doc.Select(elem.Id, Event.current.shift || Event.current.control);
            }

            GUILayout.FlexibleSpace();

            // More actions context menu
            if (GUILayout.Button("...", EditorStyles.miniButton, GUILayout.Width(22)))
            {
                ShowElementContextMenu(elem, doc, onModified);
            }

            EditorGUILayout.EndHorizontal();

            // Children recursion
            var children = doc.GetChildrenOf(elem.Name);
            foreach (var child in children)
            {
                DrawElementTreeItem(child, doc, onModified, depth + 1);
            }
        }

        private string GetElementTypeBadge(CuiElementNode elem)
        {
            if (elem.HasComponent<CuiButtonComponent>()) return "[Button]";
            if (elem.HasComponent<CuiInputFieldComponent>()) return "[Input]";
            if (elem.HasComponent<CuiTextComponent>()) return "[Text]";
            if (elem.HasComponent<CuiCountdownComponent>()) return "[Timer]";
            if (elem.HasComponent<CuiScrollViewComponent>()) return "[Scroll]";
            if (elem.HasComponent<CuiImageComponent>()) return "[Image]";
            if (elem.HasComponent<CuiRawImageComponent>()) return "[RawImg]";
            return "[Panel]";
        }

        private Color GetTypeColor(string badge)
        {
            switch (badge)
            {
                case "[Button]": return new Color(0.4f, 0.9f, 0.5f, 1f);
                case "[Text]": return new Color(0.9f, 0.85f, 0.4f, 1f);
                case "[Input]": return new Color(0.9f, 0.5f, 0.9f, 1f);
                case "[Image]": return new Color(0.4f, 0.75f, 1f, 1f);
                case "[Timer]": return new Color(1f, 0.6f, 0.3f, 1f);
                default: return new Color(0.7f, 0.75f, 0.8f, 0.9f);
            }
        }

        private void ShowElementContextMenu(CuiElementNode elem, CuiDocument doc, Action onModified)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Duplicate"), false, () =>
            {
                var clone = elem.Clone(true, $"{elem.Name}_Copy");
                doc.AddElement(clone);
                doc.Select(clone.Id);
                onModified?.Invoke();
            });

            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                doc.RemoveElement(elem.Id);
                onModified?.Invoke();
            });

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Add Child/Panel"), false, () =>
            {
                var child = new CuiElementNode($"{elem.Name}_Child", elem.Name);
                child.Components.Add(new CuiRectTransformComponent());
                child.Components.Add(new CuiImageComponent());
                doc.AddElement(child);
                doc.Select(child.Id);
                onModified?.Invoke();
            });
            menu.AddItem(new GUIContent("Add Child/Label"), false, () =>
            {
                var child = new CuiElementNode($"{elem.Name}_Text", elem.Name);
                child.Components.Add(new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" });
                child.Components.Add(new CuiTextComponent { Text = "Child Label" });
                doc.AddElement(child);
                doc.Select(child.Id);
                onModified?.Invoke();
            });
            menu.AddItem(new GUIContent("Add Child/Button"), false, () =>
            {
                var child = new CuiElementNode($"{elem.Name}_Btn", elem.Name);
                child.Components.Add(new CuiRectTransformComponent { AnchorMin = "0.1 0.1", AnchorMax = "0.9 0.9" });
                child.Components.Add(new CuiButtonComponent { Command = "action.exec" });
                child.Components.Add(new CuiTextComponent { Text = "Button", Align = TextAnchor.MiddleCenter });
                doc.AddElement(child);
                doc.Select(child.Id);
                onModified?.Invoke();
            });

            menu.ShowAsContext();
        }
    }
}
