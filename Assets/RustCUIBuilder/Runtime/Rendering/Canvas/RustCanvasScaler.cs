using System;
using UnityEngine;

namespace RustCUIBuilder.Runtime.Rendering.Canvas
{
    /// <summary>
    /// Implements precision math for Rust CUI coordinates and RectTransform layout calculations.
    /// In Rust CUI, anchors are normalized 0.0 to 1.0, and pixel offsets are defined relative to a 1280x720 reference resolution.
    /// </summary>
    public static class RustCanvasScaler
    {
        public const float ReferenceWidth = 1280f;
        public const float ReferenceHeight = 720f;

        public static Vector2 ParseVector2(string str, Vector2 defaultVal)
        {
            if (string.IsNullOrWhiteSpace(str)) return defaultVal;
            var parts = str.Trim().Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return defaultVal;

            float x = float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedX) ? parsedX : defaultVal.x;
            float y = float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsedY) ? parsedY : defaultVal.y;

            return new Vector2(x, y);
        }

        public static string FormatVector2(Vector2 vec, string format = "0.###")
        {
            return $"{vec.x.ToString(format, System.Globalization.CultureInfo.InvariantCulture)} {vec.y.ToString(format, System.Globalization.CultureInfo.InvariantCulture)}";
        }

        public static void ApplyToRectTransform(RectTransform rt, string anchorMinStr, string anchorMaxStr, string offsetMinStr, string offsetMaxStr, string pivotStr = "0.5 0.5", float rotation = 0f)
        {
            if (rt == null) return;

            Vector2 anchorMin = ParseVector2(anchorMinStr, Vector2.zero);
            Vector2 anchorMax = ParseVector2(anchorMaxStr, Vector2.one);
            Vector2 offsetMin = ParseVector2(offsetMinStr, Vector2.zero);
            Vector2 offsetMax = ParseVector2(offsetMaxStr, Vector2.zero);
            Vector2 pivot = ParseVector2(pivotStr, new Vector2(0.5f, 0.5f));

            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            rt.localEulerAngles = new Vector3(0f, 0f, rotation);
        }

        public static void ExtractFromRectTransform(RectTransform rt, out string anchorMinStr, out string anchorMaxStr, out string offsetMinStr, out string offsetMaxStr, out string pivotStr, out float rotation)
        {
            if (rt == null)
            {
                anchorMinStr = "0 0";
                anchorMaxStr = "1 1";
                offsetMinStr = "0 0";
                offsetMaxStr = "0 0";
                pivotStr = "0.5 0.5";
                rotation = 0f;
                return;
            }

            anchorMinStr = FormatVector2(rt.anchorMin);
            anchorMaxStr = FormatVector2(rt.anchorMax);
            offsetMinStr = FormatVector2(rt.offsetMin, "0.#");
            offsetMaxStr = FormatVector2(rt.offsetMax, "0.#");
            pivotStr = FormatVector2(rt.pivot);
            rotation = rt.localEulerAngles.z;
        }

        /// <summary>
        /// Computes the exact pixel rectangle within a parent bounding rectangle given anchors and offsets.
        /// </summary>
        public static Rect CalculateScreenRect(Rect parentRect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            float xMin = parentRect.xMin + parentRect.width * anchorMin.x + offsetMin.x;
            float yMin = parentRect.yMin + parentRect.height * anchorMin.y + offsetMin.y;
            float xMax = parentRect.xMin + parentRect.width * anchorMax.x + offsetMax.x;
            float yMax = parentRect.yMin + parentRect.height * anchorMax.y + offsetMax.y;

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
