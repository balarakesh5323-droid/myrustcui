using System;
using System.Linq;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Core.Registry;
using RustCUIBuilder.Runtime.Discovery;
using RustCUIBuilder.Runtime.Rendering.Canvas;
using UnityEditor;
using UnityEngine;

namespace RustCUIBuilder.Editor.Inspector
{
    /// <summary>
    /// Dynamic typed inspector for Rust CUI Builder.
    /// Exposes full property controls for all 21 verified CUI components with visual anchor presets and color pickers.
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

            // 1. Element Header
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

            // Parent Dropdown (Verified Layers + other elements)
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

            // Header
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(comp.Type, EditorStyles.boldLabel);
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

            // Type-specific field rendering
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
                case CuiSlotComponent slot:
                    slot.Filter = EditorGUILayout.TextField("Slot Filter", slot.Filter);
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
            // Visual Anchor Presets
            EditorGUILayout.LabelField("Anchor Presets", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Stretch", EditorStyles.miniButton)) SetAnchorPreset(rect, "0 0", "1 1", "0 0", "0 0");
            if (GUILayout.Button("Center", EditorStyles.miniButton)) SetAnchorPreset(rect, "0.5 0.5", "0.5 0.5", "-100 -50", "100 50");
            if (GUILayout.Button("Top", EditorStyles.miniButton)) SetAnchorPreset(rect, "0 1", "1 1", "0 -60", "0 0");
            if (GUILayout.Button("Bottom", EditorStyles.miniButton)) SetAnchorPreset(rect, "0 0", "1 0", "0 0", "0 60");
            EditorGUILayout.EndHorizontal();

            rect.AnchorMin = EditorGUILayout.TextField("Anchor Min", rect.AnchorMin);
            rect.AnchorMax = EditorGUILayout.TextField("Anchor Max", rect.AnchorMax);
            rect.OffsetMin = EditorGUILayout.TextField("Offset Min (px)", rect.OffsetMin);
            rect.OffsetMax = EditorGUILayout.TextField("Offset Max (px)", rect.OffsetMax);
            rect.Pivot = EditorGUILayout.TextField("Pivot", rect.Pivot);
            rect.Rotation = EditorGUILayout.FloatField("Rotation", rect.Rotation);
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
            EditorGUILayout.LabelField("Text Content");
            txt.Text = EditorGUILayout.TextArea(txt.Text, GUILayout.Height(40));
            txt.FontSize = EditorGUILayout.IntSlider("Font Size", txt.FontSize, 8, 72);

            // Font Dropdown
            int fontIdx = Array.IndexOf(RustAssetDiscovery.VerifiedFonts, txt.Font);
            if (fontIdx < 0) fontIdx = 0;
            int newFontIdx = EditorGUILayout.Popup("Font", fontIdx, RustAssetDiscovery.VerifiedFonts);
            txt.Font = RustAssetDiscovery.VerifiedFonts[newFontIdx];

            txt.Align = (TextAnchor)EditorGUILayout.EnumPopup("Alignment", txt.Align);
            txt.Color = DrawCuiColorField("Color", txt.Color, Color.white);
            txt.FadeIn = EditorGUILayout.FloatField("Fade In (sec)", txt.FadeIn);
        }

        private void DrawImageInspector(CuiImageComponent img)
        {
            img.Sprite = EditorGUILayout.TextField("Sprite Asset", img.Sprite);
            img.Color = DrawCuiColorField("Color", img.Color, Color.white);
            img.ImageType = (UnityEngine.UI.Image.Type)EditorGUILayout.EnumPopup("Image Type", img.ImageType);
            img.ItemId = EditorGUILayout.IntField("Rust Item ID", img.ItemId);
            img.Material = EditorGUILayout.TextField("Material", img.Material);
            img.FadeIn = EditorGUILayout.FloatField("Fade In", img.FadeIn);
        }

        private void DrawRawImageInspector(CuiRawImageComponent raw)
        {
            raw.Url = EditorGUILayout.TextField("Web Image URL", raw.Url);
            raw.SteamId = EditorGUILayout.TextField("Steam Avatar ID", raw.SteamId);
            raw.Color = DrawCuiColorField("Color", raw.Color, Color.white);
            raw.Material = EditorGUILayout.TextField("Material", raw.Material);
        }

        private void DrawButtonInspector(CuiButtonComponent btn)
        {
            btn.Command = EditorGUILayout.TextField("Console Command", btn.Command);
            btn.Close = EditorGUILayout.TextField("Close Panel", btn.Close);
            btn.Color = DrawCuiColorField("Base Color", btn.Color, new Color(0.2f, 0.6f, 0.3f, 1f));
            btn.Material = EditorGUILayout.TextField("Material", btn.Material);
            btn.FadeIn = EditorGUILayout.FloatField("Fade In", btn.FadeIn);
        }

        private void DrawInputFieldInspector(CuiInputFieldComponent input)
        {
            input.Text = EditorGUILayout.TextField("Initial Text", input.Text);
            input.Command = EditorGUILayout.TextField("Submit Command", input.Command);
            input.FontSize = EditorGUILayout.IntSlider("Font Size", input.FontSize, 8, 48);
            input.CharsLimit = EditorGUILayout.IntField("Character Limit", input.CharsLimit);
            input.IsPassword = EditorGUILayout.Toggle("Password Mode", input.IsPassword);
            input.NeedsKeyboard = EditorGUILayout.Toggle("Needs Keyboard", input.NeedsKeyboard);
            input.Autofocus = EditorGUILayout.Toggle("Autofocus", input.Autofocus);
        }

        private void DrawCountdownInspector(CuiCountdownComponent count)
        {
            count.StartTime = EditorGUILayout.FloatField("Start Time (sec)", count.StartTime);
            count.EndTime = EditorGUILayout.FloatField("End Time (sec)", count.EndTime);
            count.Step = EditorGUILayout.FloatField("Step", count.Step);
            count.Interval = EditorGUILayout.FloatField("Interval", count.Interval);
            count.TimerFormat = (CuiTimerFormat)EditorGUILayout.EnumPopup("Timer Format", count.TimerFormat);
            count.Command = EditorGUILayout.TextField("End Command", count.Command);
            count.DestroyIfDone = EditorGUILayout.Toggle("Destroy When Done", count.DestroyIfDone);
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
            scroll.ScrollSensitivity = EditorGUILayout.FloatField("Scroll Sensitivity", scroll.ScrollSensitivity);
        }

        private void DrawCanvasGroupInspector(CuiCanvasGroupComponent cg)
        {
            cg.Alpha = EditorGUILayout.Slider("Group Alpha", cg.Alpha ?? 1f, 0f, 1f);
            cg.BlocksRaycasts = EditorGUILayout.Toggle("Blocks Raycasts", cg.BlocksRaycasts ?? true);
            cg.Interactable = EditorGUILayout.Toggle("Interactable", cg.Interactable ?? true);
        }

        private void DrawTooltipInspector(CuiTooltipComponent tooltip)
        {
            tooltip.Text = EditorGUILayout.TextField("Tooltip Text", tooltip.Text);
            tooltip.TooltipType = (CuiTooltipType)EditorGUILayout.EnumPopup("Tooltip Type", tooltip.TooltipType);
            tooltip.Position = (CuiTooltipPosition)EditorGUILayout.EnumPopup("Position", tooltip.Position);
            tooltip.Offset = EditorGUILayout.TextField("Offset", tooltip.Offset);
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
            lg.Spacing = EditorGUILayout.FloatField("Spacing", lg.Spacing);
            lg.ChildAlignment = (TextAnchor)EditorGUILayout.EnumPopup("Child Alignment", lg.ChildAlignment);
            lg.ChildForceExpandWidth = EditorGUILayout.Toggle("Force Expand Width", lg.ChildForceExpandWidth ?? true);
            lg.ChildForceExpandHeight = EditorGUILayout.Toggle("Force Expand Height", lg.ChildForceExpandHeight ?? true);
            lg.Padding = EditorGUILayout.TextField("Padding (l t r b)", lg.Padding);
        }

        private void DrawGridLayoutInspector(CuiGridLayoutGroupComponent glg)
        {
            glg.CellSize = EditorGUILayout.TextField("Cell Size", glg.CellSize);
            glg.Spacing = EditorGUILayout.TextField("Spacing", glg.Spacing);
            glg.Constraint = (UnityEngine.UI.GridLayoutGroup.Constraint)EditorGUILayout.EnumPopup("Constraint", glg.Constraint);
            glg.ConstraintCount = EditorGUILayout.IntField("Constraint Count", glg.ConstraintCount);
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
