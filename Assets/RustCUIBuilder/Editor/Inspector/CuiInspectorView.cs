using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Core.Registry;
using RustCUIBuilder.Runtime.Discovery;
using RustCUIBuilder.Runtime.Rendering.Canvas;

namespace RustCUIBuilder.Editor.Inspector
{
    /// <summary>
    /// Exhaustive typed inspector for Rust CUI Builder.
    /// Exposes 100% of all tweakable properties for all Oxide/Rust CUI components with visual anchor presets,
    /// sprite/material pickers, color pickers, font selectors, enum dropdowns, and transition states.
    /// Compatible with standard Oxide CUI and 0xF CUI Library specifications.
    /// </summary>
    public class CuiInspectorView
    {
        private Vector2 _scrollPos;

        public void Draw(Rect rect, CuiDocument doc, Action onModified)
        {
            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginVertical("box");

            var elem = doc?.PrimarySelectedElement;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Property Inspector", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            if (elem == null)
            {
                EditorGUILayout.HelpBox("Select an element in the Hierarchy or Canvas to edit its properties.", MessageType.Info);
                EditorGUILayout.EndVertical();
                GUILayout.EndArea();
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // 1. Element Header Info
            DrawElementHeader(elem, doc, onModified);

            EditorGUILayout.Space(6);

            // 2. Components List
            for (int i = 0; i < elem.Components.Count; i++)
            {
                var comp = elem.Components[i];
                DrawComponentBox(elem, comp, i, onModified);
            }

            // 3. Add Component Button
            EditorGUILayout.Space(8);
            DrawAddComponentButton(elem, onModified);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawElementHeader(CuiElementNode elem, CuiDocument doc, Action onModified)
        {
            EditorGUILayout.LabelField("Element Info", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            string newName = EditorGUILayout.TextField("Name", elem.Name);
            if (newName != elem.Name)
            {
                elem.Name = newName;
                doc.EnsureUniqueName(elem);
            }

            var parentOptions = RustAssetDiscovery.VerifiedLayers
                .Concat(doc.Elements.Where(e => e.Id != elem.Id).Select(e => e.Name))
                .Distinct()
                .ToArray();

            int currentParentIdx = Array.IndexOf(parentOptions, elem.Parent);
            if (currentParentIdx < 0) currentParentIdx = 0;

            int newParentIdx = EditorGUILayout.Popup("Parent", currentParentIdx, parentOptions);
            if (newParentIdx >= 0 && newParentIdx < parentOptions.Length)
            {
                elem.Parent = parentOptions[newParentIdx];
            }

            elem.DestroyUi = EditorGUILayout.TextField("Destroy UI", elem.DestroyUi);
            elem.FadeOut = EditorGUILayout.FloatField("Fade Out (sec)", elem.FadeOut);
            elem.Update = EditorGUILayout.Toggle("Incremental Update", elem.Update);

            if (EditorGUI.EndChangeCheck())
            {
                onModified?.Invoke();
            }
        }

        private void DrawComponentBox(CuiElementNode elem, ICuiComponent comp, int index, Action onModified)
        {
            EditorGUILayout.BeginVertical("helpBox");

            // Component Header Bar
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(comp.Type, EditorStyles.boldLabel);

            if (comp is ICuiEnableable enableable)
            {
                bool isEn = enableable.Enabled ?? true;
                bool newEn = EditorGUILayout.Toggle(isEn, GUILayout.Width(20));
                if (newEn != isEn) enableable.Enabled = newEn;
            }

            if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(20)))
            {
                elem.Components.RemoveAt(index);
                onModified?.Invoke();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();

            switch (comp)
            {
                case CuiRectTransformComponent rect:
                    DrawRectTransformInspector(rect);
                    break;
                case CuiTextComponent txt:
                    DrawTextInspector(txt);
                    break;
                case CuiImageComponent img:
                    DrawImageInspector(img);
                    break;
                case CuiRawImageComponent raw:
                    DrawRawImageInspector(raw);
                    break;
                case CuiButtonComponent btn:
                    DrawButtonInspector(btn);
                    break;
                case CuiInputFieldComponent input:
                    DrawInputFieldInspector(input);
                    break;
                case CuiCountdownComponent count:
                    DrawCountdownInspector(count);
                    break;
                case CuiOutlineComponent outline:
                    DrawOutlineInspector(outline);
                    break;
                case CuiScrollViewComponent scroll:
                    DrawScrollViewInspector(scroll);
                    break;
                case CuiCanvasGroupComponent cg:
                    DrawCanvasGroupInspector(cg);
                    break;
                case CuiTooltipComponent tooltip:
                    DrawTooltipInspector(tooltip);
                    break;
                case CuiDraggableComponent drag:
                    DrawDraggableInspector(drag);
                    break;
                case CuiNeedsCursorComponent:
                    EditorGUILayout.HelpBox("NeedsCursor: Unlocks mouse cursor for interactive modal UI elements.", MessageType.None);
                    break;
                case CuiNeedsKeyboardComponent:
                    EditorGUILayout.HelpBox("NeedsKeyboard: Captures keyboard focus for chat and text input fields.", MessageType.None);
                    break;
                case CuiMaskComponent mask:
                    mask.ShowMaskGraphic = EditorGUILayout.Toggle("Show Mask Graphic", mask.ShowMaskGraphic ?? false);
                    break;
                case CuiHorizontalLayoutGroupComponent hlg:
                    DrawLayoutGroupInspector(hlg);
                    break;
                case CuiVerticalLayoutGroupComponent vlg:
                    DrawLayoutGroupInspector(vlg);
                    break;
                case CuiGridLayoutGroupComponent glg:
                    DrawGridLayoutInspector(glg);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                onModified?.Invoke();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRectTransformInspector(CuiRectTransformComponent rect)
        {
            EditorGUILayout.LabelField("Anchor Presets", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Stretch", EditorStyles.miniButton)) SetAnchorPreset(rect, "0 0", "1 1", "0 0", "0 0");
            if (GUILayout.Button("Center", EditorStyles.miniButton)) SetAnchorPreset(rect, "0.5 0.5", "0.5 0.5", "-100 -50", "100 50");
            if (GUILayout.Button("Top", EditorStyles.miniButton)) SetAnchorPreset(rect, "0 1", "1 1", "0 -60", "0 0");
            if (GUILayout.Button("Bottom", EditorStyles.miniButton)) SetAnchorPreset(rect, "0 0", "1 0", "0 0", "0 60");
            if (GUILayout.Button("Left", EditorStyles.miniButton)) SetAnchorPreset(rect, "0 0", "0 1", "0 0", "60 0");
            if (GUILayout.Button("Right", EditorStyles.miniButton)) SetAnchorPreset(rect, "1 0", "1 1", "-60 0", "0 0");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Pixel Offsets & Precision Size", EditorStyles.miniBoldLabel);
            var oMin = RustCanvasScaler.ParseVector2(rect.OffsetMin, Vector2.zero);
            var oMax = RustCanvasScaler.ParseVector2(rect.OffsetMax, Vector2.zero);

            float width = Mathf.Max(0, oMax.x - oMin.x);
            float height = Mathf.Max(0, oMax.y - oMin.y);

            EditorGUILayout.BeginHorizontal();
            float newX = EditorGUILayout.FloatField("Offset X", oMin.x);
            if (GUILayout.Button("-1", EditorStyles.miniButton, GUILayout.Width(24))) newX -= 1;
            if (GUILayout.Button("+1", EditorStyles.miniButton, GUILayout.Width(24))) newX += 1;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            float newY = EditorGUILayout.FloatField("Offset Y", oMin.y);
            if (GUILayout.Button("-1", EditorStyles.miniButton, GUILayout.Width(24))) newY -= 1;
            if (GUILayout.Button("+1", EditorStyles.miniButton, GUILayout.Width(24))) newY += 1;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            float newW = EditorGUILayout.FloatField("Width (px)", width > 0 ? width : 100);
            if (GUILayout.Button("-10", EditorStyles.miniButton, GUILayout.Width(28))) newW = Mathf.Max(10, newW - 10);
            if (GUILayout.Button("+10", EditorStyles.miniButton, GUILayout.Width(28))) newW += 10;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            float newH = EditorGUILayout.FloatField("Height (px)", height > 0 ? height : 50);
            if (GUILayout.Button("-10", EditorStyles.miniButton, GUILayout.Width(28))) newH = Mathf.Max(10, newH - 10);
            if (GUILayout.Button("+10", EditorStyles.miniButton, GUILayout.Width(28))) newH += 10;
            EditorGUILayout.EndHorizontal();

            if (newX != oMin.x || newY != oMin.y || newW != width || newH != height)
            {
                rect.OffsetMin = RustCanvasScaler.FormatVector2(new Vector2(newX, newY), "0.#");
                rect.OffsetMax = RustCanvasScaler.FormatVector2(new Vector2(newX + newW, newY + newH), "0.#");
            }

            EditorGUILayout.Space(2);
            rect.AnchorMin = EditorGUILayout.TextField("Anchor Min", rect.AnchorMin);
            rect.AnchorMax = EditorGUILayout.TextField("Anchor Max", rect.AnchorMax);
            rect.OffsetMin = EditorGUILayout.TextField("Raw Offset Min", rect.OffsetMin);
            rect.OffsetMax = EditorGUILayout.TextField("Raw Offset Max", rect.OffsetMax);
            rect.Pivot = EditorGUILayout.TextField("Pivot", rect.Pivot);
            rect.Rotation = EditorGUILayout.Slider("Rotation (deg)", rect.Rotation, -180f, 180f);

            if (!string.IsNullOrEmpty(rect.SetParent))
            {
                rect.SetParent = EditorGUILayout.TextField("Set Parent", rect.SetParent);
            }
            if (rect.SetTransformIndex >= 0)
            {
                rect.SetTransformIndex = EditorGUILayout.IntField("Transform Index", rect.SetTransformIndex);
            }
        }

        private void SetAnchorPreset(CuiRectTransformComponent rect, string aMin, string aMax, string oMin, string oMax)
        {
            rect.AnchorMin = aMin;
            rect.AnchorMax = aMax;
            rect.OffsetMin = oMin;
            rect.OffsetMax = oMax;
        }

        private void DrawTextInspector(CuiTextComponent txt)
        {
            EditorGUILayout.LabelField("Text Content (RichText Supported)");
            txt.Text = EditorGUILayout.TextArea(txt.Text, GUILayout.Height(45));
            txt.FontSize = EditorGUILayout.IntSlider("Font Size", txt.FontSize, 6, 96);

            // Font Selector Dropdown
            var fonts = RustAssetDiscovery.VerifiedFonts;
            int fontIdx = Array.IndexOf(fonts, txt.Font);
            if (fontIdx < 0) fontIdx = 0;
            int newFontIdx = EditorGUILayout.Popup("Font", fontIdx, fonts);
            txt.Font = fonts[newFontIdx];

            txt.Align = (TextAnchor)EditorGUILayout.EnumPopup("Alignment", txt.Align);
            txt.Color = DrawCuiColorField("Color", txt.Color, Color.white);
            txt.VerticalOverflow = (VerticalWrapMode)EditorGUILayout.EnumPopup("Vertical Overflow", txt.VerticalOverflow);
            txt.FadeIn = EditorGUILayout.FloatField("Fade In (sec)", txt.FadeIn);

            if (txt.BlocksRaycast.HasValue)
            {
                txt.BlocksRaycast = EditorGUILayout.Toggle("Blocks Raycast", txt.BlocksRaycast.Value);
            }
        }

        private void DrawImageInspector(CuiImageComponent img)
        {
            // Sprite Asset Picker
            EditorGUILayout.BeginHorizontal();
            img.Sprite = EditorGUILayout.TextField("Sprite Asset", img.Sprite);
            if (GUILayout.Button("▼", GUILayout.Width(22)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("None / Blank"), string.IsNullOrEmpty(img.Sprite), () => img.Sprite = "");
                foreach (var spr in RustAssetDiscovery.VerifiedSprites)
                {
                    menu.AddItem(new GUIContent(spr), img.Sprite == spr, () => img.Sprite = spr);
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

            // Material Picker
            EditorGUILayout.BeginHorizontal();
            img.Material = EditorGUILayout.TextField("Material", img.Material);
            if (GUILayout.Button("▼", GUILayout.Width(22)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("None / Standard"), string.IsNullOrEmpty(img.Material), () => img.Material = "");
                foreach (var mat in RustAssetDiscovery.VerifiedMaterials)
                {
                    menu.AddItem(new GUIContent(mat), img.Material == mat, () => img.Material = mat);
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

            img.Color = DrawCuiColorField("Color", img.Color, Color.white);
            img.ImageType = (UnityEngine.UI.Image.Type)EditorGUILayout.EnumPopup("Image Type", img.ImageType);
            img.FillCenter = EditorGUILayout.Toggle("Fill Center", img.FillCenter ?? true);

            // Item ID & Skin ID
            img.ItemId = EditorGUILayout.IntField("Rust Item ID", img.ItemId);
            if (img.ItemId != 0)
            {
                var itemMeta = RustAssetDiscovery.FindItemById(img.ItemId);
                if (itemMeta != null)
                {
                    EditorGUILayout.LabelField($"Item: {itemMeta.displayName} ({itemMeta.shortname})", EditorStyles.miniLabel);
                }
            }
            img.SkinId = (ulong)EditorGUILayout.LongField("Skin ID", (long)img.SkinId);

            // ImageLibrary PNG Id
            img.Png = EditorGUILayout.TextField("ImageLibrary PNG ID", img.Png);
            if (!string.IsNullOrEmpty(img.Slice))
            {
                img.Slice = EditorGUILayout.TextField("Border Slice (l t r b)", img.Slice);
            }

            img.PixelsPerUnitMultiplier = EditorGUILayout.FloatField("Pixels Per Unit", img.PixelsPerUnitMultiplier);
            img.FadeIn = EditorGUILayout.FloatField("Fade In (sec)", img.FadeIn);

            if (img.BlocksRaycast.HasValue)
            {
                img.BlocksRaycast = EditorGUILayout.Toggle("Blocks Raycast", img.BlocksRaycast.Value);
            }
        }

        private void DrawRawImageInspector(CuiRawImageComponent raw)
        {
            raw.Url = EditorGUILayout.TextField("Web Image URL", raw.Url);
            raw.SteamId = EditorGUILayout.TextField("Steam Avatar ID", raw.SteamId);
            raw.Png = EditorGUILayout.TextField("ImageLibrary PNG ID", raw.Png);
            raw.Sprite = EditorGUILayout.TextField("Sprite Asset", raw.Sprite);

            EditorGUILayout.BeginHorizontal();
            raw.Material = EditorGUILayout.TextField("Material", raw.Material);
            if (GUILayout.Button("▼", GUILayout.Width(22)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("None / Standard"), string.IsNullOrEmpty(raw.Material), () => raw.Material = "");
                foreach (var mat in RustAssetDiscovery.VerifiedMaterials)
                {
                    menu.AddItem(new GUIContent(mat), raw.Material == mat, () => raw.Material = mat);
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

            raw.Color = DrawCuiColorField("Color", raw.Color, Color.white);
            raw.FadeIn = EditorGUILayout.FloatField("Fade In (sec)", raw.FadeIn);

            if (raw.BlocksRaycast.HasValue)
            {
                raw.BlocksRaycast = EditorGUILayout.Toggle("Blocks Raycast", raw.BlocksRaycast.Value);
            }
        }

        private void DrawButtonInspector(CuiButtonComponent btn)
        {
            btn.Command = EditorGUILayout.TextField("Console Command", btn.Command);
            btn.Close = EditorGUILayout.TextField("Close Panel", btn.Close);

            // Sprite Asset Picker
            EditorGUILayout.BeginHorizontal();
            btn.Sprite = EditorGUILayout.TextField("Sprite Asset", btn.Sprite);
            if (GUILayout.Button("▼", GUILayout.Width(22)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("None / Standard"), string.IsNullOrEmpty(btn.Sprite), () => btn.Sprite = "");
                foreach (var spr in RustAssetDiscovery.VerifiedSprites)
                {
                    menu.AddItem(new GUIContent(spr), btn.Sprite == spr, () => btn.Sprite = spr);
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

            // Material Picker
            EditorGUILayout.BeginHorizontal();
            btn.Material = EditorGUILayout.TextField("Material", btn.Material);
            if (GUILayout.Button("▼", GUILayout.Width(22)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("None / Standard"), string.IsNullOrEmpty(btn.Material), () => btn.Material = "");
                foreach (var mat in RustAssetDiscovery.VerifiedMaterials)
                {
                    menu.AddItem(new GUIContent(mat), btn.Material == mat, () => btn.Material = mat);
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

            btn.Color = DrawCuiColorField("Base Color", btn.Color, new Color(0.2f, 0.6f, 0.3f, 1f));
            btn.ImageType = (UnityEngine.UI.Image.Type)EditorGUILayout.EnumPopup("Image Type", btn.ImageType);

            // Color Transitions
            if (!string.IsNullOrEmpty(btn.NormalColor)) btn.NormalColor = DrawCuiColorField("Normal Color", btn.NormalColor, Color.white);
            if (!string.IsNullOrEmpty(btn.HighlightedColor)) btn.HighlightedColor = DrawCuiColorField("Highlight Color", btn.HighlightedColor, Color.white);
            if (!string.IsNullOrEmpty(btn.PressedColor)) btn.PressedColor = DrawCuiColorField("Pressed Color", btn.PressedColor, Color.white);
            if (!string.IsNullOrEmpty(btn.DisabledColor)) btn.DisabledColor = DrawCuiColorField("Disabled Color", btn.DisabledColor, Color.gray);

            btn.ColorMultiplier = EditorGUILayout.FloatField("Color Multiplier", btn.ColorMultiplier);
            btn.FadeDuration = EditorGUILayout.FloatField("Fade Duration", btn.FadeDuration ?? 0.1f);
            btn.FadeIn = EditorGUILayout.FloatField("Fade In (sec)", btn.FadeIn);

            btn.Interactable = EditorGUILayout.Toggle("Interactable", btn.Interactable ?? true);
            if (btn.BlocksRaycast.HasValue)
            {
                btn.BlocksRaycast = EditorGUILayout.Toggle("Blocks Raycast", btn.BlocksRaycast.Value);
            }
        }

        private void DrawInputFieldInspector(CuiInputFieldComponent input)
        {
            input.Text = EditorGUILayout.TextField("Initial Text", input.Text);
            input.Command = EditorGUILayout.TextField("Submit Command", input.Command);
            input.FontSize = EditorGUILayout.IntSlider("Font Size", input.FontSize, 6, 48);

            var fonts = RustAssetDiscovery.VerifiedFonts;
            int fontIdx = Array.IndexOf(fonts, input.Font);
            if (fontIdx < 0) fontIdx = 0;
            int newFontIdx = EditorGUILayout.Popup("Font", fontIdx, fonts);
            input.Font = fonts[newFontIdx];

            input.Align = (TextAnchor)EditorGUILayout.EnumPopup("Alignment", input.Align);
            input.Color = DrawCuiColorField("Color", input.Color, Color.white);
            input.CharsLimit = EditorGUILayout.IntField("Character Limit", input.CharsLimit);
            input.LineType = (InputField.LineType)EditorGUILayout.EnumPopup("Line Type", input.LineType);

            input.IsPassword = EditorGUILayout.Toggle("Password Mode", input.IsPassword);
            input.NeedsKeyboard = EditorGUILayout.Toggle("Needs Keyboard", input.NeedsKeyboard);
            input.HudMenuInput = EditorGUILayout.Toggle("HUD Menu Input", input.HudMenuInput);
            input.ReadOnly = EditorGUILayout.Toggle("Read Only", input.ReadOnly);
            input.Autofocus = EditorGUILayout.Toggle("Autofocus", input.Autofocus);
            input.Interactable = EditorGUILayout.Toggle("Interactable", input.Interactable ?? true);
            input.FadeIn = EditorGUILayout.FloatField("Fade In (sec)", input.FadeIn);
        }

        private void DrawCountdownInspector(CuiCountdownComponent count)
        {
            count.StartTime = EditorGUILayout.FloatField("Start Time (sec)", count.StartTime);
            count.EndTime = EditorGUILayout.FloatField("End Time (sec)", count.EndTime);
            count.Step = EditorGUILayout.FloatField("Step", count.Step);
            count.Interval = EditorGUILayout.FloatField("Interval", count.Interval);
            count.TimerFormat = (CuiTimerFormat)EditorGUILayout.EnumPopup("Timer Format", count.TimerFormat);
            count.NumberFormat = EditorGUILayout.TextField("Number Format", count.NumberFormat);
            count.Command = EditorGUILayout.TextField("End Command", count.Command);
            count.DestroyIfDone = EditorGUILayout.Toggle("Destroy When Done", count.DestroyIfDone);
            count.FadeIn = EditorGUILayout.FloatField("Fade In (sec)", count.FadeIn);
        }

        private void DrawOutlineInspector(CuiOutlineComponent outline)
        {
            outline.Color = DrawCuiColorField("Outline Color", outline.Color, Color.black);
            outline.Distance = EditorGUILayout.TextField("Distance (px)", outline.Distance);
            outline.UseGraphicAlpha = EditorGUILayout.Toggle("Use Graphic Alpha", outline.UseGraphicAlpha);
        }

        private void DrawScrollViewInspector(CuiScrollViewComponent scroll)
        {
            scroll.Horizontal = EditorGUILayout.Toggle("Horizontal Scroll", scroll.Horizontal);
            scroll.Vertical = EditorGUILayout.Toggle("Vertical Scroll", scroll.Vertical);
            scroll.MovementType = (UnityEngine.UI.ScrollRect.MovementType)EditorGUILayout.EnumPopup("Movement Type", scroll.MovementType);
            scroll.Elasticity = EditorGUILayout.Slider("Elasticity", scroll.Elasticity, 0f, 1f);
            scroll.Inertia = EditorGUILayout.Toggle("Inertia", scroll.Inertia);
            scroll.DecelerationRate = EditorGUILayout.Slider("Deceleration Rate", scroll.DecelerationRate, 0.01f, 1f);
            scroll.ScrollSensitivity = EditorGUILayout.FloatField("Scroll Sensitivity", scroll.ScrollSensitivity);
        }

        private void DrawCanvasGroupInspector(CuiCanvasGroupComponent cg)
        {
            cg.Alpha = EditorGUILayout.Slider("Group Alpha", cg.Alpha ?? 1f, 0f, 1f);
            cg.BlocksRaycasts = EditorGUILayout.Toggle("Blocks Raycasts", cg.BlocksRaycasts ?? true);
            cg.Interactable = EditorGUILayout.Toggle("Interactable", cg.Interactable ?? true);
            cg.Fade = EditorGUILayout.TextField("Fade Mode", cg.Fade);
        }

        private void DrawTooltipInspector(CuiTooltipComponent tooltip)
        {
            tooltip.Text = EditorGUILayout.TextField("Tooltip Text", tooltip.Text);
            tooltip.TooltipType = (CuiTooltipType)EditorGUILayout.EnumPopup("Tooltip Type", tooltip.TooltipType);
            tooltip.Position = (CuiTooltipPosition)EditorGUILayout.EnumPopup("Position", tooltip.Position);
            tooltip.Offset = EditorGUILayout.TextField("Offset (x y)", tooltip.Offset);
        }

        private void DrawDraggableInspector(CuiDraggableComponent drag)
        {
            drag.DragAlpha = EditorGUILayout.Slider("Drag Alpha", drag.DragAlpha, 0f, 1f);
            drag.Filter = EditorGUILayout.TextField("Filter", drag.Filter);
            drag.LimitToParent = EditorGUILayout.Toggle("Limit To Parent", drag.LimitToParent ?? false);
            drag.DropAnywhere = EditorGUILayout.Toggle("Drop Anywhere", drag.DropAnywhere ?? true);
            drag.PositionRPC = (CuiDraggablePositionSendType)EditorGUILayout.EnumPopup("Position RPC Type", drag.PositionRPC);
        }

        private void DrawLayoutGroupInspector(CuiLayoutGroupComponent lg)
        {
            lg.Spacing = EditorGUILayout.FloatField("Spacing (px)", lg.Spacing);
            lg.ChildAlignment = (TextAnchor)EditorGUILayout.EnumPopup("Child Alignment", lg.ChildAlignment);
            lg.ChildForceExpandWidth = EditorGUILayout.Toggle("Force Expand Width", lg.ChildForceExpandWidth ?? true);
            lg.ChildForceExpandHeight = EditorGUILayout.Toggle("Force Expand Height", lg.ChildForceExpandHeight ?? true);
            lg.Padding = EditorGUILayout.TextField("Padding (l t r b)", lg.Padding);
        }

        private void DrawGridLayoutInspector(CuiGridLayoutGroupComponent glg)
        {
            glg.CellSize = EditorGUILayout.TextField("Cell Size (w h)", glg.CellSize);
            glg.Spacing = EditorGUILayout.TextField("Spacing (x y)", glg.Spacing);
            glg.Constraint = (UnityEngine.UI.GridLayoutGroup.Constraint)EditorGUILayout.EnumPopup("Constraint", glg.Constraint);
            glg.ConstraintCount = EditorGUILayout.IntField("Constraint Count", glg.ConstraintCount);
            glg.Padding = EditorGUILayout.TextField("Padding (l t r b)", glg.Padding);
        }

        private string DrawCuiColorField(string label, string cuiColorStr, Color defaultCol)
        {
            Color current = CuiColorExtensions.ToUnityColor(cuiColorStr, defaultCol);
            Color next = EditorGUILayout.ColorField(label, current);
            return CuiColorExtensions.ToCuiColorString(next);
        }

        private void DrawAddComponentButton(CuiElementNode elem, Action onModified)
        {
            if (GUILayout.Button("+ Add Component", EditorStyles.toolbarDropDown))
            {
                var menu = new GenericMenu();
                foreach (var def in CuiComponentRegistry.AllDefinitions)
                {
                    bool alreadyHas = elem.Components.Any(c => c.Type == def.TypeName);
                    if (alreadyHas)
                    {
                        menu.AddDisabledItem(new GUIContent($"{def.Category}/{def.DisplayName}"));
                    }
                    else
                    {
                        menu.AddItem(new GUIContent($"{def.Category}/{def.DisplayName}"), false, () =>
                        {
                            var newComp = CuiComponentRegistry.CreateComponent(def.TypeName);
                            if (newComp != null)
                            {
                                elem.Components.Add(newComp);
                                onModified?.Invoke();
                            }
                        });
                    }
                }
                menu.ShowAsContext();
            }
        }
    }
}
