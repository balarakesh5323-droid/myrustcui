using System;
using System.Collections.Generic;
using UnityEngine;

namespace RustCUIBuilder.Editor.Canvas
{
    public enum GuideOrientation
    {
        Horizontal,
        Vertical
    }

    [Serializable]
    public class CanvasGuide
    {
        public string Id = Guid.NewGuid().ToString("N");
        public GuideOrientation Orientation;
        public float CanvasPosition; // In canvas workspace pixels
    }

    /// <summary>
    /// Interactive draggable guide system and smart snap engine.
    /// Snaps coordinates to Grid, Draggable Guides, and Sibling element bounds.
    /// </summary>
    public class CanvasGuideSystem
    {
        public List<CanvasGuide> Guides { get; set; } = new List<CanvasGuide>();
        public bool SnapToGrid { get; set; } = true;
        public bool SnapToGuides { get; set; } = true;
        public bool SnapToElements { get; set; } = true;
        public float GridSize { get; set; } = 16f;
        public float SnapTolerancePixels { get; set; } = 8f;

        public void AddGuide(GuideOrientation orientation, float canvasPos)
        {
            Guides.Add(new CanvasGuide { Orientation = orientation, CanvasPosition = canvasPos });
        }

        public void RemoveGuide(string id)
        {
            Guides.RemoveAll(g => g.Id == id);
        }

        public void ClearGuides()
        {
            Guides.Clear();
        }

        public Vector2 SnapCanvasPoint(Vector2 canvasPos, float zoom, IEnumerable<Rect> siblingCanvasRects = null)
        {
            float snapDist = SnapTolerancePixels / (zoom > 0 ? zoom : 1f);
            float snappedX = canvasPos.x;
            float snappedY = canvasPos.y;

            // 1. Grid Snap
            if (SnapToGrid && GridSize > 0)
            {
                float gx = Mathf.Round(canvasPos.x / GridSize) * GridSize;
                float gy = Mathf.Round(canvasPos.y / GridSize) * GridSize;
                if (Mathf.Abs(canvasPos.x - gx) <= snapDist) snappedX = gx;
                if (Mathf.Abs(canvasPos.y - gy) <= snapDist) snappedY = gy;
            }

            // 2. Guide Snap
            if (SnapToGuides)
            {
                foreach (var g in Guides)
                {
                    if (g.Orientation == GuideOrientation.Vertical)
                    {
                        if (Mathf.Abs(canvasPos.x - g.CanvasPosition) <= snapDist) snappedX = g.CanvasPosition;
                    }
                    else
                    {
                        if (Mathf.Abs(canvasPos.y - g.CanvasPosition) <= snapDist) snappedY = g.CanvasPosition;
                    }
                }
            }

            // 3. Sibling Bounds Snap
            if (SnapToElements && siblingCanvasRects != null)
            {
                foreach (var sib in siblingCanvasRects)
                {
                    // Snap X to left, center, right
                    if (Mathf.Abs(canvasPos.x - sib.xMin) <= snapDist) snappedX = sib.xMin;
                    else if (Mathf.Abs(canvasPos.x - sib.center.x) <= snapDist) snappedX = sib.center.x;
                    else if (Mathf.Abs(canvasPos.x - sib.xMax) <= snapDist) snappedX = sib.xMax;

                    // Snap Y to top, center, bottom
                    if (Mathf.Abs(canvasPos.y - sib.yMin) <= snapDist) snappedY = sib.yMin;
                    else if (Mathf.Abs(canvasPos.y - sib.center.y) <= snapDist) snappedY = sib.center.y;
                    else if (Mathf.Abs(canvasPos.y - sib.yMax) <= snapDist) snappedY = sib.yMax;
                }
            }

            return new Vector2(snappedX, snappedY);
        }
    }
}
