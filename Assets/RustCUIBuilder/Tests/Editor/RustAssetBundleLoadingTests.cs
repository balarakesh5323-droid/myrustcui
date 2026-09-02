using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RustCUIBuilder.Runtime.Discovery;

namespace RustCUIBuilder.Tests.Editor
{
    public class RustAssetBundleLoadingTests
    {
        [Test]
        public void RustAssetBundle_RootAndManifest_LoadSuccessfully()
        {
            var install = SteamDiscovery.DiscoverRustInstallation();
            Assert.IsTrue(install.IsValid, "Rust installation must be discovered.");

            string rootBundlePath = Path.Combine(install.RustRootPath, "Bundles", "Bundles");
            Assert.IsTrue(File.Exists(rootBundlePath), $"Root bundle must exist at: {rootBundlePath}");

            var rootBundle = AssetBundle.LoadFromFile(rootBundlePath);
            Assert.IsNotNull(rootBundle, "Root bundle must load via AssetBundle.LoadFromFile");

            try
            {
                var manifests = rootBundle.LoadAllAssets<AssetBundleManifest>();
                Assert.IsNotNull(manifests, "LoadAllAssets<AssetBundleManifest> must return array");
                Assert.IsTrue(manifests.Length > 0, "At least one manifest must be present in root bundle");

                var manifest = manifests[0];
                string[] allBundles = manifest.GetAllAssetBundles();
                Assert.IsTrue(allBundles.Length > 0, "Manifest must list asset bundles");
                Debug.Log($"[Test] Found {allBundles.Length} total manifest bundles.");

                // Check texture or content bundles
                var candidate = allBundles.FirstOrDefault(b => b.EndsWith("content.bundle") || b.Contains("items") || b.Contains("textures"));
                Assert.IsNotNull(candidate, "At least one candidate bundle must exist");

                string candidatePath = Path.Combine(install.RustRootPath, "Bundles", candidate.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidatePath))
                {
                    var bundle = AssetBundle.LoadFromFile(candidatePath);
                    Assert.IsNotNull(bundle, $"Candidate bundle '{candidate}' must load");

                    string[] assetNames = bundle.GetAllAssetNames();
                    Debug.Log($"[Test] Loaded {candidate} with {assetNames.Length} assets.");
                    Assert.IsTrue(assetNames.Length > 0, "Bundle must contain asset names");

                    bundle.Unload(false);
                }
            }
            finally
            {
                rootBundle.Unload(true);
            }
        }
    }
}
