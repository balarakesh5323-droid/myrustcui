using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Discovery;

namespace RustCUIBuilder.Editor.AssetBrowser
{
    public enum AssetCategoryTab
    {
        UiSprites,
        GameItems,
        Materials,
        Fonts
    }

    /// <summary>
    /// Professional asset browser displaying authentic Rust Steam AssetBundle sprites,
    /// 1,723 game item icons, verified materials, and fonts with live search and click-to-apply.
    /// </summary>
    public class CuiAssetBrowserView
    {
        private AssetCategoryTab _currentTab = AssetCategoryTab.UiSprites;
        private Vector2 _scrollPos;
        private string _searchFilter = "";
        private int _currentPage = 0;
        private const int ItemsPerPage = 80;

        public void Draw(Rect rect, CuiDocument doc, Action onModified)
        {
            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginVertical("box");

            if (!RustAssetDiscovery.IsIndexed)
            {
                RustAssetDiscovery.ReindexAssets();
            }

            // Header Toolbar & Tabs
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Toggle(_currentTab == AssetCategoryTab.UiSprites, $"Sprites & Icons ({RustAssetDiscovery.TotalSpriteCount})", EditorStyles.toolbarButton))
            {
                if (_currentTab != AssetCategoryTab.UiSprites) { _currentTab = AssetCategoryTab.UiSprites; _currentPage = 0; }
            }
            if (GUILayout.Toggle(_currentTab == AssetCategoryTab.GameItems, $"Game Items ({RustAssetDiscovery.TotalItemCount})", EditorStyles.toolbarButton))
            {
                if (_currentTab != AssetCategoryTab.GameItems) { _currentTab = AssetCategoryTab.GameItems; _currentPage = 0; }
            }
            if (GUILayout.Toggle(_currentTab == AssetCategoryTab.Materials, $"Materials ({RustAssetDiscovery.VerifiedMaterials.Length})", EditorStyles.toolbarButton))
            {
                if (_currentTab != AssetCategoryTab.Materials) { _currentTab = AssetCategoryTab.Materials; _currentPage = 0; }
            }
            if (GUILayout.Toggle(_currentTab == AssetCategoryTab.Fonts, $"Fonts ({RustAssetDiscovery.VerifiedFonts.Length})", EditorStyles.toolbarButton))
            {
                if (_currentTab != AssetCategoryTab.Fonts) { _currentTab = AssetCategoryTab.Fonts; _currentPage = 0; }
            }

            GUILayout.FlexibleSpace();

            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(130));

            if (GUILayout.Button("Re-Scan", EditorStyles.toolbarButton, GUILayout.Width(55)))
            {
                RustBundleManager.Reload();
                RustAssetDiscovery.ReindexAssets();
            }
            EditorGUILayout.EndHorizontal();

            switch (_currentTab)
            {
                case AssetCategoryTab.UiSprites:
                    DrawUiSpritesGrid(rect, doc, onModified);
                    break;
                case AssetCategoryTab.GameItems:
                    DrawGameItemsGrid(rect, doc, onModified);
                    break;
                case AssetCategoryTab.Materials:
                    DrawMaterialsList(doc, onModified);
                    break;
                case AssetCategoryTab.Fonts:
                    DrawFontsList(doc, onModified);
                    break;
            }

            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawUiSpritesGrid(Rect rect, CuiDocument doc, Action onModified)
        {
            var sprites = RustAssetDiscovery.AllUiSprites;
            var filtered = string.IsNullOrEmpty(_searchFilter)
                ? sprites
                : sprites.Where(s => s.name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
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

            var tex = spriteMeta.sprite != null ? spriteMeta.sprite.texture : Texture2D.whiteTexture;
            string provenance = spriteMeta.isBundleLoaded ? "AUTHENTIC RUST ASSET" : "PROCEDURAL FALLBACK";
            var btnContent = new GUIContent(tex, $"{spriteMeta.name}\n[{provenance}]\nPath: {spriteMeta.path}");

            if (GUILayout.Button(btnContent, GUILayout.Width(64), GUILayout.Height(64)))
            {
                var selected = doc?.PrimarySelectedElement;
                if (selected != null)
                {
                    var img = selected.GetComponent<CuiImageComponent>();
                    var btn = selected.GetComponent<CuiButtonComponent>();
                    if (img != null)
                    {
                        img.Sprite = spriteMeta.path;
                    }
                    else if (btn != null)
                    {
                        btn.Material = spriteMeta.path;
                    }
                    else
                    {
                        img = selected.GetOrCreateComponent<CuiImageComponent>();
                        img.Sprite = spriteMeta.path;
                    }
                    onModified?.Invoke();
                }
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
            var tex = sprite != null ? sprite.texture : Texture2D.whiteTexture;
            var btnContent = new GUIContent(tex, $"{item.displayName}\n(ID: {item.itemId})\nShortname: {item.shortname}");

            if (GUILayout.Button(btnContent, GUILayout.Width(64), GUILayout.Height(64)))
            {
                var selected = doc?.PrimarySelectedElement;
                if (selected != null)
                {
                    var img = selected.GetOrCreateComponent<CuiImageComponent>();
                    img.ItemId = item.itemId;
                    onModified?.Invoke();
                }
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

        private void DrawMaterialsList(CuiDocument doc, Action onModified)
        {
            var mats = RustAssetDiscovery.VerifiedMaterials;
            var filtered = string.IsNullOrEmpty(_searchFilter)
                ? mats
                : mats.Where(m => m.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();

            GUILayout.Label($"Verified Rust Materials: {filtered.Length}", EditorStyles.miniLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            foreach (var matPath in filtered)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(matPath, EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("Apply", EditorStyles.miniButton, GUILayout.Width(50)))
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

        private void DrawFontsList(CuiDocument doc, Action onModified)
        {
            var fonts = RustAssetDiscovery.VerifiedFonts;
            var filtered = string.IsNullOrEmpty(_searchFilter)
                ? fonts
                : fonts.Where(f => f.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();

            GUILayout.Label($"Verified Rust Fonts: {filtered.Length}", EditorStyles.miniLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            foreach (var fontName in filtered)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(fontName, EditorStyles.boldLabel);
                if (GUILayout.Button("Apply", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    var selected = doc?.PrimarySelectedElement;
                    if (selected != null)
                    {
                        var txt = selected.GetOrCreateComponent<CuiTextComponent>();
                        txt.Font = fontName;
                        onModified?.Invoke();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
