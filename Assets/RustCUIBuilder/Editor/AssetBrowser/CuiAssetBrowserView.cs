using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Discovery;

namespace RustCUIBuilder.Editor.AssetBrowser
{
    public class CuiAssetBrowserView
    {
        private enum AssetTab
        {
            UiSprites,
            GameItems,
            Materials,
            Fonts
        }

        private AssetTab _currentTab = AssetTab.UiSprites;
        private string _searchFilter = "";
        private Vector2 _scrollPos;
        private int _currentPage = 0;
        private const int ItemsPerPage = 60;

        public void Draw(Rect rect, CuiDocument doc, Action onModified)
        {
            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginVertical();

            // Navigation Tabs
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var tabs = new[]
            {
                $"Sprites & Icons ({RustAssetDiscovery.AllUiSprites.Count})",
                $"Game Items ({RustAssetDiscovery.AllItems.Count})",
                $"Materials ({RustAssetDiscovery.VerifiedMaterials.Length})",
                $"Fonts ({RustAssetDiscovery.VerifiedFonts.Length})"
            };

            int newTab = GUILayout.Toolbar((int)_currentTab, tabs, EditorStyles.toolbarButton);
            if (newTab != (int)_currentTab)
            {
                _currentTab = (AssetTab)newTab;
                _currentPage = 0;
                _searchFilter = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();

            // Search Bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("🔍", GUILayout.Width(18));
            string newSearch = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            if (newSearch != _searchFilter)
            {
                _searchFilter = newSearch;
                _currentPage = 0;
            }
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
                {
                    _searchFilter = "";
                    _currentPage = 0;
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Tab Content
            switch (_currentTab)
            {
                case AssetTab.UiSprites:
                    DrawUiSpritesGrid(rect, doc, onModified);
                    break;
                case AssetTab.GameItems:
                    DrawGameItemsGrid(rect, doc, onModified);
                    break;
                case AssetTab.Materials:
                    DrawMaterialsList(rect, doc, onModified);
                    break;
                case AssetTab.Fonts:
                    DrawFontsList(rect, doc, onModified);
                    break;
            }

            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawUiSpritesGrid(Rect rect, CuiDocument doc, Action onModified)
        {
            var allSprites = RustAssetDiscovery.AllUiSprites;
            var filtered = string.IsNullOrEmpty(_searchFilter)
                ? allSprites
                : allSprites.Where(s => s.name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       s.path.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            int totalCount = filtered.Count;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalCount / ItemsPerPage));
            _currentPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);

            var pageItems = filtered.Skip(_currentPage * ItemsPerPage).Take(ItemsPerPage).ToList();

            // Pagination Header
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Total Sprites: {totalCount} | Page {_currentPage + 1}/{totalPages}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            GUI.enabled = _currentPage > 0;
            if (GUILayout.Button("◀ Prev", EditorStyles.miniButton, GUILayout.Width(46))) _currentPage--;
            GUI.enabled = _currentPage < totalPages - 1;
            if (GUILayout.Button("Next ▶", EditorStyles.miniButton, GUILayout.Width(46))) _currentPage++;
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            int columns = Mathf.Max(2, Mathf.FloorToInt((rect.width - 24) / 78f));
            int rows = Mathf.CeilToInt((float)pageItems.Count / columns);

            for (int r = 0; r < rows; r++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < columns; c++)
                {
                    int idx = r * columns + c;
                    if (idx < pageItems.Count)
                    {
                        var spriteMeta = pageItems[idx];
                        DrawSpriteCell(spriteMeta, doc, onModified);
                    }
                    else
                    {
                        GUILayout.Space(78);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSpriteCell(RustAssetDiscovery.UiSpriteMetadata spriteMeta, CuiDocument doc, Action onModified)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(76), GUILayout.Height(86));

            if (spriteMeta.sprite == null)
            {
                spriteMeta.sprite = RustAssetDiscovery.GetSpriteByPath(spriteMeta.path);
            }

            var spr = spriteMeta.sprite;
            var cellRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));

            // Slot Background Box
            EditorGUI.DrawRect(cellRect, new Color(0.12f, 0.14f, 0.18f, 0.95f));

            // Sharp UV-Aware Texture Rendering
            if (spr != null && spr.texture != null)
            {
                Rect uv = new Rect(
                    spr.rect.x / spr.texture.width,
                    spr.rect.y / spr.texture.height,
                    spr.rect.width / spr.texture.width,
                    spr.rect.height / spr.texture.height
                );

                float aspect = spr.rect.width / Mathf.Max(1f, spr.rect.height);
                Rect drawRect;
                if (aspect >= 1f)
                {
                    float h = (cellRect.width - 8) / aspect;
                    drawRect = new Rect(cellRect.x + 4, cellRect.y + 4 + (cellRect.height - 8 - h) * 0.5f, cellRect.width - 8, h);
                }
                else
                {
                    float w = (cellRect.height - 8) * aspect;
                    drawRect = new Rect(cellRect.x + 4 + (cellRect.width - 8 - w) * 0.5f, cellRect.y + 4, w, cellRect.height - 8);
                }

                GUI.DrawTextureWithTexCoords(drawRect, spr.texture, uv);
            }

            // Click Overlay Button
            string provenance = spriteMeta.isBundleLoaded ? "AUTHENTIC RUST ASSET" : "PROCEDURAL FALLBACK";
            string tooltip = $"{spriteMeta.name}\n[{provenance}]\nPath: {spriteMeta.path}\n(Click to apply to selected element)";
            if (GUI.Button(cellRect, new GUIContent("", tooltip), GUIStyle.none))
            {
                var selected = doc?.PrimarySelectedElement;
                if (selected != null)
                {
                    var img = selected.GetComponent<CuiImageComponent>();
                    var btn = selected.GetComponent<CuiButtonComponent>();
                    if (img != null)
                    {
                        img.Sprite = spriteMeta.path;
                        img.ItemId = 0;
                    }
                    else if (btn != null)
                    {
                        btn.Material = spriteMeta.path;
                    }
                    else
                    {
                        img = selected.GetOrCreateComponent<CuiImageComponent>();
                        img.Sprite = spriteMeta.path;
                        img.ItemId = 0;
                    }
                    onModified?.Invoke();
                }
            }

            // Highlight border on hover
            if (cellRect.Contains(Event.current.mousePosition))
            {
                Handles.BeginGUI();
                Handles.color = new Color(0.2f, 0.75f, 1f, 0.8f);
                Handles.DrawPolyLine(
                    new Vector3(cellRect.xMin, cellRect.yMin, 0),
                    new Vector3(cellRect.xMax, cellRect.yMin, 0),
                    new Vector3(cellRect.xMax, cellRect.yMax, 0),
                    new Vector3(cellRect.xMin, cellRect.yMax, 0),
                    new Vector3(cellRect.xMin, cellRect.yMin, 0)
                );
                Handles.EndGUI();
            }

            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                clipping = TextClipping.Clip
            };
            GUILayout.Label(spriteMeta.name, labelStyle, GUILayout.Width(64), GUILayout.Height(16));

            EditorGUILayout.EndVertical();
        }

        private void DrawGameItemsGrid(Rect rect, CuiDocument doc, Action onModified)
        {
            var allItems = RustAssetDiscovery.AllItems;
            var filtered = string.IsNullOrEmpty(_searchFilter)
                ? allItems
                : allItems.Where(i => i.displayName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      i.shortname.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            int totalCount = filtered.Count;
            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)totalCount / ItemsPerPage));
            _currentPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);

            var pageItems = filtered.Skip(_currentPage * ItemsPerPage).Take(ItemsPerPage).ToList();

            // Pagination Header
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Items: {totalCount} | Page {_currentPage + 1}/{totalPages}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            GUI.enabled = _currentPage > 0;
            if (GUILayout.Button("◀ Prev", EditorStyles.miniButton, GUILayout.Width(46))) _currentPage--;
            GUI.enabled = _currentPage < totalPages - 1;
            if (GUILayout.Button("Next ▶", EditorStyles.miniButton, GUILayout.Width(46))) _currentPage++;
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            int columns = Mathf.Max(2, Mathf.FloorToInt((rect.width - 24) / 78f));
            int rows = Mathf.CeilToInt((float)pageItems.Count / columns);

            for (int r = 0; r < rows; r++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < columns; c++)
                {
                    int idx = r * columns + c;
                    if (idx < pageItems.Count)
                    {
                        var item = pageItems[idx];
                        DrawItemCell(item, doc, onModified);
                    }
                    else
                    {
                        GUILayout.Space(78);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawItemCell(RustAssetDiscovery.ItemAssetMetadata item, CuiDocument doc, Action onModified)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(76), GUILayout.Height(86));

            var sprite = RustAssetDiscovery.LoadItemIcon(item);
            var cellRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));

            // Slot Background Box
            EditorGUI.DrawRect(cellRect, new Color(0.12f, 0.14f, 0.18f, 0.95f));

            if (sprite != null && sprite.texture != null)
            {
                Rect uv = new Rect(
                    sprite.rect.x / sprite.texture.width,
                    sprite.rect.y / sprite.texture.height,
                    sprite.rect.width / sprite.texture.width,
                    sprite.rect.height / sprite.texture.height
                );

                Rect drawRect = new Rect(cellRect.x + 4, cellRect.y + 4, cellRect.width - 8, cellRect.height - 8);
                GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, uv);
            }

            string tooltip = $"{item.displayName}\n(ID: {item.itemId})\nShortname: {item.shortname}\n(Click to apply to selected element)";
            if (GUI.Button(cellRect, new GUIContent("", tooltip), GUIStyle.none))
            {
                var selected = doc?.PrimarySelectedElement;
                if (selected != null)
                {
                    var img = selected.GetOrCreateComponent<CuiImageComponent>();
                    img.ItemId = item.itemId;
                    img.Sprite = "";
                    onModified?.Invoke();
                }
            }

            // Highlight border on hover
            if (cellRect.Contains(Event.current.mousePosition))
            {
                Handles.BeginGUI();
                Handles.color = new Color(0.2f, 0.75f, 1f, 0.8f);
                Handles.DrawPolyLine(
                    new Vector3(cellRect.xMin, cellRect.yMin, 0),
                    new Vector3(cellRect.xMax, cellRect.yMin, 0),
                    new Vector3(cellRect.xMax, cellRect.yMax, 0),
                    new Vector3(cellRect.xMin, cellRect.yMax, 0),
                    new Vector3(cellRect.xMin, cellRect.yMin, 0)
                );
                Handles.EndGUI();
            }

            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
                clipping = TextClipping.Clip
            };
            GUILayout.Label(item.displayName, labelStyle, GUILayout.Width(64), GUILayout.Height(16));

            EditorGUILayout.EndVertical();
        }

        private void DrawMaterialsList(Rect rect, CuiDocument doc, Action onModified)
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("Authentic Rust UI Materials", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Click any verified material to apply it to the selected element's Image or Button component.", MessageType.Info);
            EditorGUILayout.Space(4);

            foreach (var matPath in RustAssetDiscovery.VerifiedMaterials)
            {
                EditorGUILayout.BeginHorizontal("box");
                string matName = Path.GetFileName(matPath);

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(matName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(matPath, EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                if (GUILayout.Button("Apply Material", GUILayout.Width(110)))
                {
                    var selected = doc?.PrimarySelectedElement;
                    if (selected != null)
                    {
                        var img = selected.GetComponent<CuiImageComponent>();
                        var btn = selected.GetComponent<CuiButtonComponent>();
                        if (img != null) img.Material = matPath;
                        else if (btn != null) btn.Material = matPath;
                        else
                        {
                            img = selected.GetOrCreateComponent<CuiImageComponent>();
                            img.Material = matPath;
                        }
                        onModified?.Invoke();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawFontsList(Rect rect, CuiDocument doc, Action onModified)
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.LabelField("Authentic Rust Fonts", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Click any font to apply it to the selected Text component.", MessageType.Info);
            EditorGUILayout.Space(4);

            foreach (var fontName in RustAssetDiscovery.VerifiedFonts)
            {
                EditorGUILayout.BeginHorizontal("box");

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(fontName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Rust Font: {fontName}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();

                if (GUILayout.Button("Apply Font", GUILayout.Width(110)))
                {
                    var selected = doc?.PrimarySelectedElement;
                    if (selected != null)
                    {
                        var text = selected.GetOrCreateComponent<CuiTextComponent>();
                        text.Font = fontName;
                        onModified?.Invoke();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
