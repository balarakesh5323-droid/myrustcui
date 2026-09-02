using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RustCUIBuilder.Runtime.Discovery
{
    public static class SteamDiscovery
    {
        private const string CustomRustPathPrefKey = "RustCUIBuilder_CustomRustPath";
        private const string RustAppId = "252490";

        private static readonly string[] CommonSteamPaths = new string[]
        {
            "C:/Program Files (x86)/Steam",
            "C:/Program Files/Steam",
            "D:/Steam",
            "D:/SteamLibrary",
            "E:/Steam",
            "E:/SteamLibrary",
            "F:/Steam",
            "F:/SteamLibrary"
        };

        public class RustInstallationInfo
        {
            public bool IsValid { get; set; }
            public string RustRootPath { get; set; }
            public string BundlesPath { get; set; }
            public string ItemsBundlePath { get; set; }
            public string TexturesBundlePath { get; set; }
            public string RustClientDataPath { get; set; }
            public string ExecutablePath { get; set; }
            public string DiscoveryMethod { get; set; }
            public int DiscoveredItemIconCount { get; set; }
        }

        public static string GetCustomRustPath()
        {
            return PlayerPrefs.GetString(CustomRustPathPrefKey, string.Empty);
        }

        public static void SetCustomRustPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                PlayerPrefs.DeleteKey(CustomRustPathPrefKey);
            }
            else
            {
                PlayerPrefs.SetString(CustomRustPathPrefKey, path);
            }
            PlayerPrefs.Save();
        }

        public static RustInstallationInfo DiscoverRustInstallation()
        {
            string customPath = GetCustomRustPath();
            if (!string.IsNullOrEmpty(customPath) && ValidateRustPath(customPath, out var customInfo))
            {
                customInfo.DiscoveryMethod = "User Custom Override";
                return customInfo;
            }

            var steamRoots = DiscoverSteamRoots();

            foreach (var steamRoot in steamRoots)
            {
                var libraries = DiscoverLibraryFolders(steamRoot);
                foreach (var library in libraries)
                {
                    string candidateRust = Path.Combine(library, "steamapps", "common", "Rust");
                    if (ValidateRustPath(candidateRust, out var info))
                    {
                        info.DiscoveryMethod = "Steam Library (" + library + ")";
                        return info;
                    }
                }
            }

            return new RustInstallationInfo
            {
                IsValid = false,
                DiscoveryMethod = "None (Not Found)"
            };
        }

        public static List<string> DiscoverSteamRoots()
        {
            var roots = new List<string>();

            try
            {
                var registryType = Type.GetType("Microsoft.Win32.Registry, mscorlib") ??
                                   Type.GetType("Microsoft.Win32.Registry, Microsoft.Win32.Registry");
                if (registryType != null)
                {
                    var currentUserProp = registryType.GetProperty("CurrentUser", BindingFlags.Public | BindingFlags.Static);
                    var localMachineProp = registryType.GetProperty("LocalMachine", BindingFlags.Public | BindingFlags.Static);

                    if (currentUserProp != null)
                    {
                        var currentUserKey = currentUserProp.GetValue(null);
                        if (currentUserKey != null)
                        {
                            var openSubKeyMethod = currentUserKey.GetType().GetMethod("OpenSubKey", new Type[] { typeof(string) });
                            var steamKey = openSubKeyMethod?.Invoke(currentUserKey, new object[] { "Software\\Valve\\Steam" });
                            if (steamKey != null)
                            {
                                var getValueMethod = steamKey.GetType().GetMethod("GetValue", new Type[] { typeof(string) });
                                var pathVal = getValueMethod?.Invoke(steamKey, new object[] { "SteamPath" }) as string;
                                if (!string.IsNullOrEmpty(pathVal))
                                {
                                    pathVal = pathVal.Replace('/', '\\');
                                    if (Directory.Exists(pathVal) && !roots.Contains(pathVal))
                                        roots.Add(pathVal);
                                }
                            }
                        }
                    }

                    if (localMachineProp != null)
                    {
                        var localMachineKey = localMachineProp.GetValue(null);
                        if (localMachineKey != null)
                        {
                            var openSubKeyMethod = localMachineKey.GetType().GetMethod("OpenSubKey", new Type[] { typeof(string) });
                            var steamKey = openSubKeyMethod?.Invoke(localMachineKey, new object[] { "SOFTWARE\\WOW6432Node\\Valve\\Steam" }) ??
                                           openSubKeyMethod?.Invoke(localMachineKey, new object[] { "SOFTWARE\\Valve\\Steam" });
                            if (steamKey != null)
                            {
                                var getValueMethod = steamKey.GetType().GetMethod("GetValue", new Type[] { typeof(string) });
                                var pathVal = getValueMethod?.Invoke(steamKey, new object[] { "InstallPath" }) as string;
                                if (!string.IsNullOrEmpty(pathVal))
                                {
                                    pathVal = pathVal.Replace('/', '\\');
                                    if (Directory.Exists(pathVal) && !roots.Contains(pathVal))
                                        roots.Add(pathVal);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RustCUIBuilder] Registry check warning: " + ex.Message);
            }

            foreach (var commonPath in CommonSteamPaths)
            {
                string norm = commonPath.Replace('/', '\\');
                if (Directory.Exists(norm) && !roots.Contains(norm))
                {
                    roots.Add(norm);
                }
            }

            return roots;
        }

        public static List<string> DiscoverLibraryFolders(string steamRoot)
        {
            var libraryFolders = new List<string>();
            if (string.IsNullOrEmpty(steamRoot) || !Directory.Exists(steamRoot))
                return libraryFolders;

            libraryFolders.Add(steamRoot);

            string vdfPath = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdfPath))
            {
                try
                {
                    string content = File.ReadAllText(vdfPath);
                    var matches = Regex.Matches(content, "\"path\"\\s+\"([^\"]+)\"", RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        if (match.Groups.Count > 1)
                        {
                            string path = match.Groups[1].Value.Replace("\\\\", "\\");
                            if (Directory.Exists(path) && !libraryFolders.Contains(path))
                            {
                                libraryFolders.Add(path);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[RustCUIBuilder] Error parsing libraryfolders.vdf: " + ex.Message);
                }
            }

            return libraryFolders;
        }

        public static bool ValidateRustPath(string candidatePath, out RustInstallationInfo info)
        {
            info = new RustInstallationInfo { IsValid = false };

            if (string.IsNullOrEmpty(candidatePath) || !Directory.Exists(candidatePath))
                return false;

            string clientExe = Path.Combine(candidatePath, "RustClient.exe");
            string altExe = Path.Combine(candidatePath, "Rust.exe");
            string bundlesDir = Path.Combine(candidatePath, "Bundles");
            string itemsDir = Path.Combine(bundlesDir, "items");
            string texturesDir = Path.Combine(bundlesDir, "textures");
            string dataDir = Path.Combine(candidatePath, "RustClient_Data");

            if (!File.Exists(clientExe) && !File.Exists(altExe))
                return false;

            int itemCount = 0;
            if (Directory.Exists(itemsDir))
            {
                try
                {
                    itemCount = Directory.GetFiles(itemsDir, "*.png").Length;
                }
                catch { }
            }

            info.IsValid = true;
            info.RustRootPath = Path.GetFullPath(candidatePath);
            info.BundlesPath = Directory.Exists(bundlesDir) ? Path.GetFullPath(bundlesDir) : null;
            info.ItemsBundlePath = Directory.Exists(itemsDir) ? Path.GetFullPath(itemsDir) : null;
            info.TexturesBundlePath = Directory.Exists(texturesDir) ? Path.GetFullPath(texturesDir) : null;
            info.RustClientDataPath = Directory.Exists(dataDir) ? Path.GetFullPath(dataDir) : null;
            info.ExecutablePath = File.Exists(clientExe) ? clientExe : altExe;
            info.DiscoveredItemIconCount = itemCount;

            return true;
        }
    }
}
