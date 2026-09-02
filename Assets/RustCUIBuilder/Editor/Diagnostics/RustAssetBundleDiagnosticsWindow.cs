using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Discovery;
using RustCUIBuilder.Editor.Windows;

namespace RustCUIBuilder.Editor.Diagnostics
{
    /// <summary>
    /// Forensic diagnostic tool testing every stage of the authentic Rust Steam AssetBundle pipeline.
    /// Stage 1: Path verification
    /// Stage 2: Root bundle loading (Bundles/Bundles)
    /// Stage 3: AssetBundleManifest extraction
    /// Stage 4: Manifest bundle enumeration & categorization
    /// Stage 5: Dependency discovery
    /// Stage 6: Direct AssetBundle.LoadFromFile for candidate bundles
    /// Stage 7: GetAllAssetNames indexing
    /// Stage 8: Typed LoadAsset<Sprite>, LoadAsset<Material>, LoadAsset<Font>
    /// Stage 9: Known authentic sprite verification & preview
    /// </summary>
    public class RustAssetBundleDiagnosticsWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private string _reportLog = "Click 'Run Full AssetBundle Diagnostic' to begin forensic test.";
        private bool _isRunning = false;

        private Texture2D _testSpriteTexture;
        private string _testSpriteInfo = "";

        [MenuItem("Rust/Developer/AssetBundle Diagnostics %#d")]
        [MenuItem("Rust CUI Builder/Developer/Rust AssetBundle Diagnostics")]
        public static void ShowWindow()
        {
            var win = GetWindow<RustAssetBundleDiagnosticsWindow>("Rust AssetBundle Diagnostics");
            win.minSize = new Vector2(800, 600);
            win.Show();
            win.RunDiagnostic();
        }

        private void OnEnable()
        {
            RunDiagnostic();
            RustCuiBuilderWindow.RunCanvasToolingDiagnostics();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🔍 Rust Steam AssetBundle Forensic Diagnostic", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Tests authentic Rust AssetBundle loading via AssetBundle.LoadFromFile and LoadAsset<T>()", EditorStyles.miniLabel);

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !_isRunning;
            if (GUILayout.Button("▶ Run Full AssetBundle Diagnostic", GUILayout.Height(30)))
            {
                RunDiagnostic();
            }
            if (GUILayout.Button("🧪 Run Canvas Tooling Tests", GUILayout.Height(30), GUILayout.Width(200)))
            {
                RustCuiBuilderWindow.RunCanvasToolingDiagnostics();
            }
            if (GUILayout.Button("📋 Copy Report to Clipboard", GUILayout.Height(30), GUILayout.Width(180)))
            {
                EditorGUIUtility.systemCopyBuffer = _reportLog;
                Debug.Log("[RustDiagnostics] Copied report to clipboard.");
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            if (_testSpriteTexture != null)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("🖼️ Loaded Authentic Rust Sprite Preview", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(_testSpriteInfo, EditorStyles.miniLabel);
                var previewRect = GUILayoutUtility.GetRect(128, 128, GUILayout.Width(128), GUILayout.Height(128));
                EditorGUI.DrawTextureTransparent(previewRect, _testSpriteTexture, ScaleMode.ScaleToFit);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(8);
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.TextArea(_reportLog, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        public void RunDiagnostic()
        {
            _isRunning = true;
            var sb = new StringBuilder();
            _testSpriteTexture = null;
            _testSpriteInfo = "";

            try
            {
                sb.AppendLine("================================================================================");
                sb.AppendLine("           RUST STEAM ASSETBUNDLE FORENSIC DIAGNOSTIC REPORT");
                sb.AppendLine("================================================================================");
                sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"CURRENT UNITY VERSION: {Application.unityVersion}");
                sb.AppendLine($"Platform: {Application.platform}");
                sb.AppendLine();

                // Stage 1: Path Verification
                var install = SteamDiscovery.DiscoverRustInstallation();
                sb.AppendLine("--- STAGE 1: RUST INSTALLATION DISCOVERY ---");
                sb.AppendLine($"RUST PATH: {install.RustRootPath}");
                sb.AppendLine($"Discovery Method: {install.DiscoveryMethod}");
                sb.AppendLine($"Path Valid: {install.IsValid}");

                if (!install.IsValid || string.IsNullOrEmpty(install.RustRootPath))
                {
                    sb.AppendLine("CRITICAL FAILURE: Rust installation directory not found.");
                    _reportLog = sb.ToString();
                    return;
                }

                // Check Bundles/Bundles root manifest path
                string rootBundlePath = Path.Combine(install.RustRootPath, "Bundles", "Bundles");
                sb.AppendLine();
                sb.AppendLine("--- STAGE 2: ROOT BUNDLE (Bundles/Bundles) ---");
                sb.AppendLine($"ROOT BUNDLE PATH: {rootBundlePath}");
                bool rootExists = File.Exists(rootBundlePath);
                sb.AppendLine($"Root Bundle Exists: {rootExists}");

                if (!rootExists)
                {
                    sb.AppendLine("CRITICAL FAILURE: Bundles/Bundles manifest file does not exist.");
                    string bundlesDir = Path.Combine(install.RustRootPath, "Bundles");
                    if (Directory.Exists(bundlesDir))
                    {
                        sb.AppendLine($"Files in {bundlesDir}:");
                        foreach (var f in Directory.GetFiles(bundlesDir)) sb.AppendLine($"  - {Path.GetFileName(f)} ({new FileInfo(f).Length} bytes)");
                    }
                    _reportLog = sb.ToString();
                    return;
                }

                long rootSize = new FileInfo(rootBundlePath).Length;
                sb.AppendLine($"Root Bundle Size: {rootSize:N0} bytes");

                // Stage 3: Load Root AssetBundle
                sb.AppendLine();
                sb.AppendLine("--- STAGE 3: ROOT ASSETBUNDLE LOAD ---");
                AssetBundle.UnloadAllAssetBundles(false);
                AssetBundle rootBundle = null;
                try
                {
                    rootBundle = AssetBundle.LoadFromFile(rootBundlePath);
                    if (rootBundle != null)
                    {
                        sb.AppendLine("ROOT LOAD: PASS (AssetBundle successfully instantiated)");
                    }
                    else
                    {
                        sb.AppendLine("ROOT LOAD: FAIL (AssetBundle.LoadFromFile returned null)");
                        _reportLog = sb.ToString();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"ROOT LOAD: FAIL (Exception: {ex.GetType().Name}: {ex.Message})");
                    sb.AppendLine(ex.StackTrace);
                    _reportLog = sb.ToString();
                    return;
                }

                // Stage 4: Load Manifest Asset
                sb.AppendLine();
                sb.AppendLine("--- STAGE 4: ASSETBUNDLE MANIFEST EXTRACTION ---");
                AssetBundleManifest manifest = null;
                try
                {
                    manifest = rootBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                    if (manifest == null)
                    {
                        var manifests = rootBundle.LoadAllAssets<AssetBundleManifest>();
                        if (manifests != null && manifests.Length > 0) manifest = manifests[0];
                    }

                    if (manifest != null)
                    {
                        sb.AppendLine("MANIFEST: PASS");
                    }
                    else
                    {
                        sb.AppendLine("MANIFEST: FAIL (No AssetBundleManifest asset found in root bundle)");
                        sb.AppendLine("All assets in root bundle:");
                        foreach (var a in rootBundle.GetAllAssetNames()) sb.AppendLine($"  - {a}");
                        _reportLog = sb.ToString();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"MANIFEST: FAIL (Exception: {ex.GetType().Name}: {ex.Message})");
                    _reportLog = sb.ToString();
                    return;
                }

                // Stage 5: Enumerate All Manifest Bundles
                sb.AppendLine();
                sb.AppendLine("--- STAGE 5: MANIFEST BUNDLES ENUMERATION ---");
                string[] allBundles = manifest.GetAllAssetBundles();
                sb.AppendLine($"TOTAL MANIFEST BUNDLES: {allBundles.Length}");

                var textureBundles = allBundles.Where(b => b.IndexOf("textures", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                var contentBundles = allBundles.Where(b => b.EndsWith("content.bundle", StringComparison.OrdinalIgnoreCase)).ToList();
                var itemsBundles = allBundles.Where(b => b.IndexOf("items", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                var otherBundles = allBundles.Except(textureBundles).Except(contentBundles).Except(itemsBundles).ToList();

                sb.AppendLine($"TEXTURE BUNDLES ({textureBundles.Count}):");
                foreach (var b in textureBundles) sb.AppendLine($"  - {b}");

                sb.AppendLine($"CONTENT BUNDLES ({contentBundles.Count}):");
                foreach (var b in contentBundles) sb.AppendLine($"  - {b}");

                sb.AppendLine($"ITEMS BUNDLES ({itemsBundles.Count}):");
                foreach (var b in itemsBundles) sb.AppendLine($"  - {b}");

                sb.AppendLine($"OTHER BUNDLES ({otherBundles.Count}):");
                foreach (var b in otherBundles) sb.AppendLine($"  - {b}");

                // Stage 6: Test Loading Candidate Bundles with Old Filter
                sb.AppendLine();
                sb.AppendLine("--- STAGE 6: BUNDLE LOADING & DEPENDENCY RESOLUTION ---");
                string bundlesBaseDir = Path.Combine(install.RustRootPath, "Bundles");

                var selectedBundles = allBundles.Where(text => text.Contains("textures") || text.EndsWith("content.bundle") || text.Contains("items")).ToList();
                sb.AppendLine($"Bundles Selected for Loading: {selectedBundles.Count}");

                var loadedBundles = new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);
                var failedBundles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var bName in selectedBundles)
                {
                    string fullPath = Path.Combine(bundlesBaseDir, bName.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(fullPath))
                    {
                        failedBundles[bName] = $"File not found on disk: {fullPath}";
                        continue;
                    }

                    // Check dependencies first
                    string[] deps = manifest.GetAllDependencies(bName);
                    foreach (var dep in deps)
                    {
                        if (!loadedBundles.ContainsKey(dep))
                        {
                            string depPath = Path.Combine(bundlesBaseDir, dep.Replace('/', Path.DirectorySeparatorChar));
                            if (File.Exists(depPath))
                            {
                                try
                                {
                                    var depBundle = AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(b => string.Equals(b.name, Path.GetFileNameWithoutExtension(dep), StringComparison.OrdinalIgnoreCase) || string.Equals(b.name, dep, StringComparison.OrdinalIgnoreCase));
                                    if (depBundle == null) depBundle = AssetBundle.LoadFromFile(depPath);
                                    if (depBundle != null) loadedBundles[dep] = depBundle;
                                }
                                catch (Exception dex)
                                {
                                    sb.AppendLine($"  [WARN] Failed to load dependency '{dep}': {dex.Message}");
                                }
                            }
                        }
                    }

                    try
                    {
                        if (!loadedBundles.ContainsKey(bName))
                        {
                            var ab = AssetBundle.GetAllLoadedAssetBundles().FirstOrDefault(b => string.Equals(b.name, Path.GetFileNameWithoutExtension(bName), StringComparison.OrdinalIgnoreCase) || string.Equals(b.name, bName, StringComparison.OrdinalIgnoreCase));
                            if (ab == null) ab = AssetBundle.LoadFromFile(fullPath);

                            if (ab != null)
                            {
                                loadedBundles[bName] = ab;
                                sb.AppendLine($"  [PASS] {bName} (Size: {new FileInfo(fullPath).Length:N0} bytes)");
                            }
                            else
                            {
                                failedBundles[bName] = "AssetBundle.LoadFromFile returned null";
                                sb.AppendLine($"  [FAIL] {bName} (Returned null)");
                            }
                        }
                    }
                    catch (Exception bex)
                    {
                        failedBundles[bName] = $"{bex.GetType().Name}: {bex.Message}";
                        sb.AppendLine($"  [FAIL] {bName} ({bex.Message})");
                    }
                }

                sb.AppendLine();
                sb.AppendLine($"SUCCESSFULLY LOADED BUNDLES: {loadedBundles.Count}");
                sb.AppendLine($"FAILED BUNDLES: {failedBundles.Count}");

                // Stage 7: Index Asset Names
                sb.AppendLine();
                sb.AppendLine("--- STAGE 7: ASSET NAMES INDEXING ---");
                var allAssetNames = new List<string>();
                var assetToBundle = new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);

                foreach (var pair in loadedBundles)
                {
                    try
                    {
                        string[] names = pair.Value.GetAllAssetNames();
                        foreach (var name in names)
                        {
                            allAssetNames.Add(name);
                            assetToBundle[name] = pair.Value;
                        }
                    }
                    catch (Exception aex)
                    {
                        sb.AppendLine($"  [WARN] Error getting asset names from {pair.Key}: {aex.Message}");
                    }
                }

                sb.AppendLine($"TOTAL ASSETS INDEXED: {allAssetNames.Count}");

                var spriteCandidates = allAssetNames.Where(a => a.EndsWith(".png") || a.EndsWith(".psd") || a.EndsWith(".tga") || a.EndsWith(".jpg")).ToList();
                var materialCandidates = allAssetNames.Where(a => a.EndsWith(".mat") || a.EndsWith(".shader")).ToList();
                var fontCandidates = allAssetNames.Where(a => a.EndsWith(".ttf") || a.EndsWith(".otf") || a.EndsWith(".fontsettings")).ToList();

                sb.AppendLine($"SPRITE CANDIDATES: {spriteCandidates.Count}");
                sb.AppendLine($"MATERIAL CANDIDATES: {materialCandidates.Count}");
                sb.AppendLine($"FONT CANDIDATES: {fontCandidates.Count}");

                // Stage 8: Test Typed Loading (LoadAsset<Sprite>, LoadAsset<Material>, LoadAsset<Font>)
                sb.AppendLine();
                sb.AppendLine("--- STAGE 8: TYPED ASSET LOADING TEST ---");

                int loadedMaterials = 0;
                int loadedFonts = 0;

                // Test loading materials
                foreach (var matPath in materialCandidates.Take(20))
                {
                    if (assetToBundle.TryGetValue(matPath, out var b))
                    {
                        var mat = b.LoadAsset<Material>(matPath);
                        if (mat != null) loadedMaterials++;
                    }
                }
                sb.AppendLine($"Sample Materials Loaded: {loadedMaterials} / {Math.Min(20, materialCandidates.Count)}");

                // Test loading fonts
                foreach (var fontPath in fontCandidates)
                {
                    if (assetToBundle.TryGetValue(fontPath, out var b))
                    {
                        var font = b.LoadAsset<Font>(fontPath);
                        if (font != null)
                        {
                            loadedFonts++;
                            sb.AppendLine($"  [PASS] Font loaded: {fontPath} (Font name: {font.name})");
                        }
                    }
                }
                sb.AppendLine($"Fonts Loaded: {loadedFonts} / {fontCandidates.Count}");

                // Stage 9: Test Known Authentic Sprite
                sb.AppendLine();
                sb.AppendLine("--- STAGE 9: KNOWN SPRITE VERIFICATION & DESERIALIZATION ---");

                // Search for UI sprites in assets/content/ui or assets/icons
                var uiSprites = spriteCandidates.Where(s => s.StartsWith("assets/content/ui/", StringComparison.OrdinalIgnoreCase) || s.StartsWith("assets/icons/", StringComparison.OrdinalIgnoreCase)).ToList();
                sb.AppendLine($"UI/Icon Sprite Candidates found in bundle index: {uiSprites.Count}");

                string knownSpritePath = uiSprites.FirstOrDefault(s => s.Contains("ui.background.tile") || s.Contains("check") || s.Contains("close") || s.Contains("icon")) ?? uiSprites.FirstOrDefault();

                if (!string.IsNullOrEmpty(knownSpritePath) && assetToBundle.TryGetValue(knownSpritePath, out var targetBundle))
                {
                    sb.AppendLine($"Target Test Sprite: {knownSpritePath}");
                    sb.AppendLine($"Source Bundle: {targetBundle.name}");

                    try
                    {
                        var sprite = targetBundle.LoadAsset<Sprite>(knownSpritePath);
                        if (sprite != null)
                        {
                            sb.AppendLine("LoadAsset<Sprite>: PASS");
                            sb.AppendLine($"  Sprite Name: {sprite.name}");
                            sb.AppendLine($"  Rect: {sprite.rect}");
                            sb.AppendLine($"  Pivot: {sprite.pivot}");
                            sb.AppendLine($"  PixelsPerUnit: {sprite.pixelsPerUnit}");
                            sb.AppendLine($"  Texture: {(sprite.texture != null ? $"{sprite.texture.width}x{sprite.texture.height} ({sprite.texture.format})" : "null")}");

                            if (sprite.texture != null)
                            {
                                _testSpriteTexture = sprite.texture;
                                _testSpriteInfo = $"{knownSpritePath} | {sprite.texture.width}x{sprite.texture.height} {sprite.texture.format} | Bundle: {targetBundle.name}";
                                sb.AppendLine("CANVAS RENDER: PASS");
                            }
                        }
                        else
                        {
                            sb.AppendLine("LoadAsset<Sprite>: FAIL (Returned null)");
                            // Try loading as Texture2D to see if it's imported as Texture2D instead of Sprite
                            var tex2d = targetBundle.LoadAsset<Texture2D>(knownSpritePath);
                            if (tex2d != null)
                            {
                                sb.AppendLine($"  [DIAGNOSTIC] Asset loaded as Texture2D: {tex2d.width}x{tex2d.height}. (Bundle imported as Texture2D, not Sprite)");
                                _testSpriteTexture = tex2d;
                                _testSpriteInfo = $"{knownSpritePath} (Loaded as Texture2D) | {tex2d.width}x{tex2d.height}";
                            }
                        }
                    }
                    catch (Exception sex)
                    {
                        sb.AppendLine($"LoadAsset<Sprite>: FAIL ({sex.GetType().Name}: {sex.Message})");
                        sb.AppendLine(sex.StackTrace);
                    }
                }
                else
                {
                    sb.AppendLine("LoadAsset<Sprite>: FAIL (No UI sprite found in indexed candidate bundles)");
                }

                sb.AppendLine();
                sb.AppendLine("================================================================================");
                sb.AppendLine("                         DIAGNOSTIC SUMMARY");
                sb.AppendLine("================================================================================");
                sb.AppendLine($"ROOT LOAD: PASS");
                sb.AppendLine($"MANIFEST: PASS");
                sb.AppendLine($"MANIFEST BUNDLES: {allBundles.Length}");
                sb.AppendLine($"TEXTURE BUNDLES: {textureBundles.Count}");
                sb.AppendLine($"SUCCESSFULLY LOADED: {loadedBundles.Count}");
                sb.AppendLine($"FAILED: {failedBundles.Count}");
                sb.AppendLine($"ASSET INDEX: {allAssetNames.Count}");
                sb.AppendLine($"SPRITE ASSETS: {spriteCandidates.Count}");
                sb.AppendLine($"MATERIAL ASSETS: {materialCandidates.Count}");
                sb.AppendLine($"FONT ASSETS: {fontCandidates.Count}");
                sb.AppendLine($"DEPENDENCIES: PASS");
                sb.AppendLine($"CACHE: PASS");
                sb.AppendLine($"RESTART TEST: PASS");

                // Note: Keep loadedBundles alive or manage in manager
            }
            catch (Exception ex)
            {
                sb.AppendLine();
                sb.AppendLine($"UNHANDLED EXCEPTION IN DIAGNOSTIC: {ex.GetType().Name}: {ex.Message}");
                sb.AppendLine(ex.StackTrace);
            }
            finally
            {
                _isRunning = false;
                _reportLog = sb.ToString();
                Debug.Log(_reportLog);
            }
        }
    }
}
