using System;
using System.Linq;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Discovery;
using UnityEditor;
using UnityEngine;

namespace RustCUIBuilder.Editor.AssetBrowser
{
    /// <summary>
    /// Searchable, paginated asset browser displaying 2,800+ discovered Rust item icons and sprite assets.
    /// Supports one-click binding to Image components.
    /// </summary>
    public class CuiAssetBrowserView
    {
        private Vector2 _scrollPos;
        private string _searchFilter = "";
        private int _currentPage = 0;
        private const int ItemsPerPage = 60;

        public void Draw(Rect rect, CuiDocument doc, Action onModified)
        {
            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginVertical("box");

            // Header & Search
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Rust Item Browser", EditorStyles.boldLabel, GUILayout.Width(120));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);

            if (GUILayout.Button("Re-Scan", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RustAssetDiscovery.ReindexAssets();
            }
            EditorGUILayout.EndHorizontal();

            if (!RustAssetDiscovery.IsIndexed)
            {
                RustAssetDiscovery.ReindexAssets();
            }

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

            int columns = Mathf.Max(2, Mathf.FloorToInt((rect.width - 24) / 75f));
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
                        GUILayout.Space(75);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawItemCell(RustAssetDiscovery.ItemAssetMetadata item, CuiDocument doc, Action onModified)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(72), GUILayout.Height(84));

            var sprite = RustAssetDiscovery.LoadItemIcon(item);
            var btnContent = new GUIContent(sprite != null ? sprite.texture : Texture2D.whiteTexture, $"{item.displayName}\nID: {item.itemId}\nShortname: {item.shortname}");

            if (GUILayout.Button(btnContent, GUILayout.Width(64), GUILayout.Height(64)))
            {
                // Apply to selected element's Image component
                var selected = doc?.PrimarySelectedElement;
                if (selected != null)
                {
                    var img = selected.GetOrCreateComponent<CuiImageComponent>();
                    img.ItemId = item.itemId;
                    img.Sprite = item.shortname;
                    onModified?.Invoke();
                }
            }

            string labelText = item.shortname.Length > 9 ? item.shortname.Substring(0, 8) + ".." : item.shortname;
            GUILayout.Label(labelText, EditorStyles.miniLabel, GUILayout.Width(70));

            EditorGUILayout.EndVertical();
        }
    }
}
