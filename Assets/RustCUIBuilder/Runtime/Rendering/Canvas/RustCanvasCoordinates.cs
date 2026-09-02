using System;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Discovery;

namespace RustCUIBuilder.Runtime.Rendering.Canvas
{
    /// <summary>
    /// Authoritative implementation of the Rust CUI Canvas Coordinate System.
    /// Handles bi-directional math across Screen (Editor GUI), Canvas (Virtual screen),
    /// and Rust CUI (Normalized anchors + Pixel offsets).
    /// </summary>
    public class RustCanvasCoordinates : ICanvasCoordinateSystem
    {
        public static readonly RustCanvasCoordinates Instance = new RustCanvasCoordinates();

        public Vector2 ScreenToCanvas(Vector2 screenPos, Rect viewportRect, Vector2 pan, float zoom)
        {
            if (zoom <= 0f) zoom = 1f;
            float x = (screenPos.x - (viewportRect.x + pan.x)) / zoom;
            float y = (screenPos.y - (viewportRect.y + pan.y)) / zoom;
            return new Vector2(x, y);
        }

        public Vector2 CanvasToScreen(Vector2 canvasPos, Rect viewportRect, Vector2 pan, float zoom)
        {
            float x = viewportRect.x + pan.x + canvasPos.x * zoom;
            float y = viewportRect.y + pan.y + canvasPos.y * zoom;
            return new Vector2(x, y);
        }

        public Rect ScreenToCanvas(Rect screenRect, Rect viewportRect, Vector2 pan, float zoom)
        {
            var pMin = ScreenToCanvas(new Vector2(screenRect.xMin, screenRect.yMin), viewportRect, pan, zoom);
            var pMax = ScreenToCanvas(new Vector2(screenRect.xMax, screenRect.yMax), viewportRect, pan, zoom);
            return Rect.MinMaxRect(pMin.x, pMin.y, pMax.x, pMax.y);
        }

        public Rect CanvasToScreen(Rect canvasRect, Rect viewportRect, Vector2 pan, float zoom)
        {
            var pMin = CanvasToScreen(new Vector2(canvasRect.xMin, canvasRect.yMin), viewportRect, pan, zoom);
            var pMax = CanvasToScreen(new Vector2(canvasRect.xMax, canvasRect.yMax), viewportRect, pan, zoom);
            return Rect.MinMaxRect(pMin.x, pMin.y, pMax.x, pMax.y);
        }

        public Vector2 RustToCanvas(Vector2 rustNormalized, float canvasWidth, float canvasHeight)
        {
            return new Vector2(rustNormalized.x * canvasWidth, (1.0f - rustNormalized.y) * canvasHeight);
        }

        public Vector2 CanvasToRust(Vector2 canvasPos, float canvasWidth, float canvasHeight)
        {
            float normX = canvasWidth > 0 ? canvasPos.x / canvasWidth : 0f;
            float normY = canvasHeight > 0 ? 1.0f - (canvasPos.y / canvasHeight) : 0f;
            return new Vector2(normX, normY);
        }

        public Rect GetElementCanvasRect(CuiElementNode elem, CuiDocument doc, float canvasWidth, float canvasHeight)
        {
            if (elem == null) return new Rect(0, 0, canvasWidth, canvasHeight);

            Rect parentRect = new Rect(0, 0, canvasWidth, canvasHeight);
            if (!string.IsNullOrEmpty(elem.Parent) && Array.IndexOf(RustAssetDiscovery.VerifiedLayers, elem.Parent) < 0)
            {
                var parentElem = doc?.FindByName(elem.Parent);
                if (parentElem != null && parentElem != elem)
                {
                    parentRect = GetElementCanvasRect(parentElem, doc, canvasWidth, canvasHeight);
                }
            }

            var rectComp = elem.GetComponent<CuiRectTransformComponent>() ?? new CuiRectTransformComponent();
            Vector2 anchorMin = RustCanvasScaler.ParseVector2(rectComp.AnchorMin, Vector2.zero);
            Vector2 anchorMax = RustCanvasScaler.ParseVector2(rectComp.AnchorMax, Vector2.one);
            Vector2 offsetMin = RustCanvasScaler.ParseVector2(rectComp.OffsetMin, Vector2.zero);
            Vector2 offsetMax = RustCanvasScaler.ParseVector2(rectComp.OffsetMax, Vector2.zero);

            float left = parentRect.x + parentRect.width * anchorMin.x + offsetMin.x;
            float right = parentRect.x + parentRect.width * anchorMax.x + offsetMax.x;
            float top = parentRect.y + parentRect.height * (1.0f - anchorMax.y) - offsetMax.y;
            float bottom = parentRect.y + parentRect.height * (1.0f - anchorMin.y) - offsetMin.y;

            return Rect.MinMaxRect(left, top, right, bottom);
        }

        public Rect GetElementScreenRect(CuiElementNode elem, CuiDocument doc, Rect viewportRect, Vector2 pan, float zoom, float canvasWidth, float canvasHeight)
        {
            var canvasRect = GetElementCanvasRect(elem, doc, canvasWidth, canvasHeight);
            return CanvasToScreen(canvasRect, viewportRect, pan, zoom);
        }

        public Vector2[] GetAnchorScreenPoints(CuiElementNode elem, CuiDocument doc, Rect viewportRect, Vector2 pan, float zoom, float canvasWidth, float canvasHeight)
        {
            if (elem == null) return new Vector2[0];

            Rect parentRect = new Rect(0, 0, canvasWidth, canvasHeight);
            if (!string.IsNullOrEmpty(elem.Parent) && Array.IndexOf(RustAssetDiscovery.VerifiedLayers, elem.Parent) < 0)
            {
                var parentElem = doc?.FindByName(elem.Parent);
                if (parentElem != null && parentElem != elem)
                {
                    parentRect = GetElementCanvasRect(parentElem, doc, canvasWidth, canvasHeight);
                }
            }

            var rectComp = elem.GetComponent<CuiRectTransformComponent>() ?? new CuiRectTransformComponent();
            Vector2 anchorMin = RustCanvasScaler.ParseVector2(rectComp.AnchorMin, Vector2.zero);
            Vector2 anchorMax = RustCanvasScaler.ParseVector2(rectComp.AnchorMax, Vector2.one);

            // Canvas space anchor corners
            Vector2 cNW = new Vector2(parentRect.x + parentRect.width * anchorMin.x, parentRect.y + parentRect.height * (1f - anchorMax.y));
            Vector2 cNE = new Vector2(parentRect.x + parentRect.width * anchorMax.x, parentRect.y + parentRect.height * (1f - anchorMax.y));
            Vector2 cSW = new Vector2(parentRect.x + parentRect.width * anchorMin.x, parentRect.y + parentRect.height * (1f - anchorMin.y));
            Vector2 cSE = new Vector2(parentRect.x + parentRect.width * anchorMax.x, parentRect.y + parentRect.height * (1f - anchorMin.y));

            return new[]
            {
                CanvasToScreen(cNW, viewportRect, pan, zoom),
                CanvasToScreen(cNE, viewportRect, pan, zoom),
                CanvasToScreen(cSW, viewportRect, pan, zoom),
                CanvasToScreen(cSE, viewportRect, pan, zoom)
            };
        }

        public Vector2 GetPivotScreenPoint(CuiElementNode elem, CuiDocument doc, Rect viewportRect, Vector2 pan, float zoom, float canvasWidth, float canvasHeight)
        {
            var canvasRect = GetElementCanvasRect(elem, doc, canvasWidth, canvasHeight);
            var rectComp = elem?.GetComponent<CuiRectTransformComponent>() ?? new CuiRectTransformComponent();
            Vector2 pivot = RustCanvasScaler.ParseVector2(rectComp.Pivot, new Vector2(0.5f, 0.5f));

            Vector2 cPivot = new Vector2(
                canvasRect.x + canvasRect.width * pivot.x,
                canvasRect.y + canvasRect.height * (1.0f - pivot.y)
            );

            return CanvasToScreen(cPivot, viewportRect, pan, zoom);
        }

        public void ApplyNewCanvasRectToElementOffsets(Rect newCanvasRect, CuiElementNode elem, CuiDocument doc, float canvasWidth, float canvasHeight)
        {
            if (elem == null) return;

            Rect parentRect = new Rect(0, 0, canvasWidth, canvasHeight);
            if (!string.IsNullOrEmpty(elem.Parent) && Array.IndexOf(RustAssetDiscovery.VerifiedLayers, elem.Parent) < 0)
            {
                var parentElem = doc?.FindByName(elem.Parent);
                if (parentElem != null && parentElem != elem)
                {
                    parentRect = GetElementCanvasRect(parentElem, doc, canvasWidth, canvasHeight);
                }
            }

            var rectComp = elem.GetComponent<CuiRectTransformComponent>();
            if (rectComp == null) return;

            Vector2 anchorMin = RustCanvasScaler.ParseVector2(rectComp.AnchorMin, Vector2.zero);
            Vector2 anchorMax = RustCanvasScaler.ParseVector2(rectComp.AnchorMax, Vector2.one);

            float newOffsetMinX = newCanvasRect.xMin - (parentRect.x + parentRect.width * anchorMin.x);
            float newOffsetMaxX = newCanvasRect.xMax - (parentRect.x + parentRect.width * anchorMax.x);
            float newOffsetMaxY = -(newCanvasRect.yMin - (parentRect.y + parentRect.height * (1.0f - anchorMax.y)));
            float newOffsetMinY = -(newCanvasRect.yMax - (parentRect.y + parentRect.height * (1.0f - anchorMin.y)));

            rectComp.OffsetMin = RustCanvasScaler.FormatVector2(new Vector2(newOffsetMinX, newOffsetMinY), "0.#");
            rectComp.OffsetMax = RustCanvasScaler.FormatVector2(new Vector2(newOffsetMaxX, newOffsetMaxY), "0.#");
        }
    }
}
