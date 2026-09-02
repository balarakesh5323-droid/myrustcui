using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;

namespace RustCUIBuilder.Runtime.Rendering.Canvas
{
    /// <summary>
    /// Contract for bi-directional coordinate conversions between
    /// Screen (Unity Editor GUI pixels), Canvas (Virtual viewport pixels),
    /// and Rust CUI (Normalized anchors [0..1] + Pixel offsets).
    /// </summary>
    public interface ICanvasCoordinateSystem
    {
        Vector2 ScreenToCanvas(Vector2 screenPos, Rect viewportRect, Vector2 pan, float zoom);
        Vector2 CanvasToScreen(Vector2 canvasPos, Rect viewportRect, Vector2 pan, float zoom);

        Rect ScreenToCanvas(Rect screenRect, Rect viewportRect, Vector2 pan, float zoom);
        Rect CanvasToScreen(Rect canvasRect, Rect viewportRect, Vector2 pan, float zoom);

        Vector2 RustToCanvas(Vector2 rustNormalized, float canvasWidth, float canvasHeight);
        Vector2 CanvasToRust(Vector2 canvasPos, float canvasWidth, float canvasHeight);

        Rect GetElementCanvasRect(CuiElementNode elem, CuiDocument doc, float canvasWidth, float canvasHeight);
        Rect GetParentCanvasRect(CuiElementNode elem, CuiDocument doc, float canvasWidth, float canvasHeight);
        Rect GetElementScreenRect(CuiElementNode elem, CuiDocument doc, Rect viewportRect, Vector2 pan, float zoom, float canvasWidth, float canvasHeight);

        Vector2[] GetAnchorScreenPoints(CuiElementNode elem, CuiDocument doc, Rect viewportRect, Vector2 pan, float zoom, float canvasWidth, float canvasHeight);
        Vector2 GetPivotScreenPoint(CuiElementNode elem, CuiDocument doc, Rect viewportRect, Vector2 pan, float zoom, float canvasWidth, float canvasHeight);
    }
}
