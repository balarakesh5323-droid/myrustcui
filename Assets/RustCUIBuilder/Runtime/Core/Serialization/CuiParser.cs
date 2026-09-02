using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Core.Registry;
using UnityEngine;
using UnityEngine.UI;

namespace RustCUIBuilder.Runtime.Core.Serialization
{
    public class CuiParseResult
    {
        public bool Success { get; set; }
        public CuiDocument Document { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    /// <summary>
    /// Parses Rust Oxide CUI JSON structures into internal CuiDocument and CuiElementNode AST hierarchies.
    /// Handles Oxide ComponentConverter discrimination and reports clear warnings for unknown properties.
    /// </summary>
    public static class CuiParser
    {
        public static CuiParseResult ParseJson(string json, string projectName = "ImportedCUI")
        {
            var result = new CuiParseResult
            {
                Document = new CuiDocument { ProjectName = projectName }
            };

            if (string.IsNullOrWhiteSpace(json))
            {
                result.Errors.Add("Empty JSON input provided.");
                return result;
            }

            try
            {
                // Simple tokenization of elements array
                string trimmed = json.Trim();
                if (!trimmed.StartsWith("[") || !trimmed.EndsWith("]"))
                {
                    result.Errors.Add("CUI JSON root must be an array of elements starting with [ and ending with ].");
                    return result;
                }

                var elementBlocks = SplitJsonArrayObjects(trimmed);
                foreach (var elemJson in elementBlocks)
                {
                    var node = ParseElementNode(elemJson, result.Warnings);
                    if (node != null)
                    {
                        result.Document.Elements.Add(node);
                    }
                }

                result.Success = result.Document.Elements.Count > 0;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"JSON Parsing error: {ex.Message}");
                result.Success = false;
            }

            return result;
        }

        private static CuiElementNode ParseElementNode(string elemJson, List<string> warnings)
        {
            var node = new CuiElementNode();

            node.Name = ExtractStringProp(elemJson, "name") ?? "Element_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            node.Parent = ExtractStringProp(elemJson, "parent") ?? "Overlay";
            node.DestroyUi = ExtractStringProp(elemJson, "destroyUi");
            node.FadeOut = ExtractFloatProp(elemJson, "fadeOut", 0f);
            node.Update = ExtractBoolProp(elemJson, "update", false);

            if (elemJson.Contains("\"activeSelf\""))
            {
                node.ActiveSelf = ExtractBoolProp(elemJson, "activeSelf", true);
            }

            // Extract components array
            int compIdx = elemJson.IndexOf("\"components\"", StringComparison.OrdinalIgnoreCase);
            if (compIdx >= 0)
            {
                int startBracket = elemJson.IndexOf('[', compIdx);
                if (startBracket >= 0)
                {
                    int endBracket = FindMatchingBracket(elemJson, startBracket, '[', ']');
                    if (endBracket > startBracket)
                    {
                        string compArrayContent = elemJson.Substring(startBracket, endBracket - startBracket + 1);
                        var compBlocks = SplitJsonArrayObjects(compArrayContent);

                        foreach (var compJson in compBlocks)
                        {
                            string compType = ExtractStringProp(compJson, "type");
                            if (string.IsNullOrEmpty(compType))
                            {
                                warnings.Add($"Component in {node.Name} is missing 'type' property.");
                                continue;
                            }

                            var comp = ParseComponent(compType, compJson, warnings);
                            if (comp != null)
                            {
                                node.Components.Add(comp);
                            }
                            else
                            {
                                warnings.Add($"Unrecognized or unsupported CUI component type: '{compType}' in element '{node.Name}'.");
                            }
                        }
                    }
                }
            }

            // Ensure element has RectTransform
            if (!node.HasComponent<CuiRectTransformComponent>())
            {
                node.Components.Insert(0, new CuiRectTransformComponent());
            }

            return node;
        }

        private static ICuiComponent ParseComponent(string type, string compJson, List<string> warnings)
        {
            switch (type)
            {
                case "RectTransform":
                {
                    var rect = new CuiRectTransformComponent
                    {
                        AnchorMin = ExtractStringProp(compJson, "anchormin") ?? "0.0 0.0",
                        AnchorMax = ExtractStringProp(compJson, "anchormax") ?? "1.0 1.0",
                        OffsetMin = ExtractStringProp(compJson, "offsetmin") ?? "0.0 0.0",
                        OffsetMax = ExtractStringProp(compJson, "offsetmax") ?? "0.0 0.0",
                        Rotation = ExtractFloatProp(compJson, "rotation", 0f),
                        Pivot = ExtractStringProp(compJson, "pivot") ?? "0.5 0.5",
                        SetParent = ExtractStringProp(compJson, "setParent"),
                        SetTransformIndex = ExtractIntProp(compJson, "setTransformIndex", -1)
                    };
                    return rect;
                }
                case "UnityEngine.UI.Text":
                {
                    var txt = new CuiTextComponent
                    {
                        Text = ExtractStringProp(compJson, "text") ?? "",
                        FontSize = ExtractIntProp(compJson, "fontSize", 14),
                        Font = ExtractStringProp(compJson, "font") ?? "RobotoCondensed-Bold.ttf",
                        Color = ExtractStringProp(compJson, "color") ?? "1.0 1.0 1.0 1.0",
                        FadeIn = ExtractFloatProp(compJson, "fadeIn", 0f)
                    };
                    string alignStr = ExtractStringProp(compJson, "align");
                    if (!string.IsNullOrEmpty(alignStr) && Enum.TryParse<TextAnchor>(alignStr, true, out var parsedAlign))
                    {
                        txt.Align = parsedAlign;
                    }
                    return txt;
                }
                case "UnityEngine.UI.Image":
                {
                    var img = new CuiImageComponent
                    {
                        Sprite = ExtractStringProp(compJson, "sprite") ?? "assets/content/ui/ui.background.tile.psd",
                        Material = ExtractStringProp(compJson, "material"),
                        Color = ExtractStringProp(compJson, "color") ?? "1.0 1.0 1.0 1.0",
                        Png = ExtractStringProp(compJson, "png"),
                        Slice = ExtractStringProp(compJson, "slice"),
                        ItemId = ExtractIntProp(compJson, "itemid", 0),
                        SkinId = (ulong)ExtractIntProp(compJson, "skinid", 0),
                        FadeIn = ExtractFloatProp(compJson, "fadeIn", 0f)
                    };
                    string imgTypeStr = ExtractStringProp(compJson, "imagetype");
                    if (!string.IsNullOrEmpty(imgTypeStr) && Enum.TryParse<Image.Type>(imgTypeStr, true, out var parsedImgType))
                    {
                        img.ImageType = parsedImgType;
                    }
                    return img;
                }
                case "UnityEngine.UI.RawImage":
                {
                    var raw = new CuiRawImageComponent
                    {
                        Sprite = ExtractStringProp(compJson, "sprite"),
                        Color = ExtractStringProp(compJson, "color") ?? "1.0 1.0 1.0 1.0",
                        Material = ExtractStringProp(compJson, "material"),
                        Url = ExtractStringProp(compJson, "url"),
                        Png = ExtractStringProp(compJson, "png"),
                        SteamId = ExtractStringProp(compJson, "steamid"),
                        FadeIn = ExtractFloatProp(compJson, "fadeIn", 0f)
                    };
                    return raw;
                }
                case "UnityEngine.UI.Button":
                {
                    var btn = new CuiButtonComponent
                    {
                        Command = ExtractStringProp(compJson, "command") ?? "",
                        Close = ExtractStringProp(compJson, "close") ?? "",
                        Sprite = ExtractStringProp(compJson, "sprite") ?? "assets/content/ui/ui.background.tile.psd",
                        Material = ExtractStringProp(compJson, "material"),
                        Color = ExtractStringProp(compJson, "color") ?? "0.8 0.8 0.8 1.0",
                        NormalColor = ExtractStringProp(compJson, "normalColor"),
                        HighlightedColor = ExtractStringProp(compJson, "highlightedColor"),
                        PressedColor = ExtractStringProp(compJson, "pressedColor"),
                        SelectedColor = ExtractStringProp(compJson, "selectedColor"),
                        DisabledColor = ExtractStringProp(compJson, "disabledColor"),
                        ColorMultiplier = ExtractFloatProp(compJson, "colorMultiplier", 1f),
                        FadeIn = ExtractFloatProp(compJson, "fadeIn", 0f)
                    };
                    return btn;
                }
                case "UnityEngine.UI.InputField":
                {
                    var input = new CuiInputFieldComponent
                    {
                        Text = ExtractStringProp(compJson, "text") ?? "",
                        FontSize = ExtractIntProp(compJson, "fontSize", 14),
                        Font = ExtractStringProp(compJson, "font") ?? "RobotoCondensed-Bold.ttf",
                        Color = ExtractStringProp(compJson, "color") ?? "1.0 1.0 1.0 1.0",
                        CharsLimit = ExtractIntProp(compJson, "characterLimit", 0),
                        Command = ExtractStringProp(compJson, "command") ?? "",
                        ReadOnly = ExtractBoolProp(compJson, "readOnly", false),
                        PlaceholderId = ExtractStringProp(compJson, "placeholderId"),
                        IsPassword = ExtractBoolProp(compJson, "password", false),
                        NeedsKeyboard = ExtractBoolProp(compJson, "needsKeyboard", false),
                        HudMenuInput = ExtractBoolProp(compJson, "hudMenuInput", false),
                        Autofocus = ExtractBoolProp(compJson, "autofocus", false),
                        FadeIn = ExtractFloatProp(compJson, "fadeIn", 0f)
                    };
                    return input;
                }
                case "Countdown":
                {
                    var count = new CuiCountdownComponent
                    {
                        EndTime = ExtractFloatProp(compJson, "endTime", 0f),
                        StartTime = ExtractFloatProp(compJson, "startTime", 60f),
                        Step = ExtractFloatProp(compJson, "step", 1f),
                        Interval = ExtractFloatProp(compJson, "interval", 1f),
                        NumberFormat = ExtractStringProp(compJson, "numberFormat") ?? "0.####",
                        DestroyIfDone = ExtractBoolProp(compJson, "destroyIfDone", true),
                        Command = ExtractStringProp(compJson, "command") ?? "",
                        FadeIn = ExtractFloatProp(compJson, "fadeIn", 0f)
                    };
                    return count;
                }
                case "UnityEngine.UI.Outline":
                {
                    return new CuiOutlineComponent
                    {
                        Color = ExtractStringProp(compJson, "color") ?? "0.0 0.0 0.0 1.0",
                        Distance = ExtractStringProp(compJson, "distance") ?? "1.0 -1.0",
                        UseGraphicAlpha = ExtractBoolProp(compJson, "useGraphicAlpha", true)
                    };
                }
                case "UnityEngine.UI.ScrollView":
                {
                    return new CuiScrollViewComponent
                    {
                        Horizontal = ExtractBoolProp(compJson, "horizontal", false),
                        Vertical = ExtractBoolProp(compJson, "vertical", true),
                        Elasticity = ExtractFloatProp(compJson, "elasticity", 0.1f),
                        Inertia = ExtractBoolProp(compJson, "inertia", true),
                        DecelerationRate = ExtractFloatProp(compJson, "decelerationRate", 0.135f),
                        ScrollSensitivity = ExtractFloatProp(compJson, "scrollSensitivity", 20f)
                    };
                }
                case "UnityEngine.UI.CanvasGroup":
                {
                    return new CuiCanvasGroupComponent
                    {
                        Alpha = ExtractFloatProp(compJson, "alpha", 1f),
                        BlocksRaycasts = ExtractBoolProp(compJson, "blocksRaycasts", true),
                        Interactable = ExtractBoolProp(compJson, "interactable", true),
                        Fade = ExtractStringProp(compJson, "fade")
                    };
                }
                case "UnityEngine.UI.Mask":
                {
                    return new CuiMaskComponent
                    {
                        ShowMaskGraphic = ExtractBoolProp(compJson, "showMaskGraphic", false)
                    };
                }
                case "NeedsCursor":
                    return new CuiNeedsCursorComponent();
                case "NeedsKeyboard":
                    return new CuiNeedsKeyboardComponent();
                case "UnityEngine.UI.HorizontalLayoutGroup":
                    return new CuiHorizontalLayoutGroupComponent
                    {
                        Spacing = ExtractFloatProp(compJson, "spacing", 0f),
                        Padding = ExtractStringProp(compJson, "padding") ?? "0 0 0 0"
                    };
                case "UnityEngine.UI.VerticalLayoutGroup":
                    return new CuiVerticalLayoutGroupComponent
                    {
                        Spacing = ExtractFloatProp(compJson, "spacing", 0f),
                        Padding = ExtractStringProp(compJson, "padding") ?? "0 0 0 0"
                    };
                case "UnityEngine.UI.GridLayoutGroup":
                    return new CuiGridLayoutGroupComponent
                    {
                        CellSize = ExtractStringProp(compJson, "cellSize") ?? "100 100",
                        Spacing = ExtractStringProp(compJson, "spacing") ?? "0 0",
                        Padding = ExtractStringProp(compJson, "padding") ?? "0 0 0 0"
                    };
                case "UnityEngine.UI.ContentSizeFitter":
                    return new CuiContentSizeFitterComponent();
                case "UnityEngine.UI.LayoutElement":
                    return new CuiLayoutElementComponent
                    {
                        PreferredWidth = ExtractFloatProp(compJson, "preferredWidth", -1f),
                        PreferredHeight = ExtractFloatProp(compJson, "preferredHeight", -1f)
                    };
                case "Tooltip":
                    return new CuiTooltipComponent
                    {
                        Text = ExtractStringProp(compJson, "text") ?? "Tooltip",
                        Offset = ExtractStringProp(compJson, "offset") ?? "0 0"
                    };
                case "Draggable":
                    return new CuiDraggableComponent
                    {
                        DragAlpha = ExtractFloatProp(compJson, "dragAlpha", 0.8f),
                        Filter = ExtractStringProp(compJson, "filter")
                    };
                case "Slot":
                    return new CuiSlotComponent
                    {
                        Filter = ExtractStringProp(compJson, "filter")
                    };
                default:
                    return null;
            }
        }

        private static List<string> SplitJsonArrayObjects(string jsonArray)
        {
            var objects = new List<string>();
            int depth = 0;
            int start = -1;
            bool inString = false;
            bool escaped = false;

            for (int i = 0; i < jsonArray.Length; i++)
            {
                char c = jsonArray[i];

                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (!inString)
                {
                    if (c == '{')
                    {
                        if (depth == 0) start = i;
                        depth++;
                    }
                    else if (c == '}')
                    {
                        depth--;
                        if (depth == 0 && start != -1)
                        {
                            objects.Add(jsonArray.Substring(start, i - start + 1));
                            start = -1;
                        }
                    }
                }
            }

            return objects;
        }

        private static int FindMatchingBracket(string str, int startIdx, char openBracket, char closeBracket)
        {
            int depth = 0;
            bool inString = false;
            bool escaped = false;

            for (int i = startIdx; i < str.Length; i++)
            {
                char c = str[i];
                if (escaped) { escaped = false; continue; }
                if (c == '\\') { escaped = true; continue; }
                if (c == '"') { inString = !inString; continue; }

                if (!inString)
                {
                    if (c == openBracket) depth++;
                    else if (c == closeBracket)
                    {
                        depth--;
                        if (depth == 0) return i;
                    }
                }
            }
            return -1;
        }

        private static string ExtractStringProp(string json, string key)
        {
            var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*\"((?:\\\\\"|[^\"])*)\"", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n").Replace("\\r", "\r");
            }
            return null;
        }

        private static float ExtractFloatProp(string json, string key, float defaultVal = 0f)
        {
            var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*(-?[0-9]+(?:\\.[0-9]+)?)", RegexOptions.IgnoreCase);
            if (match.Success && float.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float val))
            {
                return val;
            }
            return defaultVal;
        }

        private static int ExtractIntProp(string json, string key, int defaultVal = 0)
        {
            var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*(-?[0-9]+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int val))
            {
                return val;
            }
            return defaultVal;
        }

        private static bool ExtractBoolProp(string json, string key, bool defaultVal = false)
        {
            var match = Regex.Match(json, $"\"{key}\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return string.Equals(match.Groups[1].Value, "true", StringComparison.OrdinalIgnoreCase);
            }
            return defaultVal;
        }
    }
}
