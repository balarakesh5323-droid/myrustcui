using System;
using System.Collections.Generic;
using System.Linq;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Discovery;
using UnityEditor;
using UnityEngine;

namespace RustCUIBuilder.Editor.AssetBrowser
{
    /// <summary>
    /// Searchable, categorized asset browser displaying Rust UI Sprites, 2,800+ Item Icons, and Verified Materials.
    /// Supports one-click binding to selected Image and Button components.
    /// </summary>
    public class CuiAssetBrowserView
    {
        public enum AssetCategoryTab
        {
            UiSprites,
            GameItems,
            Materials
        }

        private AssetCategoryTab _currentTab = AssetCategoryTab.UiSprites;
        private Vector2 _scrollPos;
        private string _searchFilter = "";
        private int _currentPage = 0;
        private const int ItemsPerPage = 60;

        public void Draw(Rect rect, CuiDocument doc, Action onModified)
        {
            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginVertical("box");

            // Header Toolbar & Tabs
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Toggle(_currentTab == AssetCategoryTab.UiSprites, "UI Sprites & Icons", EditorStyles.toolbarButton))
            {
                if (_currentTab != AssetCategoryTab.UiSprites) { _currentTab = AssetCategoryTab.UiSprites; _currentPage = 0; }
            }
            if (GUILayout.Toggle(_currentTab == AssetCategoryTab.GameItems, $"Game Items ({RustAssetDiscovery.TotalItemCount})", EditorStyles.toolbarButton))
            {
                if (_currentTab != AssetCategoryTab.GameItems) { _currentTab = AssetCategoryTab.GameItems; _currentPage = 0; }
            }
            if (GUILayout.Toggle(_currentTab == AssetCategoryTab.Materials, "Materials", EditorStyles.toolbarButton))
            {
                if (_currentTab != AssetCategoryTab.Materials) { _currentTab = AssetCategoryTab.Materials; _currentPage = 0; }
            }

            GUILayout.FlexibleSpace();

            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(130));

            if (GUILayout.Button("Re-Scan", EditorStyles.toolbarButton, GUILayout.Width(55)))
            {
                RustAssetDiscovery.ReindexAssets();
            }
            EditorGUILayout.EndHorizontal();

            if (!RustAssetDiscovery.IsIndexed)
            {
                RustAssetDiscovery.ReindexAssets();
            }

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
            }

            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawUiSpritesGrid(Rect rect, CuiDocument doc, Action onModified)
        {
            var sprites = RustAssetDiscovery.AllUiSprites;
            var filtered = string.IsNullOrEmpty(_searchFilter)
                ? sprites.ToList()
                : sprites.Where(s => s.name.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     s.path.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Total Sprites: {filtered.Count}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            int columns = Mathf.Max(2, Mathf.FloorToInt((rect.width - 24) / 78f));
            int rows = Mathf.CeilToInt((float)filtered.Count / columns);

            for (int r = 0; r < rows; r++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < columns; c++)
                {
                    int idx = r * columns + c;
                    if (idx < filtered.Count)
                    {
                        var spriteMeta = filtered[idx];
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
                    Debug.Log($"[RustCUIBuilder] Applied Sprite: {spriteMeta.path} to element {selected.Name}");
                }
            }

            string labelText = spriteMeta.name.Length > 10 ? spriteMeta.name.Substring(0, 9) + ".." : spriteMeta.name;
            GUILayout.Label(labelText, EditorStyles.miniLabel, GUILayout.Width(72));

            EditorGUILayout.EndVertical();
        }

        private void DrawGameItemsGrid(Rect rect, CuiDocument doc, Action onModified)
        {
            var allItems = RustAssetDiscovery.AllItems;
            var filtered = string.IsNullOrEmpty(_searchFilter)
                ? allItems
                : allItems.Where(i => i.shortname.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      i.displayName.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)filtered.Count / ItemsPerPage));
            _currentPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);

            // Pagination bar
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = _currentPage > 0;
            if (GUILayout.Button("◄ Prev", EditorStyles.miniButton, GUILayout.Width(50))) _currentPage--;
            GUI.enabled = true;

            GUILayout.Label($"Page {_currentPage + 1} / {totalPages} ({filtered.Count} Items)", EditorStyles.centeredGreyMiniLabel);

            GUI.enabled = _currentPage < totalPages - 1;
            if (GUILayout.Button("Next ►", EditorStyles.miniButton, GUILayout.Width(50))) _currentPage++;
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Grid View
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            int startIdx = _currentPage * ItemsPerPage;
            int count = Math.Min(ItemsPerPage, filtered.Count - startIdx);

            int columns = Mathf.Max(2, Mathf.FloorToInt((rect.width - 24) / 78f));
            int rows = Mathf.CeilToInt((float)count / columns);

            for (int r = 0; r < rows; r++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < columns; c++)
                {
                    int itemIdx = startIdx + (r * columns + c);
                    if (itemIdx < filtered.Count)
                    {
                        DrawItemCell(filtered[itemIdx], doc, onModified);
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
            var btnContent = new GUIContent(sprite != null ? sprite.texture : Texture2D.whiteTexture, $"{item.displayName}\nID: {item.itemId}\nShortname: {item.shortname}");

            if (GUILayout.Button(btnContent, GUILayout.Width(64), GUILayout.Height(64)))
            {
                var selected = doc?.PrimarySelectedElement;
                if (selected != null)
                {
                    var img = selected.GetOrCreateComponent<CuiImageComponent>();
                    img.ItemId = item.itemId;
                    img.Sprite = item.shortname;
                    onModified?.Invoke();
                    Debug.Log($"[RustCUIBuilder] Applied Item: {item.displayName} (ID: {item.itemId}) to element {selected.Name}");
                }
            }

            string labelText = item.shortname.Length > 9 ? item.shortname.Substring(0, 8) + ".." : item.shortname;
            GUILayout.Label(labelText, EditorStyles.miniLabel, GUILayout.Width(72));

            EditorGUILayout.EndVertical();
        }

        private void DrawMaterialsList(CuiDocument doc, Action onModified)
        {
            var mats = RustAssetDiscovery.VerifiedMaterials;
            var filtered = string.IsNullOrEmpty(_searchFilter)
                ? mats
                : mats.Where(m => m.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            foreach (var mat in filtered)
            {
                EditorGUILayout.BeginHorizontal("box");
                GUILayout.Label(mat, EditorStyles.label);
                if (GUILayout.Button("Apply Material", GUILayout.Width(100)))
                {
                    var selected = doc?.PrimarySelectedElement;
                    if (selected != null)
                    {
                        var img = selected.GetComponent<CuiImageComponent>();
                        var raw = selected.GetComponent<CuiRawImageComponent>();
                        var btn = selected.GetComponent<CuiButtonComponent>();
                        if (img != null) img.Material = mat;
                        else if (raw != null) raw.Material = mat;
                        else if (btn != null) btn.Material = mat;
                        else
                        {
                            img = selected.GetOrCreateComponent<CuiImageComponent>();
                            img.Material = mat;
                        }
                        onModified?.Invoke();
                        Debug.Log($"[RustCUIBuilder] Applied Material: {mat} to element {selected.Name}");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
