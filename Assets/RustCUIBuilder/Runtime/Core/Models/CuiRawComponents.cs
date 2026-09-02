using System;
using UnityEngine;
using UnityEngine.UI;

namespace RustCUIBuilder.Runtime.Core.Models
{
    public enum CuiTimerFormat
    {
        None,
        SecondsHundreth,
        MinutesSeconds,
        MinutesSecondsHundreth,
        HoursMinutes,
        HoursMinutesSeconds,
        HoursMinutesSecondsMilliseconds,
        HoursMinutesSecondsTenths,
        DaysHoursMinutes,
        DaysHoursMinutesSeconds,
        Custom
    }

    public enum CuiTooltipType
    {
        Default,
        AlwaysOnTop,
        AlwaysOnTopEmoji
    }

    public enum CuiTooltipDelay
    {
        Short,
        Long
    }

    public enum CuiTooltipPosition
    {
        Auto,
        Top,
        Bottom,
        Left,
        Right,
        TopLeft
    }

    public enum CuiDraggablePositionSendType
    {
        NormalizedScreen,
        NormalizedParent,
        Relative,
        RelativeAnchor
    }

    [Serializable]
    public class CuiRectTransformComponent : ICuiComponent
    {
        public string Type => "RectTransform";

        public string AnchorMin { get; set; } = "0.0 0.0";
        public string AnchorMax { get; set; } = "1.0 1.0";
        public string OffsetMin { get; set; } = "0.0 0.0";
        public string OffsetMax { get; set; } = "0.0 0.0";
        public float Rotation { get; set; } = 0.0f;
        public string Pivot { get; set; } = "0.5 0.5";
        public string SetParent { get; set; }
        public int SetTransformIndex { get; set; } = -1;

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiTextComponent : ICuiComponent, ICuiColor, ICuiEnableable, ICuiGraphic
    {
        public string Type => "UnityEngine.UI.Text";

        public string Text { get; set; } = "New Label";
        public int FontSize { get; set; } = 14;
        public string Font { get; set; } = "RobotoCondensed-Bold.ttf";
        public TextAnchor Align { get; set; } = TextAnchor.UpperLeft;
        public string Color { get; set; } = "1.0 1.0 1.0 1.0";
        public VerticalWrapMode VerticalOverflow { get; set; } = VerticalWrapMode.Truncate;
        public float FadeIn { get; set; } = 0.0f;
        public string PlaceholderParentId { get; set; }
        public bool? BlocksRaycast { get; set; }
        public bool? Enabled { get; set; } = true;

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiImageComponent : ICuiComponent, ICuiColor, ICuiEnableable, ICuiGraphic
    {
        public string Type => "UnityEngine.UI.Image";

        public string Sprite { get; set; } = "assets/content/ui/ui.background.tile.psd";
        public string Material { get; set; }
        public string Color { get; set; } = "1.0 1.0 1.0 1.0";
        public Image.Type ImageType { get; set; } = Image.Type.Simple;
        public bool? FillCenter { get; set; } = true;
        public string Png { get; set; }
        public string Slice { get; set; }
        public int ItemId { get; set; } = 0;
        public ulong SkinId { get; set; } = 0;
        public float PixelsPerUnitMultiplier { get; set; } = 1.0f;
        public float FadeIn { get; set; } = 0.0f;
        public string PlaceholderParentId { get; set; }
        public bool? BlocksRaycast { get; set; }
        public bool? Enabled { get; set; } = true;

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiRawImageComponent : ICuiComponent, ICuiColor, ICuiEnableable, ICuiGraphic
    {
        public string Type => "UnityEngine.UI.RawImage";

        public string Sprite { get; set; }
        public string Color { get; set; } = "1.0 1.0 1.0 1.0";
        public string Material { get; set; }
        public string Url { get; set; }
        public string Png { get; set; }
        public string SteamId { get; set; }
        public float FadeIn { get; set; } = 0.0f;
        public string PlaceholderParentId { get; set; }
        public bool? BlocksRaycast { get; set; }
        public bool? Enabled { get; set; } = true;

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiButtonComponent : ICuiComponent, ICuiColor, ICuiEnableable, ICuiGraphic
    {
        public string Type => "UnityEngine.UI.Button";

        public string Command { get; set; } = "";
        public string Close { get; set; } = "";
        public string Sprite { get; set; } = "assets/content/ui/ui.background.tile.psd";
        public string Material { get; set; }
        public string Color { get; set; } = "0.8 0.8 0.8 1.0";
        public Image.Type ImageType { get; set; } = Image.Type.Simple;
        public string NormalColor { get; set; }
        public string HighlightedColor { get; set; }
        public string PressedColor { get; set; }
        public string SelectedColor { get; set; }
        public string DisabledColor { get; set; }
        public float ColorMultiplier { get; set; } = 1.0f;
        public float? FadeDuration { get; set; } = 0.1f;
        public bool? Interactable { get; set; } = true;
        public float FadeIn { get; set; } = 0.0f;
        public string PlaceholderParentId { get; set; }
        public bool? BlocksRaycast { get; set; }
        public bool? Enabled { get; set; } = true;

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiInputFieldComponent : ICuiComponent, ICuiColor, ICuiEnableable, ICuiGraphic
    {
        public string Type => "UnityEngine.UI.InputField";

        public string Text { get; set; } = "";
        public int FontSize { get; set; } = 14;
        public string Font { get; set; } = "RobotoCondensed-Bold.ttf";
        public TextAnchor Align { get; set; } = TextAnchor.MiddleLeft;
        public string Color { get; set; } = "1.0 1.0 1.0 1.0";
        public int CharsLimit { get; set; } = 0;
        public string Command { get; set; } = "";
        public InputField.LineType LineType { get; set; } = InputField.LineType.SingleLine;
        public bool ReadOnly { get; set; } = false;
        public string PlaceholderId { get; set; }
        public bool IsPassword { get; set; } = false;
        public bool NeedsKeyboard { get; set; } = false;
        public bool HudMenuInput { get; set; } = false;
        public bool Autofocus { get; set; } = false;
        public bool? Interactable { get; set; } = true;
        public float FadeIn { get; set; } = 0.0f;
        public string PlaceholderParentId { get; set; }
        public bool? BlocksRaycast { get; set; }
        public bool? Enabled { get; set; } = true;

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiCountdownComponent : ICuiComponent, ICuiEnableable
    {
        public string Type => "Countdown";

        public float EndTime { get; set; } = 0.0f;
        public float StartTime { get; set; } = 60.0f;
        public float Step { get; set; } = 1.0f;
        public float Interval { get; set; } = 1.0f;
        public CuiTimerFormat TimerFormat { get; set; } = CuiTimerFormat.None;
        public string NumberFormat { get; set; } = "0.####";
        public bool DestroyIfDone { get; set; } = true;
        public string Command { get; set; } = "";
        public float FadeIn { get; set; } = 0.0f;
        public bool? Enabled { get; set; } = true;

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiOutlineComponent : ICuiComponent, ICuiColor, ICuiEnableable
    {
        public string Type => "UnityEngine.UI.Outline";

        public string Color { get; set; } = "0.0 0.0 0.0 1.0";
        public string Distance { get; set; } = "1.0 -1.0";
        public bool UseGraphicAlpha { get; set; } = true;
        public bool? Enabled { get; set; } = true;

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiScrollbar
    {
        public bool? AutoHide { get; set; } = true;
        public float? Size { get; set; } = 20.0f;
        public string HandleColor { get; set; }
        public string HighlightColor { get; set; }
        public string PressedColor { get; set; }
        public string TrackColor { get; set; }
        public string HandleSprite { get; set; }
        public string TrackSprite { get; set; }
        public bool? Invert { get; set; }
        public float? FadeDuration { get; set; }
        public bool? Enabled { get; set; } = true;

        public CuiScrollbar Clone() => (CuiScrollbar)MemberwiseClone();
    }

    [Serializable]
    public class CuiScrollViewComponent : ICuiComponent, ICuiEnableable
    {
        public string Type => "UnityEngine.UI.ScrollView";

        public CuiRectTransformComponent ContentTransform { get; set; }
        public bool Horizontal { get; set; } = false;
        public bool Vertical { get; set; } = true;
        public ScrollRect.MovementType MovementType { get; set; } = ScrollRect.MovementType.Clamped;
        public float Elasticity { get; set; } = 0.1f;
        public bool Inertia { get; set; } = true;
        public float DecelerationRate { get; set; } = 0.135f;
        public float ScrollSensitivity { get; set; } = 20.0f;
        public CuiScrollbar HorizontalScrollbar { get; set; }
        public CuiScrollbar VerticalScrollbar { get; set; }
        public float? HorizontalNormalizedPosition { get; set; }
        public float? VerticalNormalizedPosition { get; set; } = 1.0f;
        public bool? Enabled { get; set; } = true;

        public ICuiComponent Clone()
        {
            var copy = (CuiScrollViewComponent)MemberwiseClone();
            if (ContentTransform != null) copy.ContentTransform = (CuiRectTransformComponent)ContentTransform.Clone();
            if (HorizontalScrollbar != null) copy.HorizontalScrollbar = HorizontalScrollbar.Clone();
            if (VerticalScrollbar != null) copy.VerticalScrollbar = VerticalScrollbar.Clone();
            return copy;
        }
    }

    [Serializable]
    public class CuiCanvasGroupComponent : ICuiComponent
    {
        public string Type => "UnityEngine.UI.CanvasGroup";

        public float? Alpha { get; set; } = 1.0f;
        public bool? BlocksRaycasts { get; set; } = true;
        public bool? Interactable { get; set; } = true;
        public string Fade { get; set; }

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiMaskComponent : ICuiComponent, ICuiEnableable
    {
        public string Type => "UnityEngine.UI.Mask";

        public bool? ShowMaskGraphic { get; set; } = false;
        public bool? Enabled { get; set; } = true;

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiNeedsCursorComponent : ICuiComponent, ICuiEnableable
    {
        public string Type => "NeedsCursor";
        public bool? Enabled { get; set; } = true;

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiNeedsKeyboardComponent : ICuiComponent, ICuiEnableable
    {
        public string Type => "NeedsKeyboard";
        public bool? Enabled { get; set; } = true;

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public abstract class CuiLayoutGroupComponent : ICuiComponent, ICuiEnableable
    {
        public abstract string Type { get; }

        public float Spacing { get; set; } = 0.0f;
        public TextAnchor ChildAlignment { get; set; } = TextAnchor.UpperLeft;
        public bool? ChildForceExpandWidth { get; set; } = true;
        public bool? ChildForceExpandHeight { get; set; } = true;
        public bool? ChildControlWidth { get; set; } = true;
        public bool? ChildControlHeight { get; set; } = true;
        public bool? ChildScaleWidth { get; set; } = false;
        public bool? ChildScaleHeight { get; set; } = false;
        public string Padding { get; set; } = "0 0 0 0";
        public bool? Enabled { get; set; } = true;

        public abstract ICuiComponent Clone();
    }

    [Serializable]
    public class CuiHorizontalLayoutGroupComponent : CuiLayoutGroupComponent
    {
        public override string Type => "UnityEngine.UI.HorizontalLayoutGroup";
        public override ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiVerticalLayoutGroupComponent : CuiLayoutGroupComponent
    {
        public override string Type => "UnityEngine.UI.VerticalLayoutGroup";
        public override ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiGridLayoutGroupComponent : ICuiComponent, ICuiEnableable
    {
        public string Type => "UnityEngine.UI.GridLayoutGroup";

        public string CellSize { get; set; } = "100 100";
        public string Spacing { get; set; } = "0 0";
        public GridLayoutGroup.Corner StartCorner { get; set; } = GridLayoutGroup.Corner.UpperLeft;
        public GridLayoutGroup.Axis StartAxis { get; set; } = GridLayoutGroup.Axis.Horizontal;
        public TextAnchor ChildAlignment { get; set; } = TextAnchor.UpperLeft;
        public GridLayoutGroup.Constraint Constraint { get; set; } = GridLayoutGroup.Constraint.Flexible;
        public int ConstraintCount { get; set; } = 1;
        public string Padding { get; set; } = "0 0 0 0";
        public bool? Enabled { get; set; } = true;

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiContentSizeFitterComponent : ICuiComponent, ICuiEnableable
    {
        public string Type => "UnityEngine.UI.ContentSizeFitter";

        public ContentSizeFitter.FitMode HorizontalFit { get; set; } = ContentSizeFitter.FitMode.Unconstrained;
        public ContentSizeFitter.FitMode VerticalFit { get; set; } = ContentSizeFitter.FitMode.Unconstrained;
        public bool? Enabled { get; set; } = true;

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiLayoutElementComponent : ICuiComponent, ICuiEnableable
    {
        public string Type => "UnityEngine.UI.LayoutElement";

        public float PreferredWidth { get; set; } = -1.0f;
        public float PreferredHeight { get; set; } = -1.0f;
        public float MinWidth { get; set; } = -1.0f;
        public float MinHeight { get; set; } = -1.0f;
        public float FlexibleWidth { get; set; } = -1.0f;
        public float FlexibleHeight { get; set; } = -1.0f;
        public bool? IgnoreLayout { get; set; } = false;
        public bool? Enabled { get; set; } = true;

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiTooltipComponent : ICuiComponent, ICuiEnableable
    {
        public string Type => "Tooltip";

        public string Text { get; set; } = "Helpful tooltip text";
        public CuiTooltipType TooltipType { get; set; } = CuiTooltipType.Default;
        public string Offset { get; set; } = "0.0 0.0";
        public bool? UseCentre { get; set; }
        public CuiTooltipDelay Delay { get; set; } = CuiTooltipDelay.Short;
        public CuiTooltipPosition Position { get; set; } = CuiTooltipPosition.Auto;
        public bool? Enabled { get; set; } = true;

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiDraggableComponent : ICuiComponent, ICuiEnableable
    {
        public string Type => "Draggable";

        public bool? LimitToParent { get; set; } = false;
        public float MaxDistance { get; set; } = -1.0f;
        public bool? AllowSwapping { get; set; } = false;
        public bool? DropAnywhere { get; set; } = true;
        public float DragAlpha { get; set; } = 0.8f;
        public int ParentLimitIndex { get; set; } = 0;
        public string Filter { get; set; }
        public string ParentPadding { get; set; } = "0 0";
        public string AnchorOffset { get; set; } = "0 0";
        public bool? KeepOnTop { get; set; } = false;
        public CuiDraggablePositionSendType PositionRPC { get; set; } = CuiDraggablePositionSendType.Relative;
        public bool MoveToAnchor { get; set; } = true;
        public bool RebuildAnchor { get; set; } = true;
        public bool? Enabled { get; set; } = true;

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }

    [Serializable]
    public class CuiSlotComponent : ICuiComponent, ICuiEnableable
    {
        public string Type => "Slot";

        public string Filter { get; set; }
        public bool? Enabled { get; set; } = true;

        public ICuiComponent Clone() => (ICuiComponent)MemberwiseClone();
    }
}
