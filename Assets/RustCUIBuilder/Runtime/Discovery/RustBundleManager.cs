using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RustCUIBuilder.Runtime.Discovery
{
    /// <summary>
    /// Master manager for authentic Rust Steam AssetBundles.
    /// Handles discovery, manifest loading, dependency resolution, typed asset deserialization,
    /// and high-performance caching for Sprites, Materials, and Fonts.
    /// </summary>
    public static class RustBundleManager
    {
        public class RustAssetInfo
        {
            public string AssetPath;
            public string BundleName;
            public string NormalizedPath;
            public string TypeName;
        }

        private static AssetBundle _rootBundle;
        private static AssetBundleManifest _manifest;

        private static readonly Dictionary<string, AssetBundle> LoadedBundles = new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> AssetToBundleMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<RustAssetInfo> AllIndexedAssets = new List<RustAssetInfo>();

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Font> FontCache = new Dictionary<string, Font>(StringComparer.OrdinalIgnoreCase);

        public static bool IsInitialized { get; private set; }
        public static IReadOnlyList<RustAssetInfo> IndexedAssets => AllIndexedAssets;

        public static bool Initialize(string customRustPath = null)
        {
            if (IsInitialized && _manifest != null) return true;

            string rustRoot = customRustPath;
            if (string.IsNullOrEmpty(rustRoot))
            {
                var install = SteamDiscovery.DiscoverRustInstallation();
                if (!install.IsValid) return false;
                rustRoot = install.RustRootPath;
            }

            string bundlesBase = Path.Combine(rustRoot, "Bundles");
            string rootBundlePath = Path.Combine(bundlesBase, "Bundles");

            if (!File.Exists(rootBundlePath))
            {
                Debug.LogWarning($"[RustBundleManager] Root bundle not found at: {rootBundlePath}");
                return false;
            }

            try
            {
                // Unload all stale editor asset bundle handles
                AssetBundle.UnloadAllAssetBundles(false);
                LoadedBundles.Clear();

                _rootBundle = AssetBundle.LoadFromFile(rootBundlePath);

                if (_rootBundle != null)
                {
                    _manifest = _rootBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                    if (_manifest == null)
                    {
                        var manifests = _rootBundle.LoadAllAssets<AssetBundleManifest>();
                        if (manifests != null && manifests.Length > 0) _manifest = manifests[0];
                    }
                }

                if (_manifest == null)
                {
                    Debug.LogWarning("[RustBundleManager] Failed to load AssetBundleManifest from root bundle.");
                    return false;
                }

                RebuildIndex(bundlesBase);
                IsInitialized = true;
                Debug.Log($"[RustBundleManager] Initialized successfully: {LoadedBundles.Count} bundles loaded, {AllIndexedAssets.Count} assets indexed.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RustBundleManager] Initialization error: {ex.Message}");
                return false;
            }
        }

        public static void RebuildIndex(string bundlesBase)
        {
            if (_manifest == null) return;

            string[] allBundles = _manifest.GetAllAssetBundles();

            // Load UI and content bundles
            var targetBundles = allBundles.Where(text =>
                text.Contains("textures") ||
                text.EndsWith("content.bundle", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("shared") ||
                text.Contains("items")
            ).ToList();

            foreach (var bName in targetBundles)
            {
                LoadBundleWithDependencies(bName, bundlesBase);
            }

            AllIndexedAssets.Clear();
            AssetToBundleMap.Clear();

            foreach (var pair in LoadedBundles)
            {
                try
                {
                    string[] assetNames = pair.Value.GetAllAssetNames();
                    foreach (var aPath in assetNames)
                    {
                        AssetToBundleMap[aPath] = pair.Key;

                        string typeName = "Other";
                        if (aPath.EndsWith(".png") || aPath.EndsWith(".psd") || aPath.EndsWith(".jpg") || aPath.EndsWith(".tga")) typeName = "Sprite";
                        else if (aPath.EndsWith(".mat") || aPath.EndsWith(".shader")) typeName = "Material";
                        else if (aPath.EndsWith(".ttf") || aPath.EndsWith(".otf")) typeName = "Font";

                        AllIndexedAssets.Add(new RustAssetInfo
                        {
                            AssetPath = aPath,
                            BundleName = pair.Key,
                            NormalizedPath = aPath.ToLowerInvariant(),
                            TypeName = typeName
                        });
                    }
                }
                catch { }
            }
        }

        public static AssetBundle LoadBundleWithDependencies(string bundleName, string bundlesBase)
        {
            if (LoadedBundles.TryGetValue(bundleName, out var existing) && existing != null)
                return existing;

            if (_manifest != null)
            {
                string[] dependencies = _manifest.GetAllDependencies(bundleName);
                foreach (var dep in dependencies)
                {
                    if (!LoadedBundles.ContainsKey(dep))
                    {
                        string depPath = Path.Combine(bundlesBase, dep.Replace('/', Path.DirectorySeparatorChar));
                        if (File.Exists(depPath))
                        {
                            try
                            {
                                var depBundle = AssetBundle.LoadFromFile(depPath);
                                if (depBundle != null) LoadedBundles[dep] = depBundle;
                            }
                            catch { }
                        }
                    }
                }
            }

            string fullPath = Path.Combine(bundlesBase, bundleName.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
            {
                try
                {
                    var ab = AssetBundle.LoadFromFile(fullPath);
                    if (ab != null)
                    {
                        LoadedBundles[bundleName] = ab;
                        return ab;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RustBundleManager] Failed to load {bundleName}: {ex.Message}");
                }
            }

            return null;
        }

        public static Sprite LoadSprite(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;

            if (SpriteCache.TryGetValue(assetPath, out var cached) && cached != null)
                return cached;

            if (!IsInitialized) Initialize();

            if (AssetToBundleMap.TryGetValue(assetPath, out var bName) && LoadedBundles.TryGetValue(bName, out var bundle))
            {
                try
                {
                    var sprite = bundle.LoadAsset<Sprite>(assetPath);
                    if (sprite != null)
                    {
                        SpriteCache[assetPath] = sprite;
                        return sprite;
                    }

                    // Fallback: Texture2D to Sprite
                    var tex = bundle.LoadAsset<Texture2D>(assetPath);
                    if (tex != null)
                    {
                        var created = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        SpriteCache[assetPath] = created;
                        return created;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RustBundleManager] Error loading sprite {assetPath}: {ex.Message}");
                }
            }

            return null;
        }

        public static Material LoadMaterial(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;

            if (MaterialCache.TryGetValue(assetPath, out var cached) && cached != null)
                return cached;

            if (!IsInitialized) Initialize();

            if (AssetToBundleMap.TryGetValue(assetPath, out var bName) && LoadedBundles.TryGetValue(bName, out var bundle))
            {
                try
                {
                    var mat = bundle.LoadAsset<Material>(assetPath);
                    if (mat != null)
                    {
                        MaterialCache[assetPath] = mat;
                        return mat;
                    }
                }
                catch { }
            }

            return null;
        }

        public static Font LoadFont(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;

            if (FontCache.TryGetValue(assetPath, out var cached) && cached != null)
                return cached;

            if (!IsInitialized) Initialize();

            if (AssetToBundleMap.TryGetValue(assetPath, out var bName) && LoadedBundles.TryGetValue(bName, out var bundle))
            {
                try
                {
                    var font = bundle.LoadAsset<Font>(assetPath);
                    if (font != null)
                    {
                        FontCache[assetPath] = font;
                        return font;
                    }
                }
                catch { }
            }

            return null;
        }

        public static void ClearCache()
        {
            SpriteCache.Clear();
            MaterialCache.Clear();
            FontCache.Clear();
        }

        public static void Reload()
        {
            ClearCache();
            AssetBundle.UnloadAllAssetBundles(false);
            LoadedBundles.Clear();
            _rootBundle = null;
            _manifest = null;
            IsInitialized = false;
            Initialize();
        }
    }
}
