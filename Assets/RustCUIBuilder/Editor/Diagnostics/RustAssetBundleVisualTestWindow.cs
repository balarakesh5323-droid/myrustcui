using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Discovery;

namespace RustCUIBuilder.Editor.Diagnostics
{
    /// <summary>
    /// Visual test window displaying actual loaded authentic Rust sprites
    /// with path, bundle, sprite dimensions, and live preview rendering.
    /// </summary>
    public class RustAssetBundleVisualTestWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private string _searchFilter = "";
        private List<RustBundleManager.RustAssetInfo> _filteredAssets = new List<RustBundleManager.RustAssetInfo>();

        [MenuItem("Rust/Developer/AssetBundle Visual Test Window")]
        public static void ShowWindow()
        {
            var win = GetWindow<RustAssetBundleVisualTestWindow>("Rust Sprite Visual Test");
            win.minSize = new Vector2(700, 500);
            win.Show();
        }

        private void OnEnable()
        {
            RustBundleManager.Initialize();
            RefreshList();
        }

        private void RefreshList()
        {
            _filteredAssets = RustBundleManager.IndexedAssets
                .Where(a => string.IsNullOrEmpty(_searchFilter) || a.AssetPath.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                .Take(200)
                .ToList();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎨 Authentic Rust Sprite Visual Test Window", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck()) RefreshList();

            if (GUILayout.Button("Reload Bundles", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                RustBundleManager.Reload();
                RefreshList();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Total Indexed Assets: {RustBundleManager.IndexedAssets.Count} | Showing: {_filteredAssets.Count}", EditorStyles.miniLabel);

            EditorGUILayout.Space(6);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            int columns = Mathf.Max(1, Mathf.FloorToInt(position.width / 140f));
            int rows = Mathf.CeilToInt((float)_filteredAssets.Count / columns);

            for (int r = 0; r < rows; r++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < columns; c++)
                {
                    int idx = r * columns + c;
                    if (idx < _filteredAssets.Count)
                    {
                        DrawAssetCard(_filteredAssets[idx]);
                    }
                    else
                    {
                        GUILayout.Space(136);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawAssetCard(RustBundleManager.RustAssetInfo info)
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(130), GUILayout.Height(150));

            var sprite = RustBundleManager.LoadSprite(info.AssetPath);
            var tex = sprite != null ? sprite.texture : Texture2D.whiteTexture;

            var previewRect = GUILayoutUtility.GetRect(110, 80, GUILayout.Width(110), GUILayout.Height(80));
            if (tex != null)
            {
                EditorGUI.DrawTextureTransparent(previewRect, tex, ScaleMode.ScaleToFit);
            }

            string filename = System.IO.Path.GetFileName(info.AssetPath);
            EditorGUILayout.LabelField(filename, EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField(info.BundleName, EditorStyles.miniLabel);
            if (sprite != null)
            {
                EditorGUILayout.LabelField($"{sprite.rect.width:0}x{sprite.rect.height:0} px", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }
    }
}
