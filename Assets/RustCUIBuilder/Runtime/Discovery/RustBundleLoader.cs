using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RustCUIBuilder.Runtime.Discovery
{
    /// <summary>
    /// Professional, non-blocking AssetBundle loader for Rust game client bundles.
    /// Safely loads manifests, textures, fonts, and materials from Rust/Bundles.
    /// </summary>
    public static class RustBundleLoader
    {
        private static readonly Dictionary<string, AssetBundle> LoadedBundles = new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Sprite> LoadedSprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Material> LoadedMaterials = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Font> LoadedFonts = new Dictionary<string, Font>(StringComparer.OrdinalIgnoreCase);

        public static bool IsLoaded => LoadedSprites.Count > 0 || LoadedMaterials.Count > 0 || LoadedFonts.Count > 0;
        public static IReadOnlyDictionary<string, Sprite> Sprites => LoadedSprites;
        public static IReadOnlyDictionary<string, Material> Materials => LoadedMaterials;
        public static IReadOnlyDictionary<string, Font> Fonts => LoadedFonts;

        public static bool LoadBundles(string rustPath = null)
        {
            if (string.IsNullOrEmpty(rustPath))
            {
                var install = SteamDiscovery.DiscoverRustInstallation();
                if (!install.IsValid)
                {
                    Debug.LogWarning("[RustBundleLoader] Rust installation not found.");
                    return false;
                }
                rustPath = install.RustRootPath;
            }

            string bundlesDir = Path.Combine(rustPath, "Bundles");
            string manifestBundlePath = Path.Combine(bundlesDir, "Bundles");

            if (!File.Exists(manifestBundlePath))
            {
                Debug.LogWarning($"[RustBundleLoader] Bundles manifest not found at: {manifestBundlePath}");
                return false;
            }

            try
            {
                Unload();

                AssetBundle rootBundle = AssetBundle.LoadFromFile(manifestBundlePath);
                List<string> bundlesToLoad = new List<string>();

                if (rootBundle != null)
                {
                    var manifests = rootBundle.LoadAllAssets<AssetBundleManifest>();
                    if (manifests != null && manifests.Length > 0)
                    {
                        var manifest = manifests[0];
                        foreach (string bName in manifest.GetAllAssetBundles())
                        {
                            if (bName.IndexOf("textures", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                bName.EndsWith("content.bundle", StringComparison.OrdinalIgnoreCase) ||
                                bName.IndexOf("shared", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                bundlesToLoad.Add(bName);
                            }
                        }
                    }
                    rootBundle.Unload(true);
                }

                // If manifest didn't find them, scan direct files
                if (bundlesToLoad.Count == 0)
                {
                    var allBundleFiles = Directory.GetFiles(bundlesDir, "*.bundle", SearchOption.AllDirectories);
                    foreach (var bf in allBundleFiles)
                    {
                        string rel = bf.Substring(bundlesDir.Length).TrimStart('/', '\\').Replace('\\', '/');
                        bundlesToLoad.Add(rel);
                    }
                }

                Debug.Log($"[RustBundleLoader] Found {bundlesToLoad.Count} candidate bundles to load.");

                foreach (string bName in bundlesToLoad)
                {
                    string fullPath = Path.Combine(bundlesDir, bName.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(fullPath)) continue;

                    try
                    {
                        var ab = AssetBundle.LoadFromFile(fullPath);
                        if (ab == null) continue;

                        LoadedBundles[bName] = ab;
                        ExtractAssetsFromBundle(ab);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[RustBundleLoader] Could not load bundle {bName}: {ex.Message}");
                    }
                }

                Debug.Log($"[RustBundleLoader] AssetBundle extraction complete! Sprites: {LoadedSprites.Count}, Materials: {LoadedMaterials.Count}, Fonts: {LoadedFonts.Count}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[RustBundleLoader] Fatal error during bundle loading: " + ex);
                return false;
            }
        }

        private static void ExtractAssetsFromBundle(AssetBundle ab)
        {
            string[] assetNames;
            try
            {
                assetNames = ab.GetAllAssetNames();
            }
            catch
            {
                return;
            }

            foreach (string name in assetNames)
            {
                string lower = name.ToLowerInvariant();

                // Fonts
                if (lower.EndsWith(".ttf") || lower.EndsWith(".otf"))
                {
                    try
                    {
                        var font = ab.LoadAsset<Font>(name);
                        if (font != null)
                        {
                            UnityEngine.Object.DontDestroyOnLoad(font);
                            LoadedFonts[name] = font;
                            string filename = Path.GetFileName(name);
                            LoadedFonts[filename] = font;
                        }
                    }
                    catch { }
                }
                // Materials
                else if (lower.EndsWith(".mat") && (lower.Contains("ui") || lower.Contains("icon") || lower.Contains("materials")))
                {
                    try
                    {
                        var mat = ab.LoadAsset<Material>(name);
                        if (mat != null)
                        {
                            UnityEngine.Object.DontDestroyOnLoad(mat);
                            LoadedMaterials[name] = mat;
                        }
                    }
                    catch { }
                }
                // Sprites and Textures
                else if (lower.Contains("ui") || lower.Contains("icon") || lower.Contains("texture") || lower.EndsWith(".png") || lower.EndsWith(".psd") || lower.EndsWith(".jpg"))
                {
                    try
                    {
                        var sprite = ab.LoadAsset<Sprite>(name);
                        if (sprite != null)
                        {
                            UnityEngine.Object.DontDestroyOnLoad(sprite);
                            LoadedSprites[name] = sprite;
                        }
                        else
                        {
                            var tex = ab.LoadAsset<Texture2D>(name);
                            if (tex != null)
                            {
                                UnityEngine.Object.DontDestroyOnLoad(tex);
                                var spr = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                                UnityEngine.Object.DontDestroyOnLoad(spr);
                                LoadedSprites[name] = spr;
                            }
                        }
                    }
                    catch { }
                }
            }
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
