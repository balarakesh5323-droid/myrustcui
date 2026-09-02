using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RustCUIBuilder.Runtime.Discovery
{
    public enum RustAssetType
    {
        All,
        UiSprites,
        GameItems,
        Materials
    }

    /// <summary>
    /// Discovers, indexes, and caches Rust assets including 1,723 item icons from Bundles/items,
    /// authentic sprites via RustBundleManager, materials, fonts, and procedural icon fallbacks.
    /// </summary>
    public static class RustAssetDiscovery
    {
        [Serializable]
        public class ItemAssetMetadata
        {
            public int itemId;
            public string shortname;
            public string displayName;
            public string pngFilePath;
            public string jsonFilePath;
            public bool isIdLoaded;
        }

        [Serializable]
        public class UiSpriteMetadata
        {
            public string path;
            public string name;
            public string category;
            public Sprite sprite;
            public bool isBundleLoaded;
        }

        public static readonly string[] VerifiedFonts = new[]
        {
            "RobotoCondensed-Bold.ttf",
            "RobotoCondensed-Regular.ttf",
            "DroidSansMono.ttf",
            "PermanentMarker.ttf",
            "dsk.ttf"
        };

        public static readonly string[] VerifiedMaterials = new[]
        {
            "assets/content/ui/uibackgroundblur.mat",
            "assets/content/ui/uibackgroundblur-ingamemenu.mat",
            "assets/content/ui/uibackgroundblur-mainmenu.mat",
            "assets/content/ui/uibackgroundblur-notice.mat",
            "assets/icons/iconmaterial.mat",
            "assets/content/ui/ui.maskclear.mat",
            "assets/content/ui/ui.saturation.shader",
            "assets/content/ui/ui.thresholdcolor.shader",
            "assets/icons/fogofwar.mat",
            "assets/icons/greyout.mat"
        };

        public static readonly string[] VerifiedLayers = new[]
        {
            "Overall",
            "Overlay",
            "OverlayNonScaled",
            "Hud.Menu",
            "Hud",
            "Under",
            "UnderNonScaled",
            "Inventory",
            "Crafting",
            "Contacts",
            "Clans",
            "TechTree",
            "Map"
        };

        public static readonly string[] VerifiedSprites = new[]
        {
            "assets/content/ui/ui.background.tile.psd",
            "assets/content/ui/ui.background.transparent.psd",
            "assets/content/ui/ui.box.shadow.psd",
            "assets/content/ui/ui.circle.psd",
            "assets/content/ui/ui.circle.gradient.psd",
            "assets/content/ui/ui.rounded.psd",
            "assets/content/ui/ui.white.psd",
            "assets/content/materials/highlight.png",
            "assets/icons/check.png",
            "assets/icons/close.png",
            "assets/icons/cross.png",
            "assets/icons/circle_closed.png",
            "assets/icons/device_add.png",
            "assets/icons/fun.png",
            "assets/icons/facepunch.png",
            "assets/icons/explosion_sprite.png",
            "assets/icons/radiation.png",
            "assets/icons/bleeding.png",
            "assets/icons/cold.png",
            "assets/icons/wet.png",
            "assets/icons/poison.png",
            "assets/icons/starve.png",
            "assets/icons/thirst.png",
            "assets/icons/wound.png",
            "assets/icons/skull.png",
            "assets/icons/lock.png",
            "assets/icons/unlock.png",
            "assets/icons/chat.png",
            "assets/icons/store.png",
            "assets/icons/gear.png",
            "assets/icons/shield.png",
            "assets/icons/swords.png",
            "assets/icons/hammer.png",
            "assets/icons/wrench.png",
            "assets/icons/coin.png",
            "assets/icons/clock.png",
            "assets/icons/map.png",
            "assets/icons/compass.png",
            "assets/icons/server.png",
            "assets/icons/warning.png",
            "assets/icons/info.png",
            "assets/icons/refresh.png",
            "assets/icons/plus.png",
            "assets/icons/minus.png",
            "assets/icons/trash.png",
            "assets/icons/arrow_up.png",
            "assets/icons/arrow_down.png",
            "assets/icons/arrow_left.png",
            "assets/icons/arrow_right.png",
            "assets/icons/star.png",
            "assets/icons/heart.png"
        };

        private static readonly Dictionary<string, ItemAssetMetadata> ItemCacheByName = new Dictionary<string, ItemAssetMetadata>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<int, ItemAssetMetadata> ItemCacheById = new Dictionary<int, ItemAssetMetadata>();
        private static readonly List<ItemAssetMetadata> AllItemsList = new List<ItemAssetMetadata>();
        private static readonly List<UiSpriteMetadata> AllUiSpritesList = new List<UiSpriteMetadata>();
        private static readonly Dictionary<string, Sprite> LoadedSpriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        public static bool IsIndexed { get; private set; }
        public static int TotalItemCount => AllItemsList.Count;
        public static int TotalSpriteCount => AllUiSpritesList.Count;
        public static IReadOnlyList<ItemAssetMetadata> AllItems => AllItemsList;
        public static IReadOnlyList<UiSpriteMetadata> AllUiSprites => AllUiSpritesList;

        public static void ReindexAssets()
        {
            if (IsIndexed && AllItemsList.Count > 0 && AllUiSpritesList.Count > 0) return;

            ItemCacheByName.Clear();
            ItemCacheById.Clear();
            AllItemsList.Clear();
            AllUiSpritesList.Clear();
            LoadedSpriteCache.Clear();

            // 1. Initialize Rust Bundle Manager
            RustBundleManager.Initialize();

            // 2. Initialize UI Sprites (Authentic bundle load first, procedural fallback second)
            foreach (var spritePath in VerifiedSprites)
            {
                string filename = Path.GetFileNameWithoutExtension(spritePath);
                string category = spritePath.StartsWith("assets/content/ui", StringComparison.OrdinalIgnoreCase) ? "UI Elements" : "Icons";

                var authenticSprite = RustBundleManager.LoadSprite(spritePath);
                if (authenticSprite != null)
                {
                    AllUiSpritesList.Add(new UiSpriteMetadata
                    {
                        path = spritePath,
                        name = filename,
                        category = category,
                        sprite = authenticSprite,
                        isBundleLoaded = true
                    });
                    LoadedSpriteCache[spritePath] = authenticSprite;
                }
                else
                {
                    var spriteObj = GenerateOrLoadSprite(spritePath, filename);
                    AllUiSpritesList.Add(new UiSpriteMetadata
                    {
                        path = spritePath,
                        name = filename,
                        category = category,
                        sprite = spriteObj,
                        isBundleLoaded = false
                    });
                    if (spriteObj != null)
                    {
                        LoadedSpriteCache[spritePath] = spriteObj;
                    }
                }
            }

            // 3. Discover Item Icons from Steam Rust installation
            var install = SteamDiscovery.DiscoverRustInstallation();
            if (install.IsValid && !string.IsNullOrEmpty(install.ItemsBundlePath) && Directory.Exists(install.ItemsBundlePath))
            {
                try
                {
                    var files = Directory.GetFiles(install.ItemsBundlePath, "*.png");
                    foreach (var pngPath in files)
                    {
                        string shortname = Path.GetFileNameWithoutExtension(pngPath);
                        var item = new ItemAssetMetadata
                        {
                            shortname = shortname,
                            displayName = FormatDisplayName(shortname),
                            pngFilePath = pngPath,
                            jsonFilePath = Path.ChangeExtension(pngPath, ".json"),
                            isIdLoaded = false
                        };

                        ItemCacheByName[shortname] = item;
                        AllItemsList.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[RustAssetDiscovery] Error scanning items bundle: " + ex.Message);
                }
            }

            IsIndexed = true;
            Debug.Log($"[RustAssetDiscovery] Discovery ready: {AllItemsList.Count} items indexed, {AllUiSpritesList.Count} UI sprites ready.");
        }

        public static ItemAssetMetadata FindItemByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            ItemCacheByName.TryGetValue(name, out var item);
            if (item != null && !item.isIdLoaded) LoadItemIdLazy(item);
            return item;
        }

        public static ItemAssetMetadata FindItemById(int id)
        {
            if (id == 0) return null;
            if (ItemCacheById.TryGetValue(id, out var cached)) return cached;

            foreach (var item in AllItemsList)
            {
                if (!item.isIdLoaded) LoadItemIdLazy(item);
                if (item.itemId == id) return item;
            }
            return null;
        }

        private static void LoadItemIdLazy(ItemAssetMetadata item)
        {
            if (item == null || item.isIdLoaded) return;
            item.isIdLoaded = true;

            if (!string.IsNullOrEmpty(item.jsonFilePath) && File.Exists(item.jsonFilePath))
            {
                try
                {
                    string json = File.ReadAllText(item.jsonFilePath);
                    int id = ExtractItemIdFromJson(json);
                    if (id != 0)
                    {
                        item.itemId = id;
                        ItemCacheById[id] = item;
                    }
                }
                catch { }
            }
        }

        public static Sprite GetSpriteByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            if (LoadedSpriteCache.TryGetValue(path, out var cached) && cached != null)
                return cached;

            // 1. Check Authentic Rust Bundle Manager
            var authentic = RustBundleManager.LoadSprite(path);
            if (authentic != null)
            {
                LoadedSpriteCache[path] = authentic;
                return authentic;
            }

            // 2. Check if it's an item icon shortname
            var item = FindItemByName(path);
            if (item != null)
            {
                var itemSprite = LoadItemIcon(item);
                if (itemSprite != null)
                {
                    LoadedSpriteCache[path] = itemSprite;
                    return itemSprite;
                }
            }

            // 3. Fallback procedural sprite
            var sprite = GenerateOrLoadSprite(path, Path.GetFileNameWithoutExtension(path));
            if (sprite != null)
            {
                LoadedSpriteCache[path] = sprite;
            }
            return sprite;
        }

        public static Sprite LoadItemIcon(ItemAssetMetadata item)
        {
            if (item == null || string.IsNullOrEmpty(item.pngFilePath) || !File.Exists(item.pngFilePath))
                return null;

            if (LoadedSpriteCache.TryGetValue(item.pngFilePath, out var cached) && cached != null)
                return cached;

            try
            {
                byte[] bytes = File.ReadAllBytes(item.pngFilePath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    LoadedSpriteCache[item.pngFilePath] = sprite;
                    return sprite;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RustAssetDiscovery] Failed to load icon {item.shortname}: {ex.Message}");
            }

            return null;
        }

        private static Sprite GenerateOrLoadSprite(string path, string name)
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            FillClear(colors, size);

            string lower = name.ToLowerInvariant();

            // 1. UI Backgrounds (Must be pure white with borders so Color tint works properly)
            if (lower.Contains("tile") || lower.Contains("background"))
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool isBorder = x <= 1 || x >= size - 2 || y <= 1 || y >= size - 2;
                        colors[y * size + x] = isBorder ? new Color(1f, 1f, 1f, 0.95f) : new Color(1f, 1f, 1f, 0.85f);
                    }
                }
            }
            else if (lower.Contains("box.shadow"))
            {
                float radius = size / 2f;
                Vector2 center = new Vector2(radius, radius);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        float a = Mathf.Clamp01(1f - (dist / radius));
                        colors[y * size + x] = new Color(1f, 1f, 1f, a * a);
                    }
                }
            }
            else if (lower.Contains("circle"))
            {
                float radius = size / 2f - 2f;
                Vector2 center = new Vector2(size / 2f, size / 2f);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), center);
                        if (dist <= radius)
                        {
                            float alpha = lower.Contains("gradient") ? Mathf.Clamp01(1f - (dist / radius)) : 1f;
                            colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                        }
                    }
                }
            }
            else if (lower.Contains("rounded"))
            {
                float cornerRadius = 10f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool inside = true;
                        if (x < cornerRadius && y < cornerRadius && Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, cornerRadius)) > cornerRadius) inside = false;
                        if (x > size - cornerRadius && y < cornerRadius && Vector2.Distance(new Vector2(x, y), new Vector2(size - cornerRadius, cornerRadius)) > cornerRadius) inside = false;
                        if (x < cornerRadius && y > size - cornerRadius && Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, size - cornerRadius)) > cornerRadius) inside = false;
                        if (x > size - cornerRadius && y > size - cornerRadius && Vector2.Distance(new Vector2(x, y), new Vector2(size - cornerRadius, size - cornerRadius)) > cornerRadius) inside = false;

                        if (inside) colors[y * size + x] = Color.white;
                    }
                }
            }
            else if (lower.Contains("highlight"))
            {
                for (int y = 0; y < size; y++)
                {
                    float a = Mathf.Lerp(0.8f, 0.1f, (float)y / size);
                    for (int x = 0; x < size; x++) colors[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            // 2. Verified Icons
            else if (lower.Contains("check"))
            {
                DrawThickLine(colors, size, new Vector2(14, 32), new Vector2(26, 18), 5, new Color(0.25f, 0.9f, 0.35f, 1f));
                DrawThickLine(colors, size, new Vector2(26, 18), new Vector2(52, 48), 5, new Color(0.25f, 0.9f, 0.35f, 1f));
            }
            else if (lower.Contains("close") || lower.Contains("cross"))
            {
                DrawThickLine(colors, size, new Vector2(16, 16), new Vector2(48, 48), 5, new Color(0.95f, 0.25f, 0.25f, 1f));
                DrawThickLine(colors, size, new Vector2(16, 48), new Vector2(48, 16), 5, new Color(0.95f, 0.25f, 0.25f, 1f));
            }
            else if (lower.Contains("heart"))
            {
                DrawFilledCircle(colors, size, new Vector2(24, 38), 12, new Color(0.95f, 0.2f, 0.3f, 1f));
                DrawFilledCircle(colors, size, new Vector2(40, 38), 12, new Color(0.95f, 0.2f, 0.3f, 1f));
                DrawThickLine(colors, size, new Vector2(14, 34), new Vector2(32, 12), 12, new Color(0.95f, 0.2f, 0.3f, 1f));
                DrawThickLine(colors, size, new Vector2(50, 34), new Vector2(32, 12), 12, new Color(0.95f, 0.2f, 0.3f, 1f));
            }
            else if (lower.Contains("star"))
            {
                DrawFilledCircle(colors, size, new Vector2(32, 32), 14, new Color(1f, 0.85f, 0.2f, 1f));
                DrawThickLine(colors, size, new Vector2(32, 54), new Vector2(32, 10), 6, new Color(1f, 0.85f, 0.2f, 1f));
                DrawThickLine(colors, size, new Vector2(10, 38), new Vector2(54, 38), 6, new Color(1f, 0.85f, 0.2f, 1f));
            }
            else if (lower.Contains("skull") || lower.Contains("poison"))
            {
                DrawFilledCircle(colors, size, new Vector2(32, 36), 16, Color.white);
                DrawFilledRect(colors, size, new Rect(24, 16, 16, 12), Color.white);
                DrawFilledCircle(colors, size, new Vector2(26, 36), 4, Color.black);
                DrawFilledCircle(colors, size, new Vector2(38, 36), 4, Color.black);
            }
            else if (lower.Contains("lock"))
            {
                bool isOpen = lower.Contains("unlock");
                DrawFilledRect(colors, size, new Rect(18, 14, 28, 22), new Color(0.95f, 0.75f, 0.25f, 1f));
                Vector2 shackleTop = isOpen ? new Vector2(32, 48) : new Vector2(32, 42);
                DrawThickLine(colors, size, new Vector2(24, 36), shackleTop, 4, Color.white);
                DrawThickLine(colors, size, shackleTop, new Vector2(isOpen ? 46 : 40, isOpen ? 42 : 36), 4, Color.white);
            }
            else if (lower.Contains("radiation"))
            {
                DrawFilledCircle(colors, size, new Vector2(32, 32), 6, new Color(1f, 0.85f, 0.1f, 1f));
                DrawThickLine(colors, size, new Vector2(32, 32), new Vector2(32, 52), 6, new Color(1f, 0.85f, 0.1f, 1f));
                DrawThickLine(colors, size, new Vector2(32, 32), new Vector2(14, 20), 6, new Color(1f, 0.85f, 0.1f, 1f));
                DrawThickLine(colors, size, new Vector2(32, 32), new Vector2(50, 20), 6, new Color(1f, 0.85f, 0.1f, 1f));
            }
            else if (lower.Contains("bleeding") || lower.Contains("wet") || lower.Contains("thirst"))
            {
                Color dropColor = lower.Contains("bleeding") ? new Color(0.9f, 0.15f, 0.15f, 1f) : new Color(0.2f, 0.65f, 1f, 1f);
                DrawFilledCircle(colors, size, new Vector2(32, 24), 14, dropColor);
                DrawThickLine(colors, size, new Vector2(32, 50), new Vector2(32, 24), 10, dropColor);
            }
            else if (lower.Contains("shield"))
            {
                DrawFilledCircle(colors, size, new Vector2(32, 36), 18, new Color(0.3f, 0.6f, 0.9f, 1f));
                DrawThickLine(colors, size, new Vector2(18, 36), new Vector2(32, 12), 8, new Color(0.3f, 0.6f, 0.9f, 1f));
                DrawThickLine(colors, size, new Vector2(46, 36), new Vector2(32, 12), 8, new Color(0.3f, 0.6f, 0.9f, 1f));
            }
            else if (lower.Contains("gear"))
            {
                DrawFilledCircle(colors, size, new Vector2(32, 32), 16, new Color(0.75f, 0.78f, 0.82f, 1f));
                DrawFilledCircle(colors, size, new Vector2(32, 32), 6, Color.clear);
                DrawThickLine(colors, size, new Vector2(32, 10), new Vector2(32, 54), 6, new Color(0.75f, 0.78f, 0.82f, 1f));
                DrawThickLine(colors, size, new Vector2(10, 32), new Vector2(54, 32), 6, new Color(0.75f, 0.78f, 0.82f, 1f));
            }
            else if (lower.Contains("chat"))
            {
                DrawFilledCircle(colors, size, new Vector2(32, 36), 16, Color.white);
                DrawThickLine(colors, size, new Vector2(24, 24), new Vector2(16, 14), 6, Color.white);
            }
            else if (lower.Contains("coin"))
            {
                DrawFilledCircle(colors, size, new Vector2(32, 32), 18, new Color(1f, 0.8f, 0.2f, 1f));
                DrawFilledCircle(colors, size, new Vector2(32, 32), 14, new Color(0.95f, 0.65f, 0.1f, 1f));
            }
            else if (lower.Contains("clock"))
            {
                DrawFilledCircle(colors, size, new Vector2(32, 32), 18, Color.white);
                DrawFilledCircle(colors, size, new Vector2(32, 32), 14, Color.black);
                DrawThickLine(colors, size, new Vector2(32, 32), new Vector2(32, 42), 3, Color.white);
                DrawThickLine(colors, size, new Vector2(32, 32), new Vector2(40, 32), 3, Color.white);
            }
            else if (lower.Contains("fun"))
            {
                DrawFilledCircle(colors, size, new Vector2(32, 32), 20, new Color(1f, 0.85f, 0.15f, 1f));
                DrawFilledCircle(colors, size, new Vector2(24, 38), 3, Color.black);
                DrawFilledCircle(colors, size, new Vector2(40, 38), 3, Color.black);
                DrawThickLine(colors, size, new Vector2(22, 24), new Vector2(32, 18), 3, Color.black);
                DrawThickLine(colors, size, new Vector2(32, 18), new Vector2(42, 24), 3, Color.black);
            }
            else if (lower.Contains("facepunch"))
            {
                DrawFilledCircle(colors, size, new Vector2(32, 32), 20, new Color(0.9f, 0.15f, 0.15f, 1f));
                DrawThickLine(colors, size, new Vector2(20, 24), new Vector2(44, 40), 6, Color.white);
            }
            else if (lower.Contains("explosion"))
            {
                DrawFilledCircle(colors, size, new Vector2(32, 32), 16, new Color(1f, 0.45f, 0.1f, 1f));
                DrawThickLine(colors, size, new Vector2(12, 12), new Vector2(52, 52), 5, new Color(1f, 0.8f, 0.1f, 1f));
                DrawThickLine(colors, size, new Vector2(12, 52), new Vector2(52, 12), 5, new Color(1f, 0.8f, 0.1f, 1f));
            }
            else if (lower.Contains("plus"))
            {
                DrawThickLine(colors, size, new Vector2(14, 32), new Vector2(50, 32), 6, Color.white);
                DrawThickLine(colors, size, new Vector2(32, 14), new Vector2(32, 50), 6, Color.white);
            }
            else if (lower.Contains("minus"))
            {
                DrawThickLine(colors, size, new Vector2(14, 32), new Vector2(50, 32), 6, Color.white);
            }
            else if (lower.Contains("arrow_up"))
            {
                DrawThickLine(colors, size, new Vector2(32, 14), new Vector2(32, 50), 6, Color.white);
                DrawThickLine(colors, size, new Vector2(18, 36), new Vector2(32, 50), 6, Color.white);
                DrawThickLine(colors, size, new Vector2(46, 36), new Vector2(32, 50), 6, Color.white);
            }
            else if (lower.Contains("arrow_down"))
            {
                DrawThickLine(colors, size, new Vector2(32, 50), new Vector2(32, 14), 6, Color.white);
                DrawThickLine(colors, size, new Vector2(18, 28), new Vector2(32, 14), 6, Color.white);
                DrawThickLine(colors, size, new Vector2(46, 28), new Vector2(32, 14), 6, Color.white);
            }
            else
            {
                // Clean default icon token
                DrawFilledCircle(colors, size, new Vector2(32, 32), 18, new Color(0.85f, 0.88f, 0.92f, 1f));
            }

            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static void FillClear(Color[] colors, int size)
        {
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;
        }

        private static void DrawFilledCircle(Color[] colors, int size, Vector2 center, float radius, Color col)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), center) <= radius)
                    {
                        colors[y * size + x] = col;
                    }
                }
            }
        }

        private static void DrawFilledRect(Color[] colors, int size, Rect rect, Color col)
        {
            for (int y = (int)rect.yMin; y <= (int)rect.yMax && y < size; y++)
            {
                if (y < 0) continue;
                for (int x = (int)rect.xMin; x <= (int)rect.xMax && x < size; x++)
                {
                    if (x < 0) continue;
                    colors[y * size + x] = col;
                }
            }
        }

        private static void DrawThickLine(Color[] colors, int size, Vector2 p1, Vector2 p2, float thickness, Color col)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(p1, p2) * 2f);
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 pt = Vector2.Lerp(p1, p2, t);
                int px = Mathf.RoundToInt(pt.x);
                int py = Mathf.RoundToInt(pt.y);

                int rad = Mathf.CeilToInt(thickness / 2f);
                for (int y = py - rad; y <= py + rad; y++)
                {
                    for (int x = px - rad; x <= px + rad; x++)
                    {
                        if (x >= 0 && x < size && y >= 0 && y < size)
                        {
                            colors[y * size + x] = col;
                        }
                    }
                }
            }
        }

        private static string FormatDisplayName(string shortname)
        {
            var parts = shortname.Replace('_', ' ').Replace('.', ' ').Split(' ');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
                }
            }
            return string.Join(" ", parts);
        }

        private static int ExtractItemIdFromJson(string json)
        {
            int idx = json.IndexOf("\"itemid\":", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                int start = idx + 9;
                while (start < json.Length && (char.IsWhiteSpace(json[start]) || json[start] == ':')) start++;
                int end = start;
                while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
                if (int.TryParse(json.Substring(start, end - start), out int val))
                    return val;
            }
            return 0;
        }
    }
}
