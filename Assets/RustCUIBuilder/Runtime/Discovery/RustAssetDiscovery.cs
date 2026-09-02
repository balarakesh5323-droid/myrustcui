using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RustCUIBuilder.Runtime.Discovery
{
    /// <summary>
    /// Discovers and caches Rust assets including 2,800+ item icons from Bundles/items,
    /// verified fonts, materials, UI layers, and sprite definitions.
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
            "assets/content/ui/ui.circle.gradient.psd",
            "assets/icons/check.png",
            "assets/icons/close.png",
            "assets/icons/circle_closed.png",
            "assets/icons/device_add.png",
            "assets/icons/fun.png",
            "assets/icons/facepunch.png",
            "assets/icons/explosion_sprite.png",
            "assets/icons/embrella.png"
        };

        private static readonly Dictionary<string, ItemAssetMetadata> ItemCacheByName = new Dictionary<string, ItemAssetMetadata>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<int, ItemAssetMetadata> ItemCacheById = new Dictionary<int, ItemAssetMetadata>();
        private static readonly List<ItemAssetMetadata> AllItemsList = new List<ItemAssetMetadata>();
        private static readonly Dictionary<string, Sprite> LoadedSpriteCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        public static bool IsIndexed { get; private set; }
        public static int TotalItemCount => AllItemsList.Count;
        public static IReadOnlyList<ItemAssetMetadata> AllItems => AllItemsList;

        public static void ReindexAssets()
        {
            ItemCacheByName.Clear();
            ItemCacheById.Clear();
            AllItemsList.Clear();
            LoadedSpriteCache.Clear();

            var install = SteamDiscovery.DiscoverRustInstallation();
            if (!install.IsValid || string.IsNullOrEmpty(install.ItemsBundlePath))
            {
                IsIndexed = false;
                return;
            }

            try
            {
                var pngFiles = Directory.GetFiles(install.ItemsBundlePath, "*.png");
                foreach (var png in pngFiles)
                {
                    string filenameNoExt = Path.GetFileNameWithoutExtension(png);
                    string jsonPath = Path.Combine(install.ItemsBundlePath, filenameNoExt + ".json");

                    var meta = new ItemAssetMetadata
                    {
                        shortname = filenameNoExt,
                        displayName = CleanDisplayName(filenameNoExt),
                        pngFilePath = png,
                        jsonFilePath = File.Exists(jsonPath) ? jsonPath : null
                    };

                    if (File.Exists(jsonPath))
                    {
                        try
                        {
                            string jsonContent = File.ReadAllText(jsonPath);
                            // Simple parsing of itemid if present
                            int idIndex = jsonContent.IndexOf("\"itemid\":", StringComparison.OrdinalIgnoreCase);
                            if (idIndex >= 0)
                            {
                                int commaIndex = jsonContent.IndexOf(',', idIndex);
                                int braceIndex = jsonContent.IndexOf('}', idIndex);
                                int end = (commaIndex > 0 && commaIndex < braceIndex) ? commaIndex : braceIndex;
                                if (end > idIndex)
                                {
                                    string idSub = jsonContent.Substring(idIndex + 9, end - (idIndex + 9)).Trim();
                                    if (int.TryParse(idSub, out int parsedId))
                                    {
                                        meta.itemId = parsedId;
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    ItemCacheByName[meta.shortname] = meta;
                    if (meta.itemId != 0 && !ItemCacheById.ContainsKey(meta.itemId))
                    {
                        ItemCacheById[meta.itemId] = meta;
                    }
                    AllItemsList.Add(meta);
                }

                IsIndexed = true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RustCUIBuilder] Failed indexing items: {ex.Message}");
                IsIndexed = false;
            }
        }

        public static ItemAssetMetadata FindItemByShortname(string shortname)
        {
            if (!IsIndexed) ReindexAssets();
            if (string.IsNullOrEmpty(shortname)) return null;
            ItemCacheByName.TryGetValue(shortname, out var item);
            return item;
        }

        public static ItemAssetMetadata FindItemById(int itemId)
        {
            if (!IsIndexed) ReindexAssets();
            ItemCacheById.TryGetValue(itemId, out var item);
            return item;
        }

        public static Sprite LoadItemIcon(ItemAssetMetadata item)
        {
            if (item == null || string.IsNullOrEmpty(item.pngFilePath) || !File.Exists(item.pngFilePath))
                return null;

            if (LoadedSpriteCache.TryGetValue(item.shortname, out var cached))
                return cached;

            try
            {
                byte[] bytes = File.ReadAllBytes(item.pngFilePath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    tex.name = item.shortname;
                    var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    sprite.name = item.shortname;
                    LoadedSpriteCache[item.shortname] = sprite;
                    return sprite;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RustCUIBuilder] Failed loading item sprite {item.shortname}: {ex.Message}");
            }

            return null;
        }

        private static string CleanDisplayName(string shortname)
        {
            if (string.IsNullOrEmpty(shortname)) return string.Empty;
            string formatted = shortname.Replace('.', ' ').Replace('_', ' ');
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(formatted);
        }
    }
}
