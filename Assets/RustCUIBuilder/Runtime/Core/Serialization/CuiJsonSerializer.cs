using System;
using System.Collections.Generic;
using System.Text;
using RustCUIBuilder.Runtime.Core.Models;
using UnityEngine;

namespace RustCUIBuilder.Runtime.Core.Serialization
{
    /// <summary>
    /// High-performance, zero-dependency JSON serializer and parser for Rust Oxide CUI.
    /// Strictly replicates the serialization format of Oxide.Game.Rust.Cui.CuiHelper and ComponentConverter.
    /// </summary>
    public static class CuiJsonSerializer
    {
        public static string SerializeDocument(CuiDocument document, bool indented = true)
        {
            if (document == null || document.Elements == null)
                return "[]";

            return SerializeElements(document.Elements, indented);
        }

        public static string SerializeElements(IReadOnlyList<CuiElementNode> elements, bool indented = true)
        {
            if (elements == null || elements.Count == 0)
                return "[]";

            var sb = new StringBuilder(4096);
            string nl = indented ? "\n" : "";
            string ind1 = indented ? "  " : "";
            string ind2 = indented ? "    " : "";
            string ind3 = indented ? "      " : "";

            sb.Append("[").Append(nl);

            for (int i = 0; i < elements.Count; i++)
            {
                var elem = elements[i];
                sb.Append(ind1).Append("{").Append(nl);

                // name
                sb.Append(ind2).Append("\"name\": \"").Append(Escape(elem.Name)).Append("\",").Append(nl);

                // parent
                if (!string.IsNullOrEmpty(elem.Parent))
                {
                    sb.Append(ind2).Append("\"parent\": \"").Append(Escape(elem.Parent)).Append("\",").Append(nl);
                }

                // destroyUi
                if (!string.IsNullOrEmpty(elem.DestroyUi))
                {
                    sb.Append(ind2).Append("\"destroyUi\": \"").Append(Escape(elem.DestroyUi)).Append("\",").Append(nl);
                }

                // fadeOut
                if (elem.FadeOut > 0.0001f)
                {
                    sb.Append(ind2).Append("\"fadeOut\": ").Append(elem.FadeOut.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Append(",").Append(nl);
                }

                // update
                if (elem.Update)
                {
                    sb.Append(ind2).Append("\"update\": true,").Append(nl);
                }

                // activeSelf
                if (elem.ActiveSelf.HasValue)
                {
                    sb.Append(ind2).Append("\"activeSelf\": ").Append(elem.ActiveSelf.Value ? "true" : "false").Append(",").Append(nl);
                }

                // components
                sb.Append(ind2).Append("\"components\": [").Append(nl);

                for (int j = 0; j < elem.Components.Count; j++)
                {
                    var comp = elem.Components[j];
                    sb.Append(ind3).Append("{").Append(nl);
                    SerializeComponentProperties(sb, comp, ind3 + (indented ? "  " : ""), indented);
                    sb.Append(ind3).Append("}");
                    if (j < elem.Components.Count - 1) sb.Append(",");
                    sb.Append(nl);
                }

                sb.Append(ind2).Append("]").Append(nl);

                sb.Append(ind1).Append("}");
                if (i < elements.Count - 1) sb.Append(",");
                sb.Append(nl);
            }

            sb.Append("]");
            return sb.ToString();
        }

        private static void SerializeComponentProperties(StringBuilder sb, ICuiComponent comp, string indent, bool indented)
        {
            string nl = indented ? "\n" : "";

            // type is always first
            sb.Append(indent).Append("\"type\": \"").Append(Escape(comp.Type)).Append("\"");

            switch (comp)
            {
                case CuiRectTransformComponent rect:
                    AppendString(sb, indent, "anchormin", rect.AnchorMin, nl);
                    AppendString(sb, indent, "anchormax", rect.AnchorMax, nl);
                    AppendString(sb, indent, "offsetmin", rect.OffsetMin, nl);
                    AppendString(sb, indent, "offsetmax", rect.OffsetMax, nl);
                    if (Mathf.Abs(rect.Rotation) > 0.001f) AppendFloat(sb, indent, "rotation", rect.Rotation, nl);
                    if (!string.IsNullOrEmpty(rect.Pivot) && rect.Pivot != "0.5 0.5") AppendString(sb, indent, "pivot", rect.Pivot, nl);
                    if (!string.IsNullOrEmpty(rect.SetParent)) AppendString(sb, indent, "setParent", rect.SetParent, nl);
                    if (rect.SetTransformIndex >= 0) AppendInt(sb, indent, "setTransformIndex", rect.SetTransformIndex, nl);
                    break;

                case CuiTextComponent text:
                    AppendString(sb, indent, "text", text.Text, nl);
                    AppendInt(sb, indent, "fontSize", text.FontSize, nl);
                    if (!string.IsNullOrEmpty(text.Font)) AppendString(sb, indent, "font", text.Font, nl);
                    AppendString(sb, indent, "align", text.Align.ToString(), nl);
                    AppendString(sb, indent, "color", text.Color, nl);
                    if (text.VerticalOverflow != VerticalWrapMode.Truncate) AppendString(sb, indent, "verticalOverflow", text.VerticalOverflow.ToString(), nl);
                    if (text.FadeIn > 0.001f) AppendFloat(sb, indent, "fadeIn", text.FadeIn, nl);
                    if (text.BlocksRaycast.HasValue) AppendBool(sb, indent, "blocksRaycast", text.BlocksRaycast.Value, nl);
                    if (text.Enabled.HasValue && !text.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
                    break;

                case CuiImageComponent img:
                    if (!string.IsNullOrEmpty(img.Sprite)) AppendString(sb, indent, "sprite", img.Sprite, nl);
                    if (!string.IsNullOrEmpty(img.Material)) AppendString(sb, indent, "material", img.Material, nl);
                    AppendString(sb, indent, "color", img.Color, nl);
                    if (img.ImageType != UnityEngine.UI.Image.Type.Simple) AppendString(sb, indent, "imagetype", img.ImageType.ToString(), nl);
                    if (img.FillCenter.HasValue && !img.FillCenter.Value) AppendBool(sb, indent, "fillCenter", false, nl);
                    if (!string.IsNullOrEmpty(img.Png)) AppendString(sb, indent, "png", img.Png, nl);
                    if (!string.IsNullOrEmpty(img.Slice)) AppendString(sb, indent, "slice", img.Slice, nl);
                    if (img.ItemId != 0) AppendInt(sb, indent, "itemid", img.ItemId, nl);
                    if (img.SkinId != 0) AppendUlong(sb, indent, "skinid", img.SkinId, nl);
                    if (Mathf.Abs(img.PixelsPerUnitMultiplier - 1f) > 0.01f) AppendFloat(sb, indent, "ppuMultiplier", img.PixelsPerUnitMultiplier, nl);
                    if (img.FadeIn > 0.001f) AppendFloat(sb, indent, "fadeIn", img.FadeIn, nl);
                    if (img.BlocksRaycast.HasValue) AppendBool(sb, indent, "blocksRaycast", img.BlocksRaycast.Value, nl);
                    if (img.Enabled.HasValue && !img.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
                    break;

                case CuiRawImageComponent raw:
                    if (!string.IsNullOrEmpty(raw.Sprite)) AppendString(sb, indent, "sprite", raw.Sprite, nl);
                    AppendString(sb, indent, "color", raw.Color, nl);
                    if (!string.IsNullOrEmpty(raw.Material)) AppendString(sb, indent, "material", raw.Material, nl);
                    if (!string.IsNullOrEmpty(raw.Url)) AppendString(sb, indent, "url", raw.Url, nl);
                    if (!string.IsNullOrEmpty(raw.Png)) AppendString(sb, indent, "png", raw.Png, nl);
                    if (!string.IsNullOrEmpty(raw.SteamId)) AppendString(sb, indent, "steamid", raw.SteamId, nl);
                    if (raw.FadeIn > 0.001f) AppendFloat(sb, indent, "fadeIn", raw.FadeIn, nl);
                    if (raw.BlocksRaycast.HasValue) AppendBool(sb, indent, "blocksRaycast", raw.BlocksRaycast.Value, nl);
                    if (raw.Enabled.HasValue && !raw.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
                    break;

                case CuiButtonComponent btn:
                    if (!string.IsNullOrEmpty(btn.Command)) AppendString(sb, indent, "command", btn.Command, nl);
                    if (!string.IsNullOrEmpty(btn.Close)) AppendString(sb, indent, "close", btn.Close, nl);
                    if (!string.IsNullOrEmpty(btn.Sprite)) AppendString(sb, indent, "sprite", btn.Sprite, nl);
                    if (!string.IsNullOrEmpty(btn.Material)) AppendString(sb, indent, "material", btn.Material, nl);
                    AppendString(sb, indent, "color", btn.Color, nl);
                    if (btn.ImageType != UnityEngine.UI.Image.Type.Simple) AppendString(sb, indent, "imagetype", btn.ImageType.ToString(), nl);
                    if (!string.IsNullOrEmpty(btn.NormalColor)) AppendString(sb, indent, "normalColor", btn.NormalColor, nl);
                    if (!string.IsNullOrEmpty(btn.HighlightedColor)) AppendString(sb, indent, "highlightedColor", btn.HighlightedColor, nl);
                    if (!string.IsNullOrEmpty(btn.PressedColor)) AppendString(sb, indent, "pressedColor", btn.PressedColor, nl);
                    if (!string.IsNullOrEmpty(btn.SelectedColor)) AppendString(sb, indent, "selectedColor", btn.SelectedColor, nl);
                    if (!string.IsNullOrEmpty(btn.DisabledColor)) AppendString(sb, indent, "disabledColor", btn.DisabledColor, nl);
                    if (Mathf.Abs(btn.ColorMultiplier - 1f) > 0.01f) AppendFloat(sb, indent, "colorMultiplier", btn.ColorMultiplier, nl);
                    if (btn.FadeDuration.HasValue) AppendFloat(sb, indent, "fadeDuration", btn.FadeDuration.Value, nl);
                    if (btn.Interactable.HasValue && !btn.Interactable.Value) AppendBool(sb, indent, "interactable", false, nl);
                    if (btn.FadeIn > 0.001f) AppendFloat(sb, indent, "fadeIn", btn.FadeIn, nl);
                    if (btn.BlocksRaycast.HasValue) AppendBool(sb, indent, "blocksRaycast", btn.BlocksRaycast.Value, nl);
                    if (btn.Enabled.HasValue && !btn.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
                    break;

                case CuiInputFieldComponent input:
                    AppendString(sb, indent, "text", input.Text, nl);
                    AppendInt(sb, indent, "fontSize", input.FontSize, nl);
                    if (!string.IsNullOrEmpty(input.Font)) AppendString(sb, indent, "font", input.Font, nl);
                    AppendString(sb, indent, "align", input.Align.ToString(), nl);
                    AppendString(sb, indent, "color", input.Color, nl);
                    if (input.CharsLimit > 0) AppendInt(sb, indent, "characterLimit", input.CharsLimit, nl);
                    if (!string.IsNullOrEmpty(input.Command)) AppendString(sb, indent, "command", input.Command, nl);
                    if (input.LineType != UnityEngine.UI.InputField.LineType.SingleLine) AppendString(sb, indent, "lineType", input.LineType.ToString(), nl);
                    if (input.ReadOnly) AppendBool(sb, indent, "readOnly", true, nl);
                    if (!string.IsNullOrEmpty(input.PlaceholderId)) AppendString(sb, indent, "placeholderId", input.PlaceholderId, nl);
                    if (input.IsPassword) AppendBool(sb, indent, "password", true, nl);
                    if (input.NeedsKeyboard) AppendBool(sb, indent, "needsKeyboard", true, nl);
                    if (input.HudMenuInput) AppendBool(sb, indent, "hudMenuInput", true, nl);
                    if (input.Autofocus) AppendBool(sb, indent, "autofocus", true, nl);
                    if (input.Interactable.HasValue && !input.Interactable.Value) AppendBool(sb, indent, "interactable", false, nl);
                    if (input.FadeIn > 0.001f) AppendFloat(sb, indent, "fadeIn", input.FadeIn, nl);
                    if (input.BlocksRaycast.HasValue) AppendBool(sb, indent, "blocksRaycast", input.BlocksRaycast.Value, nl);
                    if (input.Enabled.HasValue && !input.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
                    break;

                case CuiCountdownComponent count:
                    AppendFloat(sb, indent, "endTime", count.EndTime, nl);
                    AppendFloat(sb, indent, "startTime", count.StartTime, nl);
                    AppendFloat(sb, indent, "step", count.Step, nl);
                    AppendFloat(sb, indent, "interval", count.Interval, nl);
                    if (count.TimerFormat != CuiTimerFormat.None) AppendString(sb, indent, "timerFormat", count.TimerFormat.ToString(), nl);
                    if (!string.IsNullOrEmpty(count.NumberFormat)) AppendString(sb, indent, "numberFormat", count.NumberFormat, nl);
                    AppendBool(sb, indent, "destroyIfDone", count.DestroyIfDone, nl);
                    if (!string.IsNullOrEmpty(count.Command)) AppendString(sb, indent, "command", count.Command, nl);
                    if (count.FadeIn > 0.001f) AppendFloat(sb, indent, "fadeIn", count.FadeIn, nl);
                    if (count.Enabled.HasValue && !count.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
                    break;

                case CuiOutlineComponent outline:
                    AppendString(sb, indent, "color", outline.Color, nl);
                    AppendString(sb, indent, "distance", outline.Distance, nl);
                    if (outline.UseGraphicAlpha) AppendBool(sb, indent, "useGraphicAlpha", true, nl);
                    if (outline.Enabled.HasValue && !outline.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
                    break;

                case CuiScrollViewComponent scroll:
                    if (scroll.ContentTransform != null)
                    {
                        sb.Append(",").Append(nl).Append(indent).Append("\"contentTransform\": {");
                        var subIndent = indent + "  ";
                        sb.Append(nl).Append(subIndent).Append("\"type\": \"RectTransform\"");
                        AppendString(sb, subIndent, "anchormin", scroll.ContentTransform.AnchorMin, nl);
                        AppendString(sb, subIndent, "anchormax", scroll.ContentTransform.AnchorMax, nl);
                        AppendString(sb, subIndent, "offsetmin", scroll.ContentTransform.OffsetMin, nl);
                        AppendString(sb, subIndent, "offsetmax", scroll.ContentTransform.OffsetMax, nl);
                        sb.Append(nl).Append(indent).Append("}");
                    }
                    AppendBool(sb, indent, "horizontal", scroll.Horizontal, nl);
                    AppendBool(sb, indent, "vertical", scroll.Vertical, nl);
                    AppendString(sb, indent, "movementType", scroll.MovementType.ToString(), nl);
                    AppendFloat(sb, indent, "elasticity", scroll.Elasticity, nl);
                    AppendBool(sb, indent, "inertia", scroll.Inertia, nl);
                    AppendFloat(sb, indent, "decelerationRate", scroll.DecelerationRate, nl);
                    AppendFloat(sb, indent, "scrollSensitivity", scroll.ScrollSensitivity, nl);
                    if (scroll.HorizontalNormalizedPosition.HasValue) AppendFloat(sb, indent, "horizontalNormalizedPosition", scroll.HorizontalNormalizedPosition.Value, nl);
                    if (scroll.VerticalNormalizedPosition.HasValue) AppendFloat(sb, indent, "verticalNormalizedPosition", scroll.VerticalNormalizedPosition.Value, nl);
                    if (scroll.Enabled.HasValue && !scroll.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
                    break;

                case CuiCanvasGroupComponent cg:
                    if (cg.Alpha.HasValue) AppendFloat(sb, indent, "alpha", cg.Alpha.Value, nl);
                    if (cg.BlocksRaycasts.HasValue) AppendBool(sb, indent, "blocksRaycasts", cg.BlocksRaycasts.Value, nl);
                    if (cg.Interactable.HasValue) AppendBool(sb, indent, "interactable", cg.Interactable.Value, nl);
                    if (!string.IsNullOrEmpty(cg.Fade)) AppendString(sb, indent, "fade", cg.Fade, nl);
                    break;

                case CuiMaskComponent mask:
                    if (mask.ShowMaskGraphic.HasValue) AppendBool(sb, indent, "showMaskGraphic", mask.ShowMaskGraphic.Value, nl);
                    if (mask.Enabled.HasValue && !mask.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
                    break;

                case CuiHorizontalLayoutGroupComponent hlg:
                    AppendLayoutGroupProps(sb, indent, hlg, nl);
                    break;

                case CuiVerticalLayoutGroupComponent vlg:
                    AppendLayoutGroupProps(sb, indent, vlg, nl);
                    break;

                case CuiGridLayoutGroupComponent glg:
                    AppendString(sb, indent, "cellSize", glg.CellSize, nl);
                    AppendString(sb, indent, "spacing", glg.Spacing, nl);
                    AppendString(sb, indent, "startCorner", glg.StartCorner.ToString(), nl);
                    AppendString(sb, indent, "startAxis", glg.StartAxis.ToString(), nl);
                    AppendString(sb, indent, "childAlignment", glg.ChildAlignment.ToString(), nl);
                    AppendString(sb, indent, "constraint", glg.Constraint.ToString(), nl);
                    AppendInt(sb, indent, "constraintCount", glg.ConstraintCount, nl);
                    if (!string.IsNullOrEmpty(glg.Padding)) AppendString(sb, indent, "padding", glg.Padding, nl);
                    if (glg.Enabled.HasValue && !glg.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
                    break;

                case CuiContentSizeFitterComponent csf:
                    AppendString(sb, indent, "horizontalFit", csf.HorizontalFit.ToString(), nl);
                    AppendString(sb, indent, "verticalFit", csf.VerticalFit.ToString(), nl);
                    if (csf.Enabled.HasValue && !csf.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
                    break;

                case CuiLayoutElementComponent le:
                    if (le.PreferredWidth >= 0) AppendFloat(sb, indent, "preferredWidth", le.PreferredWidth, nl);
                    if (le.PreferredHeight >= 0) AppendFloat(sb, indent, "preferredHeight", le.PreferredHeight, nl);
                    if (le.MinWidth >= 0) AppendFloat(sb, indent, "minWidth", le.MinWidth, nl);
                    if (le.MinHeight >= 0) AppendFloat(sb, indent, "minHeight", le.MinHeight, nl);
                    if (le.FlexibleWidth >= 0) AppendFloat(sb, indent, "flexibleWidth", le.FlexibleWidth, nl);
                    if (le.FlexibleHeight >= 0) AppendFloat(sb, indent, "flexibleHeight", le.FlexibleHeight, nl);
                    if (le.IgnoreLayout.HasValue) AppendBool(sb, indent, "ignoreLayout", le.IgnoreLayout.Value, nl);
                    if (le.Enabled.HasValue && !le.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
                    break;

                case CuiTooltipComponent tooltip:
                    AppendString(sb, indent, "text", tooltip.Text, nl);
                    AppendString(sb, indent, "tooltipType", tooltip.TooltipType.ToString(), nl);
                    if (!string.IsNullOrEmpty(tooltip.Offset)) AppendString(sb, indent, "offset", tooltip.Offset, nl);
                    if (tooltip.UseCentre.HasValue) AppendBool(sb, indent, "useCentre", tooltip.UseCentre.Value, nl);
                    AppendString(sb, indent, "delay", tooltip.Delay.ToString(), nl);
                    AppendString(sb, indent, "position", tooltip.Position.ToString(), nl);
                    if (tooltip.Enabled.HasValue && !tooltip.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
                    break;

                case CuiDraggableComponent drag:
                    if (drag.LimitToParent.HasValue) AppendBool(sb, indent, "limitToParent", drag.LimitToParent.Value, nl);
                    if (drag.MaxDistance >= 0) AppendFloat(sb, indent, "maxDistance", drag.MaxDistance, nl);
                    if (drag.AllowSwapping.HasValue) AppendBool(sb, indent, "allowSwapping", drag.AllowSwapping.Value, nl);
                    if (drag.DropAnywhere.HasValue) AppendBool(sb, indent, "dropAnywhere", drag.DropAnywhere.Value, nl);
                    AppendFloat(sb, indent, "dragAlpha", drag.DragAlpha, nl);
                    if (drag.ParentLimitIndex > 0) AppendInt(sb, indent, "parentLimitIndex", drag.ParentLimitIndex, nl);
                    if (!string.IsNullOrEmpty(drag.Filter)) AppendString(sb, indent, "filter", drag.Filter, nl);
                    if (!string.IsNullOrEmpty(drag.ParentPadding)) AppendString(sb, indent, "parentPadding", drag.ParentPadding, nl);
                    if (!string.IsNullOrEmpty(drag.AnchorOffset)) AppendString(sb, indent, "anchorOffset", drag.AnchorOffset, nl);
                    if (drag.KeepOnTop.HasValue) AppendBool(sb, indent, "keepOnTop", drag.KeepOnTop.Value, nl);
                    AppendString(sb, indent, "positionRPC", drag.PositionRPC.ToString(), nl);
                    if (drag.MoveToAnchor) AppendBool(sb, indent, "moveToAnchor", true, nl);
                    if (drag.RebuildAnchor) AppendBool(sb, indent, "rebuildAnchor", true, nl);
                    if (drag.Enabled.HasValue && !drag.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
                    break;

                case CuiSlotComponent slot:
                    if (!string.IsNullOrEmpty(slot.Filter)) AppendString(sb, indent, "filter", slot.Filter, nl);
                    if (slot.Enabled.HasValue && !slot.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
                    break;

                case CuiNeedsCursorComponent cursor:
                    if (cursor.Enabled.HasValue && !cursor.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
                    break;

                case CuiNeedsKeyboardComponent kb:
                    if (kb.Enabled.HasValue && !kb.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
                    break;
            }
        }

        private static void AppendLayoutGroupProps(StringBuilder sb, string indent, CuiLayoutGroupComponent lg, string nl)
        {
            if (Mathf.Abs(lg.Spacing) > 0.01f) AppendFloat(sb, indent, "spacing", lg.Spacing, nl);
            AppendString(sb, indent, "childAlignment", lg.ChildAlignment.ToString(), nl);
            if (lg.ChildForceExpandWidth.HasValue) AppendBool(sb, indent, "childForceExpandWidth", lg.ChildForceExpandWidth.Value, nl);
            if (lg.ChildForceExpandHeight.HasValue) AppendBool(sb, indent, "childForceExpandHeight", lg.ChildForceExpandHeight.Value, nl);
            if (lg.ChildControlWidth.HasValue) AppendBool(sb, indent, "childControlWidth", lg.ChildControlWidth.Value, nl);
            if (lg.ChildControlHeight.HasValue) AppendBool(sb, indent, "childControlHeight", lg.ChildControlHeight.Value, nl);
            if (lg.ChildScaleWidth.HasValue && lg.ChildScaleWidth.Value) AppendBool(sb, indent, "childScaleWidth", true, nl);
            if (lg.ChildScaleHeight.HasValue && lg.ChildScaleHeight.Value) AppendBool(sb, indent, "childScaleHeight", true, nl);
            if (!string.IsNullOrEmpty(lg.Padding)) AppendString(sb, indent, "padding", lg.Padding, nl);
            if (lg.Enabled.HasValue && !lg.Enabled.Value) AppendBool(sb, indent, "enabled", false, nl);
        }

        private static void AppendString(StringBuilder sb, string indent, string key, string value, string nl)
        {
            if (value == null) return;
            sb.Append(",").Append(nl).Append(indent).Append("\"").Append(key).Append("\": \"").Append(Escape(value)).Append("\"");
        }

        private static void AppendFloat(StringBuilder sb, string indent, string key, float value, string nl)
        {
            sb.Append(",").Append(nl).Append(indent).Append("\"").Append(key).Append("\": ").Append(value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
        }

        private static void AppendInt(StringBuilder sb, string indent, string key, int value, string nl)
        {
            sb.Append(",").Append(nl).Append(indent).Append("\"").Append(key).Append("\": ").Append(value);
        }

        private static void AppendUlong(StringBuilder sb, string indent, string key, ulong value, string nl)
        {
            sb.Append(",").Append(nl).Append(indent).Append("\"").Append(key).Append("\": ").Append(value);
        }

        private static void AppendBool(StringBuilder sb, string indent, string key, bool value, string nl)
        {
            sb.Append(",").Append(nl).Append(indent).Append("\"").Append(key).Append("\": ").Append(value ? "true" : "false");
        }

        private static string Escape(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
    }
}
