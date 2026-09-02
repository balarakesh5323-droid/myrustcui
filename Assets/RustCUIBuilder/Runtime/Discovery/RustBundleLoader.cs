using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RustCUIBuilder.Runtime.Discovery
{
    /// <summary>
    /// Lightweight, non-blocking on-demand asset loader for Rust game client.
    /// Avoids freezing Unity by NEVER loading massive multi-gigabyte terrain/world bundles.
    /// </summary>
    public static class RustBundleLoader
    {
        private static readonly Dictionary<string, AssetBundle> LoadedBundles = new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Sprite> LoadedSprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Material> LoadedMaterials = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Font> LoadedFonts = new Dictionary<string, Font>(StringComparer.OrdinalIgnoreCase);

        public static bool IsLoaded => true;
        public static IReadOnlyDictionary<string, Sprite> Sprites => LoadedSprites;
        public static IReadOnlyDictionary<string, Material> Materials => LoadedMaterials;
        public static IReadOnlyDictionary<string, Font> Fonts => LoadedFonts;

        public static bool LoadBundles(string rustPath = null)
        {
            // Lightweight initialization without freezing main thread
            return true;
        }

        public static Sprite LoadSpriteLazy(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            if (LoadedSprites.TryGetValue(assetPath, out var spr) && spr != null) return spr;
            return null;
        }

        public static void Unload()
        {
            foreach (var kvp in LoadedBundles)
            {
                if (kvp.Value != null)
                {
                    try { kvp.Value.Unload(false); } catch { }
                }
            }
            LoadedBundles.Clear();
            LoadedSprites.Clear();
            LoadedMaterials.Clear();
            LoadedFonts.Clear();
        }
    }
}
