using System;
using UnityEngine;

namespace RustCUIBuilder.Runtime.Core.Models
{
    public interface ICuiComponent
    {
        string Type { get; }
        ICuiComponent Clone();
    }

    public interface ICuiColor
    {
        string Color { get; set; }
    }

    public interface ICuiEnableable
    {
        bool? Enabled { get; set; }
    }

    public interface ICuiGraphic
    {
        float FadeIn { get; set; }
        string PlaceholderParentId { get; set; }
        bool? BlocksRaycast { get; set; }
    }

    public static class CuiColorExtensions
    {
        public static Color ToUnityColor(string cuiColorStr, Color defaultColor)
        {
            if (string.IsNullOrWhiteSpace(cuiColorStr))
                return defaultColor;

            var parts = cuiColorStr.Trim().Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3)
                return defaultColor;

            float r = float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedR) ? parsedR : 1f;
            float g = float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedG) ? parsedG : 1f;
            float b = float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedB) ? parsedB : 1f;
            float a = parts.Length >= 4 && float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedA) ? parsedA : 1f;

            return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b), Mathf.Clamp01(a));
        }

        public static string ToCuiColorString(Color color)
        {
            return $"{color.r.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)} {color.g.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)} {color.b.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)} {color.a.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}";
        }
    }
}
