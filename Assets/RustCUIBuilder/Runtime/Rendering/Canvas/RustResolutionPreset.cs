using System;
using System.Collections.Generic;
using UnityEngine;

namespace RustCUIBuilder.Runtime.Rendering.Canvas
{
    [Serializable]
    public class RustResolutionPreset
    {
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string AspectRatio { get; set; }
        public bool IsBaseReference { get; set; }

        public float AspectRatioFloat => (float)Width / Height;

        public RustResolutionPreset(string name, int width, int height, string aspect, bool isBase = false)
        {
            Name = name;
            Width = width;
            Height = height;
            AspectRatio = aspect;
            IsBaseReference = isBase;
        }

        public static readonly List<RustResolutionPreset> Presets = new List<RustResolutionPreset>
        {
            new RustResolutionPreset("1280 x 720 (720p - Rust Base Reference)", 1280, 720, "16:9", true),
            new RustResolutionPreset("1366 x 768 (Laptop Standard)", 1366, 768, "16:9"),
            new RustResolutionPreset("1600 x 900 (HD+)", 1600, 900, "16:9"),
            new RustResolutionPreset("1920 x 1080 (1080p FHD - Standard)", 1920, 1080, "16:9"),
            new RustResolutionPreset("2560 x 1440 (1440p QHD - 2K)", 2560, 1440, "16:9"),
            new RustResolutionPreset("3840 x 2160 (2160p UHD - 4K)", 3840, 2160, "16:9"),
            new RustResolutionPreset("1920 x 1200 (WUXGA)", 1920, 1200, "16:10"),
            new RustResolutionPreset("2560 x 1080 (UW-FHD Ultrawide)", 2560, 1080, "21:9"),
            new RustResolutionPreset("3440 x 1440 (UW-QHD Ultrawide)", 3440, 1440, "21:9"),
            new RustResolutionPreset("1024 x 768 (XGA Classic)", 1024, 768, "4:3")
        };
    }
}
