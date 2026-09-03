using System;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Canvas.Tools
{
    /// <summary>
    /// Interactive element rotation tool featuring a precision compass dial protractor gizmo,
    /// dedicated top stalk grab knob, 4 cardinal nodes, pivot bullseye, live angle sector arc,
    /// 15-degree magnetic snapping, floating HUD readout, and native RotateArrow cursor.
    /// </summary>
    public class RotateTool : ICanvasTool
    {
        public CanvasToolMode ToolMode => CanvasToolMode.Rotate;
        public string ToolName => "Rotate";

        private bool _isRotating;
        public bool IsRotating => _isRotating;
        public bool IsInteracting => _isRotating;

        private float _initialRotation;
        private float _initialAngle;
        private float _currentMouseAngle;
        private Vector2 _currentMousePos;
        private bool _isSnapped;
        private float _effectiveDelta;

        private bool _isHoveringRing;
        private bool _isHoveringTopHandle;
        private Vector2 _topHandlePos;

        // Custom GUI style for floating HUD
        private static GUIStyle _hudStyle;

        public void OnToolActivate()
        {
            _isRotating = false;
            _isHoveringRing = false;
            _isHoveringTopHandle = false;
        }

        public void OnToolDeactivate()
        {
            _isRotating = false;
            _isHoveringRing = false;
            _isHoveringTopHandle = false;
        }

        public bool ProcessEvent(
            Event currentEvent,
            Rect viewportRect,
            Vector2 pan,
            float zoom,
            float canvasWidth,
            float canvasHeight,
            CuiDocument doc,
            CanvasGuideSystem guideSystem,
            Action onModified,
            Action<string> onCommitUndo)
        {
            if (doc == null) return false;
            if (!viewportRect.Contains(currentEvent.mousePosition) && !_isRotating) return false;

            var primary = doc.PrimarySelectedElement;
            if (primary == null || primary.IsLocked || primary.IsHidden) return false;

            var rectComp = primary.GetComponent<CuiRectTransformComponent>();
            if (rectComp == null) return false;

            var coords = RustCanvasCoordinates.Instance;
            var pivotScreen = coords.GetPivotScreenPoint(primary, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);
            var elemScreenRect = coords.GetElementScreenRect(primary, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);

            float dialRadius = CalculateDialRadius(elemScreenRect);
            Vector2 topHandlePos = CalculateTopHandlePosition(elemScreenRect, pivotScreen, rectComp.Rotation, dialRadius);
            _topHandlePos = topHandlePos;

            // Update hover state
            float distToPivot = Vector2.Distance(currentEvent.mousePosition, pivotScreen);
            _isHoveringRing = Mathf.Abs(distToPivot - dialRadius) <= 16f;
            _isHoveringTopHandle = Vector2.Distance(currentEvent.mousePosition, topHandlePos) <= 14f;

            // 1. Mouse Down
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt)
            {
                bool hitRing = Mathf.Abs(distToPivot - dialRadius) <= 24f;
                bool hitHandle = Vector2.Distance(currentEvent.mousePosition, topHandlePos) <= 18f;
                bool hitElem = elemScreenRect.Contains(currentEvent.mousePosition);

                // Allow interaction if clicking dial ring, top handle, or inside/near element
                if (hitRing || hitHandle || hitElem || distToPivot <= dialRadius + 20f)
                {
                    var diff = currentEvent.mousePosition - pivotScreen;
                    _isRotating = true;
                    _initialRotation = rectComp.Rotation;
                    _initialAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                    _currentMouseAngle = _initialAngle;
                    _currentMousePos = currentEvent.mousePosition;
                    _isSnapped = false;
                    _effectiveDelta = 0f;

                    currentEvent.Use();
                    return true;
                }
            }

            // 2. Mouse Drag
            if (currentEvent.type == EventType.MouseDrag && _isRotating)
            {
                _currentMousePos = currentEvent.mousePosition;
                var diff = currentEvent.mousePosition - pivotScreen;
                float currentAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                _currentMouseAngle = currentAngle;

                float rawDelta = Mathf.DeltaAngle(_initialAngle, currentAngle);

                // Visual angle rotates clockwise as mouse moves clockwise.
                // Since GUI is rendered with -rotation, newRot = initialRotation - rawDelta.
                float newRot = _initialRotation - rawDelta;

                _isSnapped = false;
                if (currentEvent.shift)
                {
                    // Snap to exact 15-degree increments
                    newRot = Mathf.Round(newRot / 15f) * 15f;
                    _isSnapped = true;
                }
                else
                {
                    // Subtle magnetic snap near cardinal and 45-degree angles
                    float[] snapTargets = { 0f, 45f, 90f, 135f, 180f, -45f, -90f, -135f, -180f };
                    foreach (var target in snapTargets)
                    {
                        if (Mathf.Abs(Mathf.DeltaAngle(newRot, target)) < 2.5f)
                        {
                            newRot = target;
                            _isSnapped = true;
                            break;
                        }
                    }
                }

                _effectiveDelta = Mathf.DeltaAngle(_initialRotation, newRot);

                // Normalize rotation cleanly between -180 and 180
                float normalized = (float)Math.Round((((newRot + 180f) % 360f + 360f) % 360f) - 180f, 1);
                rectComp.Rotation = normalized;

                onModified?.Invoke();
                currentEvent.Use();
                return true;
            }

            // 3. Mouse Up
            if (currentEvent.type == EventType.MouseUp && _isRotating)
            {
                _isRotating = false;
                onCommitUndo?.Invoke($"Rotate {primary.Name} to {rectComp.Rotation:0.#}°");
                currentEvent.Use();
                return true;
            }

            // 4. Keyboard Shortcuts: Arrow keys nudge, Alt+R reset, Escape cancel
            if (currentEvent.type == EventType.KeyDown)
            {
                if (currentEvent.keyCode == KeyCode.Escape && _isRotating)
                {
                    _isRotating = false;
                    rectComp.Rotation = _initialRotation;
                    onModified?.Invoke();
                    currentEvent.Use();
                    return true;
                }

                // Alt+R resets rotation to 0
                if (currentEvent.keyCode == KeyCode.R && currentEvent.alt)
                {
                    rectComp.Rotation = 0f;
                    onModified?.Invoke();
                    onCommitUndo?.Invoke($"Reset Rotation on {primary.Name}");
                    currentEvent.Use();
                    return true;
                }

                // Left/Right arrows nudge rotation
                if (currentEvent.keyCode == KeyCode.LeftArrow && !currentEvent.alt)
                {
                    float step = currentEvent.shift ? 15f : 1f;
                    float n = Mathf.Round(rectComp.Rotation - step);
                    rectComp.Rotation = (float)Math.Round((((n + 180f) % 360f + 360f) % 360f) - 180f, 1);
                    onModified?.Invoke();
                    onCommitUndo?.Invoke($"Nudge Rotate {primary.Name}");
                    currentEvent.Use();
                    return true;
                }
                if (currentEvent.keyCode == KeyCode.RightArrow && !currentEvent.alt)
                {
                    float step = currentEvent.shift ? 15f : 1f;
                    float n = Mathf.Round(rectComp.Rotation + step);
                    rectComp.Rotation = (float)Math.Round((((n + 180f) % 360f + 360f) % 360f) - 180f, 1);
                    onModified?.Invoke();
                    onCommitUndo?.Invoke($"Nudge Rotate {primary.Name}");
                    currentEvent.Use();
                    return true;
                }
            }

            return false;
        }

        public void DrawToolOverlay(
            Rect viewportRect,
            Vector2 pan,
            float zoom,
            float canvasWidth,
            float canvasHeight,
            CuiDocument doc)
        {
            var primary = doc?.PrimarySelectedElement;
            if (primary == null || primary.IsHidden) return;

            var rectComp = primary.GetComponent<CuiRectTransformComponent>();
            if (rectComp == null) return;

            var coords = RustCanvasCoordinates.Instance;
            var pivotScreen = coords.GetPivotScreenPoint(primary, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);
            var elemScreenRect = coords.GetElementScreenRect(primary, doc, viewportRect, pan, zoom, canvasWidth, canvasHeight);

            float dialRadius = CalculateDialRadius(elemScreenRect);
            Vector2 topHandlePos = CalculateTopHandlePosition(elemScreenRect, pivotScreen, rectComp.Rotation, dialRadius);

            // Register RotateArrow cursor when hovering gizmo
            Rect cursorArea = new Rect(pivotScreen.x - dialRadius - 20f, pivotScreen.y - dialRadius - 20f, (dialRadius + 20f) * 2, (dialRadius + 20f) * 2);
            EditorGUIUtility.AddCursorRect(cursorArea, MouseCursor.RotateArrow);

            Handles.BeginGUI();

            // 1. Stalk line and Top Rotation Knob
            Vector2 elemTopCenter = RotatePointAroundPivot(new Vector2(elemScreenRect.center.x, elemScreenRect.yMin), pivotScreen, -rectComp.Rotation);
            Handles.color = new Color(1f, 0.75f, 0.2f, 0.75f);
            Handles.DrawDottedLine(elemTopCenter, topHandlePos, 3f);

            // Draw Top Handle Knob
            Color knobColor = (_isHoveringTopHandle || _isRotating) ? new Color(1f, 0.85f, 0.3f, 1f) : new Color(0.95f, 0.95f, 0.98f, 0.95f);
            Handles.color = new Color(0.1f, 0.12f, 0.16f, 0.9f);
            Handles.DrawSolidDisc(topHandlePos, Vector3.forward, 8.5f);
            Handles.color = knobColor;
            Handles.DrawSolidDisc(topHandlePos, Vector3.forward, 6.5f);
            Handles.color = new Color(0.15f, 0.18f, 0.25f, 1f);
            Handles.DrawSolidDisc(topHandlePos, Vector3.forward, 2.5f);

            // 2. Dual-Ring Compass Track
            Color ringColor = (_isHoveringRing || _isRotating)
                ? new Color(1f, 0.75f, 0.2f, 0.95f)
                : new Color(1f, 0.7f, 0.2f, 0.65f);

            // Outer ring
            Handles.color = ringColor;
            Handles.DrawWireDisc(pivotScreen, Vector3.forward, dialRadius);

            // Inner guide ring
            Handles.color = new Color(1f, 0.75f, 0.2f, 0.2f);
            Handles.DrawWireDisc(pivotScreen, Vector3.forward, dialRadius - 14f);

            // Subtle translucent track fill
            Handles.color = new Color(1f, 0.7f, 0.2f, _isRotating ? 0.08f : 0.03f);
            Handles.DrawSolidArc(pivotScreen, Vector3.forward, Vector3.right, 360f, dialRadius);

            // 3. Protractor Graduation Ticks every 15 degrees
            for (int angle = 0; angle < 360; angle += 15)
            {
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                bool isCardinal = (angle % 90 == 0);
                bool isIntercardinal = (angle % 45 == 0 && !isCardinal);

                if (isCardinal)
                {
                    // Major cardinal ticks (0, 90, 180, 270)
                    Handles.color = new Color(1f, 0.88f, 0.35f, 0.95f);
                    Vector2 pStart = pivotScreen + dir * (dialRadius - 14f);
                    Vector2 pEnd = pivotScreen + dir * (dialRadius + 7f);
                    Handles.DrawLine(pStart, pEnd);

                    // Tiny diamond or dot at cardinal tip
                    Handles.DrawSolidDisc(pEnd, Vector3.forward, 2f);
                }
                else if (isIntercardinal)
                {
                    // Medium intercardinal ticks (45, 135, 225, 315)
                    Handles.color = new Color(1f, 0.75f, 0.25f, 0.7f);
                    Vector2 pStart = pivotScreen + dir * (dialRadius - 10f);
                    Vector2 pEnd = pivotScreen + dir * (dialRadius + 4f);
                    Handles.DrawLine(pStart, pEnd);
                }
                else
                {
                    // Minor 15-degree sub-division ticks
                    Handles.color = new Color(1f, 0.75f, 0.2f, 0.35f);
                    Vector2 pStart = pivotScreen + dir * (dialRadius - 6f);
                    Vector2 pEnd = pivotScreen + dir * dialRadius;
                    Handles.DrawLine(pStart, pEnd);
                }
            }

            // 4. 4 Cardinal Grab Knobs along the Ring
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f - rectComp.Rotation) * Mathf.Deg2Rad;
                Vector2 knobPos = pivotScreen + new Vector2(Mathf.Sin(a), -Mathf.Cos(a)) * dialRadius;
                Handles.color = new Color(0.1f, 0.12f, 0.16f, 0.85f);
                Handles.DrawSolidDisc(knobPos, Vector3.forward, 4.5f);
                Handles.color = new Color(1f, 0.8f, 0.3f, 0.9f);
                Handles.DrawSolidDisc(knobPos, Vector3.forward, 3f);
            }

            // 5. Center Pivot Bullseye
            Handles.color = new Color(0.1f, 0.12f, 0.16f, 0.9f);
            Handles.DrawSolidDisc(pivotScreen, Vector3.forward, 7f);
            Handles.color = new Color(0.2f, 0.85f, 1f, 0.95f);
            Handles.DrawWireDisc(pivotScreen, Vector3.forward, 6f);
            // Crosshairs
            Handles.DrawLine(pivotScreen + new Vector2(-8f, 0), pivotScreen + new Vector2(8f, 0));
            Handles.DrawLine(pivotScreen + new Vector2(0, -8f), pivotScreen + new Vector2(0, 8f));
            // Center white core
            Handles.color = Color.white;
            Handles.DrawSolidDisc(pivotScreen, Vector3.forward, 2f);

            // 6. Active Live Drag Feedback (Arc, Rays, HUD)
            if (_isRotating)
            {
                float radStart = _initialAngle * Mathf.Deg2Rad;
                Vector2 startDir = new Vector2(Mathf.Cos(radStart), Mathf.Sin(radStart));

                float radCurr = _currentMouseAngle * Mathf.Deg2Rad;
                Vector2 currDir = new Vector2(Mathf.Cos(radCurr), Mathf.Sin(radCurr));

                // Dashed ray from pivot to start point
                Handles.color = new Color(1f, 1f, 1f, 0.45f);
                Handles.DrawDottedLine(pivotScreen, pivotScreen + startDir * (dialRadius + 12f), 3f);

                // Dynamic filled swept arc sector
                float sweptAngle = Mathf.DeltaAngle(_initialAngle, _currentMouseAngle);
                Color arcColor = _isSnapped
                    ? new Color(0f, 1f, 0.85f, 0.28f)
                    : new Color(1f, 0.75f, 0.2f, 0.22f);
                Handles.color = arcColor;
                Handles.DrawSolidArc(pivotScreen, Vector3.forward, startDir, sweptAngle, dialRadius);

                // Current mouse ray line
                Color rayColor = _isSnapped
                    ? new Color(0f, 1f, 0.85f, 1f)
                    : new Color(1f, 0.85f, 0.35f, 0.95f);
                Handles.color = rayColor;
                Handles.DrawLine(pivotScreen, pivotScreen + currDir * (dialRadius + 16f));
                Handles.DrawSolidDisc(pivotScreen + currDir * (dialRadius + 16f), Vector3.forward, 4.5f);
            }

            Handles.EndGUI();

            // 7. Floating HUD Angle Readout Badge
            if (_isRotating)
            {
                DrawAngleHudBadge(pivotScreen, dialRadius, rectComp.Rotation, _effectiveDelta, _isSnapped, viewportRect);
            }
        }

        private static void DrawAngleHudBadge(Vector2 pivotScreen, float dialRadius, float currentRot, float deltaAngle, bool isSnapped, Rect viewportRect)
        {
            EnsureStyles();

            string sign = deltaAngle >= 0 ? "+" : "";
            string snapText = isSnapped ? "  <color=#00FFCC>• SNAP 15°</color>" : "  <color=#AAAAAA>[Shift: Snap]</color>";
            string text = $"<b>{currentRot:0.0}°</b>  <color=#FFD54F>({sign}{deltaAngle:0.0}°)</color>{snapText}";

            var content = new GUIContent(text);
            Vector2 size = _hudStyle.CalcSize(content) + new Vector2(16f, 8f);

            // Place HUD above the gizmo
            Vector2 badgePos = new Vector2(pivotScreen.x - size.x * 0.5f, pivotScreen.y - dialRadius - size.y - 18f);

            // Keep within viewport
            badgePos.x = Mathf.Clamp(badgePos.x, viewportRect.xMin + 8f, viewportRect.xMax - size.x - 8f);
            badgePos.y = Mathf.Clamp(badgePos.y, viewportRect.yMin + 8f, viewportRect.yMax - size.y - 8f);

            Rect badgeRect = new Rect(badgePos.x, badgePos.y, size.x, size.y);

            // Dark pill container with accent border
            Color bg = new Color(0.08f, 0.10f, 0.14f, 0.94f);
            Color border = isSnapped ? new Color(0f, 1f, 0.85f, 0.8f) : new Color(1f, 0.75f, 0.2f, 0.7f);

            EditorGUI.DrawRect(badgeRect, bg);

            // 1px border
            Handles.BeginGUI();
            Handles.color = border;
            Handles.DrawPolyLine(
                new Vector3(badgeRect.xMin, badgeRect.yMin, 0),
                new Vector3(badgeRect.xMax, badgeRect.yMin, 0),
                new Vector3(badgeRect.xMax, badgeRect.yMax, 0),
                new Vector3(badgeRect.xMin, badgeRect.yMax, 0),
                new Vector3(badgeRect.xMin, badgeRect.yMin, 0)
            );
            Handles.EndGUI();

            GUI.Label(badgeRect, text, _hudStyle);
        }

        private static void EnsureStyles()
        {
            if (_hudStyle == null)
            {
                _hudStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true,
                    normal = { textColor = Color.white }
                };
            }
        }

        private static float CalculateDialRadius(Rect elemScreenRect)
        {
            float maxDim = Mathf.Max(elemScreenRect.width, elemScreenRect.height);
            return Mathf.Max(60f, maxDim * 0.55f + 24f);
        }

        private static Vector2 CalculateTopHandlePosition(Rect elemScreenRect, Vector2 pivotScreen, float rotation, float dialRadius)
        {
            Vector2 unrotatedTopCenter = new Vector2(elemScreenRect.center.x, elemScreenRect.yMin);
            float distToTop = Vector2.Distance(pivotScreen, unrotatedTopCenter);
            float stalkDistance = Mathf.Max(dialRadius, distToTop + 26f);

            // Direction upwards in screen space from pivot
            Vector2 unrotatedPos = new Vector2(pivotScreen.x, pivotScreen.y - stalkDistance);
            return RotatePointAroundPivot(unrotatedPos, pivotScreen, -rotation);
        }

        public static Vector2 RotatePointAroundPivot(Vector2 point, Vector2 pivot, float angleDegrees)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            Vector2 diff = point - pivot;
            return new Vector2(
                cos * diff.x - sin * diff.y + pivot.x,
                sin * diff.x + cos * diff.y + pivot.y
            );
        }
    }
}
