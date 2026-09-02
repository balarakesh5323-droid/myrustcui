using System;
using System.Collections.Generic;
using System.Linq;
using RustCUIBuilder.Runtime.Core.Models;
using UnityEngine;

namespace RustCUIBuilder.Runtime.Core.Registry
{
    public enum ComponentCategory
    {
        Layout,
        Graphic,
        Interaction,
        Advanced,
        Utility
    }

    public class PropertyDefinition
    {
        public string PropertyName { get; set; }
        public string JsonKey { get; set; }
        public Type PropertyType { get; set; }
        public object DefaultValue { get; set; }
        public string Description { get; set; }
        public bool IsRequired { get; set; }
    }

    public class CuiComponentDefinition
    {
        public string TypeName { get; set; }
        public string DisplayName { get; set; }
        public ComponentCategory Category { get; set; }
        public string Description { get; set; }
        public string EvidenceSource { get; set; }
        public Type ComponentType { get; set; }
        public List<PropertyDefinition> Properties { get; set; } = new List<PropertyDefinition>();
        public Func<ICuiComponent> Factory { get; set; }
    }

    /// <summary>
    /// Data-driven component registry maintaining all 21 verified Rust Oxide CUI component types,
    /// their metadata, validation schemas, and factory constructors.
    /// </summary>
    public static class CuiComponentRegistry
    {
        private static readonly Dictionary<string, CuiComponentDefinition> RegistryByType = new Dictionary<string, CuiComponentDefinition>(StringComparer.OrdinalIgnoreCase);

        static CuiComponentRegistry()
        {
            RegisterAllDefinitions();
        }

        public static IReadOnlyCollection<CuiComponentDefinition> AllDefinitions => RegistryByType.Values;

        public static CuiComponentDefinition GetDefinition(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            RegistryByType.TryGetValue(typeName, out var def);
            return def;
        }

        public static IEnumerable<CuiComponentDefinition> GetByCategory(ComponentCategory category)
        {
            return RegistryByType.Values.Where(d => d.Category == category);
        }

        public static ICuiComponent CreateComponent(string typeName)
        {
            var def = GetDefinition(typeName);
            return def?.Factory?.Invoke();
        }

        public static CuiElementNode CreatePresetElement(string presetName, string parent = "Overlay")
        {
            switch (presetName.ToLowerInvariant())
            {
                case "panel":
                {
                    var node = new CuiElementNode("Panel", parent);
                    node.Components.Add(new CuiImageComponent
                    {
                        Color = "0.1 0.1 0.1 0.85",
                        Sprite = "assets/content/ui/ui.background.tile.psd"
                    });
                    node.Components.Add(new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2 0.2",
                        AnchorMax = "0.8 0.8"
                    });
                    return node;
                }
                case "label":
                {
                    var node = new CuiElementNode("Label", parent);
                    node.Components.Add(new CuiTextComponent
                    {
                        Text = "Rust CUI Label",
                        FontSize = 16,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1.0 1.0 1.0 1.0"
                    });
                    node.Components.Add(new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0 0.8",
                        AnchorMax = "1.0 1.0"
                    });
                    return node;
                }
                case "button":
                {
                    var node = new CuiElementNode("Button", parent);
                    node.Components.Add(new CuiButtonComponent
                    {
                        Color = "0.2 0.6 0.3 0.9",
                        Command = "myplugin.buttonclick"
                    });
                    node.Components.Add(new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3 0.1",
                        AnchorMax = "0.7 0.25"
                    });
                    return node;
                }
                case "inputfield":
                {
                    var node = new CuiElementNode("InputField", parent);
                    node.Components.Add(new CuiInputFieldComponent
                    {
                        Text = "Type here...",
                        FontSize = 14,
                        Align = TextAnchor.MiddleLeft,
                        Command = "myplugin.submitinput"
                    });
                    node.Components.Add(new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2 0.4",
                        AnchorMax = "0.8 0.5"
                    });
                    return node;
                }
                case "image":
                {
                    var node = new CuiElementNode("Image", parent);
                    node.Components.Add(new CuiImageComponent
                    {
                        Color = "1.0 1.0 1.0 1.0",
                        Sprite = "assets/icons/check.png"
                    });
                    node.Components.Add(new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4 0.4",
                        AnchorMax = "0.6 0.6"
                    });
                    return node;
                }
                case "rawimage":
                {
                    var node = new CuiElementNode("RawImage", parent);
                    node.Components.Add(new CuiRawImageComponent
                    {
                        Color = "1.0 1.0 1.0 1.0",
                        Url = "https://files.facepunch.com/garry/2015/June/03/2015-06-03_12-19-17.jpg"
                    });
                    node.Components.Add(new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1 0.1",
                        AnchorMax = "0.9 0.9"
                    });
                    return node;
                }
                case "countdown":
                {
                    var node = new CuiElementNode("Countdown", parent);
                    node.Components.Add(new CuiTextComponent
                    {
                        Text = "Time Remaining: {0}",
                        FontSize = 18,
                        Align = TextAnchor.MiddleCenter
                    });
                    node.Components.Add(new CuiCountdownComponent
                    {
                        StartTime = 60f,
                        EndTime = 0f,
                        Step = 1f,
                        Interval = 1f
                    });
                    node.Components.Add(new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2 0.45",
                        AnchorMax = "0.8 0.55"
                    });
                    return node;
                }
                case "scrollview":
                {
                    var node = new CuiElementNode("ScrollView", parent);
                    node.Components.Add(new CuiScrollViewComponent
                    {
                        Horizontal = false,
                        Vertical = true,
                        ContentTransform = new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1",
                            OffsetMin = "0 -500",
                            OffsetMax = "0 0"
                        }
                    });
                    node.Components.Add(new CuiRectTransformComponent
                    {
                        AnchorMin = "0.1 0.1",
                        AnchorMax = "0.9 0.9"
                    });
                    return node;
                }
                default:
                {
                    var node = new CuiElementNode("Element", parent);
                    node.Components.Add(new CuiRectTransformComponent());
                    return node;
                }
            }
        }

        private static void Register(CuiComponentDefinition def)
        {
            RegistryByType[def.TypeName] = def;
        }

        private static void RegisterAllDefinitions()
        {
            RegistryByType.Clear();

            // 1. RectTransform
            Register(new CuiComponentDefinition
            {
                TypeName = "RectTransform",
                DisplayName = "Rect Transform",
                Category = ComponentCategory.Layout,
                Description = "Defines element bounds, relative anchors (0.0 - 1.0), offsets in pixels (1280x720 base), pivot, and rotation.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiRectTransformComponent / Assembly-CSharp.cui",
                ComponentType = typeof(CuiRectTransformComponent),
                Factory = () => new CuiRectTransformComponent()
            });

            // 2. UnityEngine.UI.Text
            Register(new CuiComponentDefinition
            {
                TypeName = "UnityEngine.UI.Text",
                DisplayName = "Text Label",
                Category = ComponentCategory.Graphic,
                Description = "Renders text with font size, color, alignment, and wrap mode using Rust fonts.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiTextComponent / Assembly-CSharp.cui",
                ComponentType = typeof(CuiTextComponent),
                Factory = () => new CuiTextComponent()
            });

            // 3. UnityEngine.UI.Image
            Register(new CuiComponentDefinition
            {
                TypeName = "UnityEngine.UI.Image",
                DisplayName = "Image",
                Category = ComponentCategory.Graphic,
                Description = "Renders 2D sprites, item icons (itemid/skinid), sliced backgrounds, and material effects.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiImageComponent / Assembly-CSharp.cui",
                ComponentType = typeof(CuiImageComponent),
                Factory = () => new CuiImageComponent()
            });

            // 4. UnityEngine.UI.RawImage
            Register(new CuiComponentDefinition
            {
                TypeName = "UnityEngine.UI.RawImage",
                DisplayName = "Raw Image",
                Category = ComponentCategory.Graphic,
                Description = "Renders web images by URL, Steam player avatars by 64-bit Steam ID, or cached PNG bytes.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiRawImageComponent / Assembly-CSharp.cui",
                ComponentType = typeof(CuiRawImageComponent),
                Factory = () => new CuiRawImageComponent()
            });

            // 5. UnityEngine.UI.Button
            Register(new CuiComponentDefinition
            {
                TypeName = "UnityEngine.UI.Button",
                DisplayName = "Button",
                Category = ComponentCategory.Interaction,
                Description = "Interactive button executing console commands and closing CUI panels upon click.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiButtonComponent / Assembly-CSharp.cui",
                ComponentType = typeof(CuiButtonComponent),
                Factory = () => new CuiButtonComponent()
            });

            // 6. UnityEngine.UI.InputField
            Register(new CuiComponentDefinition
            {
                TypeName = "UnityEngine.UI.InputField",
                DisplayName = "Input Field",
                Category = ComponentCategory.Interaction,
                Description = "Text input box capturing user keyboard input and submitting text to console commands.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiInputFieldComponent / Assembly-CSharp.cui",
                ComponentType = typeof(CuiInputFieldComponent),
                Factory = () => new CuiInputFieldComponent()
            });

            // 7. Countdown
            Register(new CuiComponentDefinition
            {
                TypeName = "Countdown",
                DisplayName = "Countdown Timer",
                Category = ComponentCategory.Interaction,
                Description = "Client-side animated countdown timer with custom formatting and completion commands.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiCountdownComponent / Assembly-CSharp.CommunityEntity+Countdown",
                ComponentType = typeof(CuiCountdownComponent),
                Factory = () => new CuiCountdownComponent()
            });

            // 8. UnityEngine.UI.Outline
            Register(new CuiComponentDefinition
            {
                TypeName = "UnityEngine.UI.Outline",
                DisplayName = "Outline Effect",
                Category = ComponentCategory.Graphic,
                Description = "Applies outline shadow effect to text and graphic components.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiOutlineComponent / UnityEngine.UI.Outline",
                ComponentType = typeof(CuiOutlineComponent),
                Factory = () => new CuiOutlineComponent()
            });

            // 9. UnityEngine.UI.ScrollView
            Register(new CuiComponentDefinition
            {
                TypeName = "UnityEngine.UI.ScrollView",
                DisplayName = "Scroll View",
                Category = ComponentCategory.Layout,
                Description = "Scrollable viewport container with horizontal/vertical scrollbars and elasticity.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiScrollViewComponent / UnityEngine.UI.ScrollRect",
                ComponentType = typeof(CuiScrollViewComponent),
                Factory = () => new CuiScrollViewComponent()
            });

            // 10. UnityEngine.UI.CanvasGroup
            Register(new CuiComponentDefinition
            {
                TypeName = "UnityEngine.UI.CanvasGroup",
                DisplayName = "Canvas Group",
                Category = ComponentCategory.Advanced,
                Description = "Controls group alpha transparency, raycast blocking, and interactability for child hierarchy.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiCanvasGroupComponent / UnityEngine.UI.CanvasGroup",
                ComponentType = typeof(CuiCanvasGroupComponent),
                Factory = () => new CuiCanvasGroupComponent()
            });

            // 11. UnityEngine.UI.Mask
            Register(new CuiComponentDefinition
            {
                TypeName = "UnityEngine.UI.Mask",
                DisplayName = "Mask",
                Category = ComponentCategory.Advanced,
                Description = "Clips and masks child elements to the bounding box of this graphic.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiMaskComponent / UnityEngine.UI.Mask",
                ComponentType = typeof(CuiMaskComponent),
                Factory = () => new CuiMaskComponent()
            });

            // 12. NeedsCursor
            Register(new CuiComponentDefinition
            {
                TypeName = "NeedsCursor",
                DisplayName = "Needs Cursor",
                Category = ComponentCategory.Interaction,
                Description = "Unlocks mouse cursor so the player can interact with buttons and sliders.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiNeedsCursorComponent / Assembly-CSharp.cui",
                ComponentType = typeof(CuiNeedsCursorComponent),
                Factory = () => new CuiNeedsCursorComponent()
            });

            // 13. NeedsKeyboard
            Register(new CuiComponentDefinition
            {
                TypeName = "NeedsKeyboard",
                DisplayName = "Needs Keyboard",
                Category = ComponentCategory.Interaction,
                Description = "Directs player keyboard input to CUI input fields.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiNeedsKeyboardComponent / Assembly-CSharp.cui",
                ComponentType = typeof(CuiNeedsKeyboardComponent),
                Factory = () => new CuiNeedsKeyboardComponent()
            });

            // 14. UnityEngine.UI.HorizontalLayoutGroup
            Register(new CuiComponentDefinition
            {
                TypeName = "UnityEngine.UI.HorizontalLayoutGroup",
                DisplayName = "Horizontal Layout",
                Category = ComponentCategory.Layout,
                Description = "Automatically arranges child elements horizontally with padding and spacing.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiHorizontalLayoutGroupComponent / UnityEngine.UI.HorizontalLayoutGroup",
                ComponentType = typeof(CuiHorizontalLayoutGroupComponent),
                Factory = () => new CuiHorizontalLayoutGroupComponent()
            });

            // 15. UnityEngine.UI.VerticalLayoutGroup
            Register(new CuiComponentDefinition
            {
                TypeName = "UnityEngine.UI.VerticalLayoutGroup",
                DisplayName = "Vertical Layout",
                Category = ComponentCategory.Layout,
                Description = "Automatically arranges child elements vertically with padding and spacing.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiVerticalLayoutGroupComponent / UnityEngine.UI.VerticalLayoutGroup",
                ComponentType = typeof(CuiVerticalLayoutGroupComponent),
                Factory = () => new CuiVerticalLayoutGroupComponent()
            });

            // 16. UnityEngine.UI.GridLayoutGroup
            Register(new CuiComponentDefinition
            {
                TypeName = "UnityEngine.UI.GridLayoutGroup",
                DisplayName = "Grid Layout",
                Category = ComponentCategory.Layout,
                Description = "Arranges child elements into grid rows and columns with fixed or flexible constraints.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiGridLayoutGroupComponent / UnityEngine.UI.GridLayoutGroup",
                ComponentType = typeof(CuiGridLayoutGroupComponent),
                Factory = () => new CuiGridLayoutGroupComponent()
            });

            // 17. UnityEngine.UI.ContentSizeFitter
            Register(new CuiComponentDefinition
            {
                TypeName = "UnityEngine.UI.ContentSizeFitter",
                DisplayName = "Content Size Fitter",
                Category = ComponentCategory.Layout,
                Description = "Resizes the RectTransform to fit the size of its child content.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiContentSizeFitterComponent / UnityEngine.UI.ContentSizeFitter",
                ComponentType = typeof(CuiContentSizeFitterComponent),
                Factory = () => new CuiContentSizeFitterComponent()
            });

            // 18. UnityEngine.UI.LayoutElement
            Register(new CuiComponentDefinition
            {
                TypeName = "UnityEngine.UI.LayoutElement",
                DisplayName = "Layout Element",
                Category = ComponentCategory.Layout,
                Description = "Overrides min, preferred, and flexible sizes inside layout groups.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiLayoutElementComponent / UnityEngine.UI.LayoutElement",
                ComponentType = typeof(CuiLayoutElementComponent),
                Factory = () => new CuiLayoutElementComponent()
            });

            // 19. Tooltip
            Register(new CuiComponentDefinition
            {
                TypeName = "Tooltip",
                DisplayName = "Tooltip",
                Category = ComponentCategory.Utility,
                Description = "Displays popup tooltip text on hover with emoji support and custom positioning.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiTooltipComponent / Assembly-CSharp.CommunityEntity+TooltipType",
                ComponentType = typeof(CuiTooltipComponent),
                Factory = () => new CuiTooltipComponent()
            });

            // 20. Draggable
            Register(new CuiComponentDefinition
            {
                TypeName = "Draggable",
                DisplayName = "Draggable",
                Category = ComponentCategory.Interaction,
                Description = "Allows player to drag element across screen or into slots with server RPC callbacks.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiDraggableComponent / Assembly-CSharp.CommunityEntity+DraggablePositionSendType",
                ComponentType = typeof(CuiDraggableComponent),
                Factory = () => new CuiDraggableComponent()
            });

            // 21. Slot
            Register(new CuiComponentDefinition
            {
                TypeName = "Slot",
                DisplayName = "Drop Slot",
                Category = ComponentCategory.Interaction,
                Description = "Defines a drop target slot for Draggable elements with filter matching.",
                EvidenceSource = "Oxide.Game.Rust.Cui.CuiSlotComponent",
                ComponentType = typeof(CuiSlotComponent),
                Factory = () => new CuiSlotComponent()
            });
        }
    }
}
