using System;
using System.Collections.Generic;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Discovery;
using RustCUIBuilder.Runtime.Rendering.Components;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RustCUIBuilder.Runtime.Rendering.Canvas
{
    /// <summary>
    /// Master in-engine viewport renderer for Rust CUI documents.
    /// Replicates Rust CommunityEntity client rendering with exact layer ordering,
    /// uGUI component translation, and resolution simulation.
    /// </summary>
    [ExecuteAlways]
    public class RustViewportRenderer : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Canvas _canvas;
        [SerializeField] private CanvasScaler _scaler;
        [SerializeField] private RectTransform _viewportRoot;

        private readonly Dictionary<string, RectTransform> _layerRoots = new Dictionary<string, RectTransform>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GameObject> _elementGameObjects = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

        private CuiDocument _currentDocument;

        public event Action<string> OnElementClicked;

        private void Awake()
        {
            EnsureCanvasSetup();
        }

        public void EnsureCanvasSetup()
        {
            if (_canvas == null)
            {
                _canvas = GetComponentInChildren<UnityEngine.Canvas>(true);
                if (_canvas == null)
                {
                    var canvasGo = new GameObject("RustCuiCanvas", typeof(UnityEngine.Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    canvasGo.transform.SetParent(transform, false);
                    _canvas = canvasGo.GetComponent<UnityEngine.Canvas>();
                }
            }

            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            if (_scaler == null)
            {
                _scaler = _canvas.GetComponent<CanvasScaler>();
                if (_scaler == null) _scaler = _canvas.gameObject.AddComponent<CanvasScaler>();
            }

            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(RustCanvasScaler.ReferenceWidth, RustCanvasScaler.ReferenceHeight);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _scaler.matchWidthOrHeight = 0.5f;

            if (_viewportRoot == null)
            {
                var vp = _canvas.transform.Find("ViewportRoot");
                if (vp == null)
                {
                    var vpGo = new GameObject("ViewportRoot", typeof(RectTransform));
                    vpGo.transform.SetParent(_canvas.transform, false);
                    _viewportRoot = vpGo.GetComponent<RectTransform>();
                    _viewportRoot.anchorMin = Vector2.zero;
                    _viewportRoot.anchorMax = Vector2.one;
                    _viewportRoot.offsetMin = Vector2.zero;
                    _viewportRoot.offsetMax = Vector2.zero;
                }
                else
                {
                    _viewportRoot = vp.GetComponent<RectTransform>();
                }
            }

            EnsureLayerRoots();
        }

        private void EnsureLayerRoots()
        {
            _layerRoots.Clear();
            foreach (var layerName in RustAssetDiscovery.VerifiedLayers)
            {
                var layerTrans = _viewportRoot.Find(layerName) as RectTransform;
                if (layerTrans == null)
                {
                    var layerGo = new GameObject(layerName, typeof(RectTransform));
                    layerGo.transform.SetParent(_viewportRoot, false);
                    layerTrans = layerGo.GetComponent<RectTransform>();
                    layerTrans.anchorMin = Vector2.zero;
                    layerTrans.anchorMax = Vector2.one;
                    layerTrans.offsetMin = Vector2.zero;
                    layerTrans.offsetMax = Vector2.zero;
                }
                _layerRoots[layerName] = layerTrans;
            }
        }

        public void RenderDocument(CuiDocument doc)
        {
            _currentDocument = doc;
            EnsureCanvasSetup();

            // Clear previous element GameObjects
            foreach (var kvp in _elementGameObjects)
            {
                if (kvp.Value != null)
                {
                    if (Application.isPlaying) Destroy(kvp.Value);
                    else DestroyImmediate(kvp.Value);
                }
            }
            _elementGameObjects.Clear();

            if (doc == null || doc.Elements == null) return;

            // Render elements in hierarchical order
            foreach (var elem in doc.Elements)
            {
                RenderElement(elem);
            }
        }

        public GameObject GetGameObjectForElement(string elementId)
        {
            if (string.IsNullOrEmpty(elementId)) return null;
            _elementGameObjects.TryGetValue(elementId, out var go);
            return go;
        }

        private void RenderElement(CuiElementNode elem)
        {
            Transform parentTransform = null;

            // 1. Resolve parent transform
            if (!string.IsNullOrEmpty(elem.Parent))
            {
                if (_layerRoots.TryGetValue(elem.Parent, out var layerRoot))
                {
                    parentTransform = layerRoot;
                }
                else
                {
                    var parentElem = _currentDocument.FindByName(elem.Parent);
                    if (parentElem != null && _elementGameObjects.TryGetValue(parentElem.Id, out var parentGo))
                    {
                        parentTransform = parentGo.transform;
                    }
                }
            }

            if (parentTransform == null)
            {
                if (_layerRoots.TryGetValue("Overlay", out var overlayRoot))
                    parentTransform = overlayRoot;
                else
                    parentTransform = _viewportRoot;
            }

            // 2. Create GameObject for this element
            var go = new GameObject(elem.Name, typeof(RectTransform));
            go.transform.SetParent(parentTransform, false);
            _elementGameObjects[elem.Id] = go;

            var rt = go.GetComponent<RectTransform>();

            // Apply RectTransform
            var rectComp = elem.GetComponent<CuiRectTransformComponent>() ?? new CuiRectTransformComponent();
            RustCanvasScaler.ApplyToRectTransform(rt, rectComp.AnchorMin, rectComp.AnchorMax, rectComp.OffsetMin, rectComp.OffsetMax, rectComp.Pivot, rectComp.Rotation);

            // Apply Graphic / Image Component
            var imgComp = elem.GetComponent<CuiImageComponent>();
            if (imgComp != null)
            {
                var img = go.AddComponent<Image>();
                img.color = CuiColorExtensions.ToUnityColor(imgComp.Color, Color.white);
                img.type = imgComp.ImageType;
                img.fillCenter = imgComp.FillCenter ?? true;
                img.raycastTarget = imgComp.BlocksRaycast ?? true;

                if (imgComp.ItemId != 0)
                {
                    var item = RustAssetDiscovery.FindItemById(imgComp.ItemId);
                    if (item != null)
                    {
                        var sprite = RustAssetDiscovery.LoadItemIcon(item);
                        if (sprite != null) img.sprite = sprite;
                    }
                }
            }

            // Apply RawImage Component
            var rawComp = elem.GetComponent<CuiRawImageComponent>();
            if (rawComp != null)
            {
                var raw = go.AddComponent<RawImage>();
                raw.color = CuiColorExtensions.ToUnityColor(rawComp.Color, Color.white);
                raw.raycastTarget = rawComp.BlocksRaycast ?? true;
            }

            // Apply Text Component
            var textComp = elem.GetComponent<CuiTextComponent>();
            if (textComp != null)
            {
                var txt = go.AddComponent<TextMeshProUGUI>();
                txt.text = textComp.Text;
                txt.fontSize = textComp.FontSize;
                txt.color = CuiColorExtensions.ToUnityColor(textComp.Color, Color.white);
                txt.alignment = ConvertTextAnchorToTmp(textComp.Align);
                txt.textWrappingMode = textComp.VerticalOverflow == VerticalWrapMode.Truncate ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                txt.raycastTarget = textComp.BlocksRaycast ?? false;
            }

            // Apply Button Component
            var btnComp = elem.GetComponent<CuiButtonComponent>();
            if (btnComp != null)
            {
                var btn = go.AddComponent<Button>();
                btn.interactable = btnComp.Interactable ?? true;

                if (!string.IsNullOrEmpty(btnComp.NormalColor))
                {
                    var colors = btn.colors;
                    colors.normalColor = CuiColorExtensions.ToUnityColor(btnComp.NormalColor, colors.normalColor);
                    if (!string.IsNullOrEmpty(btnComp.HighlightedColor)) colors.highlightedColor = CuiColorExtensions.ToUnityColor(btnComp.HighlightedColor, colors.highlightedColor);
                    if (!string.IsNullOrEmpty(btnComp.PressedColor)) colors.pressedColor = CuiColorExtensions.ToUnityColor(btnComp.PressedColor, colors.pressedColor);
                    if (!string.IsNullOrEmpty(btnComp.SelectedColor)) colors.selectedColor = CuiColorExtensions.ToUnityColor(btnComp.SelectedColor, colors.selectedColor);
                    if (!string.IsNullOrEmpty(btnComp.DisabledColor)) colors.disabledColor = CuiColorExtensions.ToUnityColor(btnComp.DisabledColor, colors.disabledColor);
                    btn.colors = colors;
                }

                if (imgComp == null && rawComp == null)
                {
                    var img = go.AddComponent<Image>();
                    img.color = CuiColorExtensions.ToUnityColor(btnComp.Color, new Color(0.8f, 0.8f, 0.8f, 1f));
                    img.type = btnComp.ImageType;
                    btn.targetGraphic = img;
                }

                btn.onClick.AddListener(() => OnElementClicked?.Invoke(elem.Id));
            }

            // Apply Outline Component
            var outlineComp = elem.GetComponent<CuiOutlineComponent>();
            if (outlineComp != null)
            {
                var outline = go.AddComponent<Outline>();
                outline.effectColor = CuiColorExtensions.ToUnityColor(outlineComp.Color, Color.black);
                outline.effectDistance = RustCanvasScaler.ParseVector2(outlineComp.Distance, new Vector2(1f, -1f));
                outline.useGraphicAlpha = outlineComp.UseGraphicAlpha;
            }

            // Apply CanvasGroup Component
            var cgComp = elem.GetComponent<CuiCanvasGroupComponent>();
            if (cgComp != null)
            {
                var cg = go.AddComponent<CanvasGroup>();
                cg.alpha = cgComp.Alpha ?? 1f;
                cg.blocksRaycasts = cgComp.BlocksRaycasts ?? true;
                cg.interactable = cgComp.Interactable ?? true;
            }

            // Apply Mask Component
            var maskComp = elem.GetComponent<CuiMaskComponent>();
            if (maskComp != null)
            {
                var mask = go.AddComponent<Mask>();
                mask.showMaskGraphic = maskComp.ShowMaskGraphic ?? false;
            }

            // Apply Countdown Component
            var countdownComp = elem.GetComponent<CuiCountdownComponent>();
            if (countdownComp != null)
            {
                var countdown = go.AddComponent<RustCountdownRuntime>();
                countdown.startTime = countdownComp.StartTime;
                countdown.endTime = countdownComp.EndTime;
                countdown.step = countdownComp.Step;
                countdown.interval = countdownComp.Interval;
                countdown.timerFormat = countdownComp.TimerFormat;
                countdown.numberFormat = countdownComp.NumberFormat;
                countdown.destroyIfDone = countdownComp.DestroyIfDone;
            }

            // Apply Layout Groups
            var hlg = elem.GetComponent<CuiHorizontalLayoutGroupComponent>();
            if (hlg != null)
            {
                var lg = go.AddComponent<HorizontalLayoutGroup>();
                lg.spacing = hlg.Spacing;
                lg.childAlignment = hlg.ChildAlignment;
                lg.childForceExpandWidth = hlg.ChildForceExpandWidth ?? true;
                lg.childForceExpandHeight = hlg.ChildForceExpandHeight ?? true;
                lg.childControlWidth = hlg.ChildControlWidth ?? true;
                lg.childControlHeight = hlg.ChildControlHeight ?? true;
                lg.childScaleWidth = hlg.ChildScaleWidth ?? false;
                lg.childScaleHeight = hlg.ChildScaleHeight ?? false;
            }

            var vlg = elem.GetComponent<CuiVerticalLayoutGroupComponent>();
            if (vlg != null)
            {
                var lg = go.AddComponent<VerticalLayoutGroup>();
                lg.spacing = vlg.Spacing;
                lg.childAlignment = vlg.ChildAlignment;
                lg.childForceExpandWidth = vlg.ChildForceExpandWidth ?? true;
                lg.childForceExpandHeight = vlg.ChildForceExpandHeight ?? true;
                lg.childControlWidth = vlg.ChildControlWidth ?? true;
                lg.childControlHeight = vlg.ChildControlHeight ?? true;
                lg.childScaleWidth = vlg.ChildScaleWidth ?? false;
                lg.childScaleHeight = vlg.ChildScaleHeight ?? false;
            }

            var glg = elem.GetComponent<CuiGridLayoutGroupComponent>();
            if (glg != null)
            {
                var grid = go.AddComponent<GridLayoutGroup>();
                grid.cellSize = RustCanvasScaler.ParseVector2(glg.CellSize, new Vector2(100, 100));
                grid.spacing = RustCanvasScaler.ParseVector2(glg.Spacing, Vector2.zero);
                grid.startCorner = glg.StartCorner;
                grid.startAxis = glg.StartAxis;
                grid.childAlignment = glg.ChildAlignment;
                grid.constraint = glg.Constraint;
                grid.constraintCount = glg.ConstraintCount;
            }

            // Apply Active State
            if (elem.ActiveSelf.HasValue)
            {
                go.SetActive(elem.ActiveSelf.Value);
            }
            if (elem.IsHidden)
            {
                go.SetActive(false);
            }
        }

        private static TextAlignmentOptions ConvertTextAnchorToTmp(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.MidlineLeft;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Midline;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.MidlineRight;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.TopLeft;
            }
        }
    }
}
