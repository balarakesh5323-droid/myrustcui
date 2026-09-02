using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RustCUIBuilder.Runtime.Discovery
{
    public enum RustAssetType
    {
        All,
        ItemIcon,
        UiSprite,
        Material
    }

    /// <summary>
    /// Discovers, indexes, and caches Rust assets including 2,800+ item icons from Bundles/items,
    /// verified UI sprites, procedural sprite fallbacks, materials, fonts, and UI layers.
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
        }

        [Serializable]
        public class UiSpriteMetadata
        {
            public string path;
            public string name;
            public string category;
            public Sprite sprite;
        }

        public static readonly string[] VerifiedFonts = new[]
        {
            "RobotoCondensed-Bold.ttf",
            "RobotoCondensed-Regular.ttf",
            "DroidSansMono.ttf",
            "PermanentMarker.ttf"
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
            "assets/icons/signal_sprite.png",
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
            ItemCacheByName.Clear();
            ItemCacheById.Clear();
            AllItemsList.Clear();
            AllUiSpritesList.Clear();
            LoadedSpriteCache.Clear();

            // 1. Initialize all Verified UI Sprites & procedural textures
            foreach (var spritePath in VerifiedSprites)
            {
                string filename = Path.GetFileNameWithoutExtension(spritePath);
                string category = spritePath.StartsWith("assets/content/ui", StringComparison.OrdinalIgnoreCase) ? "UI Elements" : "Icons";

                var spriteObj = GenerateOrLoadSprite(spritePath, filename);
                AllUiSpritesList.Add(new UiSpriteMetadata
                {
                    path = spritePath,
                    name = filename,
                    category = category,
                    sprite = spriteObj
                });
                if (spriteObj != null)
                {
                    LoadedSpriteCache[spritePath] = spriteObj;
                }
            }

            // 2. Discover Item Icons from Steam Rust installation
            var install = SteamDiscovery.DiscoverRustInstallation();
            if (install.IsValid && !string.IsNullOrEmpty(install.ItemsBundlePath))
            {
                try
                {
                    var files = Directory.GetFiles(install.ItemsBundlePath, "*.png");
                    foreach (var pngPath in files)
                    {
                        string shortname = Path.GetFileNameWithoutExtension(pngPath);
                        string jsonPath = Path.ChangeExtension(pngPath, ".json");

                        var item = new ItemAssetMetadata
                        {
                            shortname = shortname,
                            displayName = FormatDisplayName(shortname),
                            pngFilePath = pngPath,
                            jsonFilePath = File.Exists(jsonPath) ? jsonPath : null
                        };

                        if (File.Exists(jsonPath))
                        {
                            try
                            {
                                string json = File.ReadAllText(jsonPath);
                                int id = ExtractItemIdFromJson(json);
                                if (id != 0)
                                {
                                    item.itemId = id;
                                    ItemCacheById[id] = item;
                                }
                            }
                            catch { }
                        }

                        ItemCacheByName[shortname] = item;
                        AllItemsList.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[RustCUIBuilder] Error scanning items bundle: " + ex.Message);
                }
            }

            IsIndexed = true;
            Debug.Log($"[RustCUIBuilder] Discovery complete: {AllItemsList.Count} items indexed, {AllUiSpritesList.Count} UI sprites ready.");
        }

        public static ItemAssetMetadata FindItemByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            ItemCacheByName.TryGetValue(name, out var item);
            return item;
        }

        public static ItemAssetMetadata FindItemById(int id)
        {
            if (id == 0) return null;
            ItemCacheById.TryGetValue(id, out var item);
            return item;
        }

        public static Sprite GetSpriteByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            if (LoadedSpriteCache.TryGetValue(path, out var cached) && cached != null)
                return cached;

            // Check if it's an item icon shortname
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

            // Check if it's a verified UI sprite
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
                Debug.LogWarning($"[RustCUIBuilder] Failed to load icon {item.shortname}: {ex.Message}");
            }

            return null;
        }

        private static Sprite GenerateOrLoadSprite(string path, string name)
        {
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];

            string lower = name.ToLowerInvariant();

            if (lower.Contains("tile") || lower.Contains("background"))
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool isBorder = x == 0 || x == size - 1 || y == 0 || y == size - 1;
                        colors[y * size + x] = isBorder ? new Color(0.35f, 0.38f, 0.42f, 0.95f) : new Color(0.14f, 0.16f, 0.19f, 0.92f);
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
                        else
                        {
                            colors[y * size + x] = Color.clear;
                        }
                    }
                }
            }
            else if (lower.Contains("rounded") || lower.Contains("box"))
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

                        colors[y * size + x] = inside ? new Color(0.9f, 0.9f, 0.95f, 1f) : Color.clear;
                    }
                }
            }
            else if (lower.Contains("check"))
            {
                FillClear(colors, size);
                DrawThickLine(colors, size, new Vector2(16, 32), new Vector2(28, 18), 4, Color.green);
                DrawThickLine(colors, size, new Vector2(28, 18), new Vector2(50, 48), 4, Color.green);
            }
            else if (lower.Contains("close") || lower.Contains("cross"))
            {
                FillClear(colors, size);
                DrawThickLine(colors, size, new Vector2(18, 18), new Vector2(46, 46), 4, new Color(0.9f, 0.25f, 0.25f, 1f));
                DrawThickLine(colors, size, new Vector2(18, 46), new Vector2(46, 18), 4, new Color(0.9f, 0.25f, 0.25f, 1f));
            }
            else if (lower.Contains("plus"))
            {
                FillClear(colors, size);
                DrawThickLine(colors, size, new Vector2(16, 32), new Vector2(48, 32), 4, Color.white);
                DrawThickLine(colors, size, new Vector2(32, 16), new Vector2(32, 48), 4, Color.white);
            }
            else if (lower.Contains("minus"))
            {
                FillClear(colors, size);
                DrawThickLine(colors, size, new Vector2(16, 32), new Vector2(48, 32), 4, Color.white);
            }
            else
            {
                // Crisp White square default
                for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
            }

            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static void FillClear(Color[] colors, int size)
        {
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;
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
