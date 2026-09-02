# Rust CUI API Forensic Hallucination Audit

> **Objective**: Verify that NO hallucinated, fake, or invented properties, types, component names, or Oxide APIs exist anywhere in the project.

---

## 1. Audit Scope & Methodology
Every component in `Assets/RustCUIBuilder/Runtime/Core/Models/CuiRawComponents.cs` was cross-referenced line-by-line against the authoritative decompiled source code in:
* `C:\Users\Bala Rakesh\Documents\rustmcprag\Source_Code\Oxide.Rust\Oxide.Game.Rust.Cui\`
* `C:\Users\Bala Rakesh\Documents\rustmcprag\Source_Code\Assembly-CSharp\CommunityEntity.cs`
* `C:\Users\Bala Rakesh\Documents\rustmcprag\Source_Code\Assembly-CSharp\cui.cs`

---

## 2. Forensic Audit Findings by Component

### 1. `CuiRectTransformComponent` (Type: `"RectTransform"`)
* **Decompiled Source**: `Oxide.Game.Rust.Cui/CuiRectTransformComponent.cs` & `CuiRectTransform.cs`
* **Properties Verified**:
  * `AnchorMin` (json: `"anchormin"`, string `x y`) — ✅ VERIFIED
  * `AnchorMax` (json: `"anchormax"`, string `x y`) — ✅ VERIFIED
  * `OffsetMin` (json: `"offsetmin"`, string `x y`) — ✅ VERIFIED
  * `OffsetMax` (json: `"offsetmax"`, string `x y`) — ✅ VERIFIED
  * `Rotation` (json: `"rotation"`, float) — ✅ VERIFIED
  * `Pivot` (json: `"pivot"`, string `x y`) — ✅ VERIFIED
  * `SetParent` (json: `"setParent"`, string) — ✅ VERIFIED
  * `SetTransformIndex` (json: `"setTransformIndex"`, int) — ✅ VERIFIED
* **Hallucinations**: **0 FOUND**.

### 2. `CuiTextComponent` (Type: `"UnityEngine.UI.Text"`)
* **Decompiled Source**: `Oxide.Game.Rust.Cui/CuiTextComponent.cs`
* **Properties Verified**:
  * `Text` (json: `"text"`, string) — ✅ VERIFIED
  * `FontSize` (json: `"fontSize"`, int) — ✅ VERIFIED
  * `Font` (json: `"font"`, string) — ✅ VERIFIED
  * `Align` (json: `"align"`, `UnityEngine.TextAnchor`) — ✅ VERIFIED
  * `Color` (json: `"color"`, string) — ✅ VERIFIED
  * `VerticalOverflow` (json: `"verticalOverflow"`, `UnityEngine.VerticalWrapMode`) — ✅ VERIFIED
  * `FadeIn` (json: `"fadeIn"`, float) — ✅ VERIFIED
  * `BlocksRaycast` (json: `"blocksRaycast"`, bool?) — ✅ VERIFIED
  * `Enabled` (json: `"enabled"`, bool?) — ✅ VERIFIED
* **Hallucinations**: **0 FOUND**.

### 3. `CuiImageComponent` (Type: `"UnityEngine.UI.Image"`)
* **Decompiled Source**: `Oxide.Game.Rust.Cui/CuiImageComponent.cs`
* **Properties Verified**:
  * `Sprite` (json: `"sprite"`, string) — ✅ VERIFIED
  * `Material` (json: `"material"`, string) — ✅ VERIFIED
  * `Color` (json: `"color"`, string) — ✅ VERIFIED
  * `ImageType` (json: `"imagetype"`, `UnityEngine.UI.Image.Type`) — ✅ VERIFIED
  * `FillCenter` (json: `"fillCenter"`, bool?) — ✅ VERIFIED
  * `ItemId` (json: `"itemid"`, int) — ✅ VERIFIED
  * `Png` (json: `"png"`, string) — ✅ VERIFIED
  * `FadeIn` (json: `"fadeIn"`, float) — ✅ VERIFIED
  * `BlocksRaycast` (json: `"blocksRaycast"`, bool?) — ✅ VERIFIED
* **Hallucinations**: **0 FOUND**.

### 4. `CuiRawImageComponent` (Type: `"UnityEngine.UI.RawImage"`)
* **Decompiled Source**: `Oxide.Game.Rust.Cui/CuiRawImageComponent.cs`
* **Properties Verified**:
  * `Sprite` (json: `"sprite"`, string) — ✅ VERIFIED
  * `Color` (json: `"color"`, string) — ✅ VERIFIED
  * `Material` (json: `"material"`, string) — ✅ VERIFIED
  * `Url` (json: `"url"`, string) — ✅ VERIFIED
  * `Png` (json: `"png"`, string) — ✅ VERIFIED
  * `SteamId` (json: `"steamid"`, string) — ✅ VERIFIED
  * `FadeIn` (json: `"fadeIn"`, float) — ✅ VERIFIED
  * `BlocksRaycast` (json: `"blocksRaycast"`, bool?) — ✅ VERIFIED
* **Hallucinations**: **0 FOUND**.

### 5. `CuiButtonComponent` (Type: `"UnityEngine.UI.Button"`)
* **Decompiled Source**: `Oxide.Game.Rust.Cui/CuiButtonComponent.cs`
* **Properties Verified**:
  * `Command` (json: `"command"`, string) — ✅ VERIFIED
  * `Close` (json: `"close"`, string) — ✅ VERIFIED
  * `Sprite` (json: `"sprite"`, string) — ✅ VERIFIED
  * `Material` (json: `"material"`, string) — ✅ VERIFIED
  * `Color` (json: `"color"`, string) — ✅ VERIFIED
  * `ImageType` (json: `"imagetype"`, `UnityEngine.UI.Image.Type`) — ✅ VERIFIED
  * `FadeIn` (json: `"fadeIn"`, float) — ✅ VERIFIED
  * `BlocksRaycast` (json: `"blocksRaycast"`, bool?) — ✅ VERIFIED
* **Hallucinations**: **0 FOUND**.

### 6. `CuiInputFieldComponent` (Type: `"UnityEngine.UI.InputField"`)
* **Decompiled Source**: `Oxide.Game.Rust.Cui/CuiInputFieldComponent.cs`
* **Properties Verified**:
  * `Text` (json: `"text"`, string) — ✅ VERIFIED
  * `FontSize` (json: `"fontSize"`, int) — ✅ VERIFIED
  * `Font` (json: `"font"`, string) — ✅ VERIFIED
  * `Align` (json: `"align"`, `UnityEngine.TextAnchor`) — ✅ VERIFIED
  * `Color` (json: `"color"`, string) — ✅ VERIFIED
  * `Command` (json: `"command"`, string) — ✅ VERIFIED
  * `CharsLimit` (json: `"characterLimit"`, int) — ✅ VERIFIED
  * `LineType` (json: `"lineType"`, `UnityEngine.UI.InputField.LineType`) — ✅ VERIFIED
  * `IsPassword` (json: `"password"`, bool) — ✅ VERIFIED
  * `NeedsKeyboard` (json: `"needsKeyboard"`, bool) — ✅ VERIFIED
  * `Autofocus` (json: `"autofocus"`, bool) — ✅ VERIFIED
  * `HudMenuInput` (json: `"hudMenuInput"`, bool) — ✅ VERIFIED
  * `ReadOnly` (json: `"readOnly"`, bool) — ✅ VERIFIED
  * `FadeIn` (json: `"fadeIn"`, float) — ✅ VERIFIED
* **Hallucinations**: **0 FOUND**.

### 7. `CuiCountdownComponent` (Type: `"Countdown"`)
* **Decompiled Source**: `Oxide.Game.Rust.Cui/CuiCountdownComponent.cs` & `TimerFormat.cs`
* **Properties Verified**:
  * `StartTime` (json: `"startTime"`, float) — ✅ VERIFIED
  * `EndTime` (json: `"endTime"`, float) — ✅ VERIFIED
  * `Step` (json: `"step"`, float) — ✅ VERIFIED
  * `Interval` (json: `"interval"`, float) — ✅ VERIFIED
  * `TimerFormat` (json: `"timerFormat"`, `TimerFormat` enum) — ✅ VERIFIED
  * `Command` (json: `"command"`, string) — ✅ VERIFIED
  * `DestroyIfDone` (json: `"destroyIfDone"`, bool) — ✅ VERIFIED
* **Hallucinations**: **0 FOUND**.

### 8. `CuiOutlineComponent` (Type: `"UnityEngine.UI.Outline"`)
* **Decompiled Source**: `Oxide.Game.Rust.Cui/CuiOutlineComponent.cs`
* **Properties Verified**:
  * `Color` (json: `"color"`, string) — ✅ VERIFIED
  * `Distance` (json: `"distance"`, string) — ✅ VERIFIED
  * `UseGraphicAlpha` (json: `"useGraphicAlpha"`, bool) — ✅ VERIFIED
* **Hallucinations**: **0 FOUND**.

### 9. `CuiScrollViewComponent` (Type: `"UnityEngine.UI.ScrollView"`)
* **Decompiled Source**: `Oxide.Game.Rust.Cui/CuiScrollViewComponent.cs` & `CuiScrollbar.cs`
* **Properties Verified**:
  * `ContentTransform` (json: `"contentTransform"`, `CuiRectTransform`) — ✅ VERIFIED
  * `Horizontal` (json: `"horizontal"`, bool) — ✅ VERIFIED
  * `Vertical` (json: `"vertical"`, bool) — ✅ VERIFIED
  * `MovementType` (json: `"movementType"`, `ScrollRect.MovementType`) — ✅ VERIFIED
  * `Elasticity` (json: `"elasticity"`, float) — ✅ VERIFIED
  * `Inertia` (json: `"inertia"`, bool) — ✅ VERIFIED
  * `DecelerationRate` (json: `"decelerationRate"`, float) — ✅ VERIFIED
  * `ScrollSensitivity` (json: `"scrollSensitivity"`, float) — ✅ VERIFIED
  * `HorizontalScrollbar` (json: `"horizontalScrollbar"`, `CuiScrollbar`) — ✅ VERIFIED
  * `VerticalScrollbar` (json: `"verticalScrollbar"`, `CuiScrollbar`) — ✅ VERIFIED
* **Hallucinations**: **0 FOUND**.

### 10. `CuiCanvasGroupComponent` (Type: `"UnityEngine.CanvasGroup"`)
* **Decompiled Source**: `Oxide.Game.Rust.Cui/CuiCanvasGroupComponent.cs`
* **Properties Verified**:
  * `Alpha` (json: `"alpha"`, float?) — ✅ VERIFIED
  * `BlocksRaycasts` (json: `"blocksRaycasts"`, bool?) — ✅ VERIFIED
  * `Interactable` (json: `"interactable"`, bool?) — ✅ VERIFIED
  * `IgnoreParentGroups` (json: `"ignoreParentGroups"`, bool?) — ✅ VERIFIED
* **Hallucinations**: **0 FOUND**.

### 11-21. Other Components
* `CuiNeedsCursorComponent` (json type `"NeedsCursor"`) — ✅ VERIFIED
* `CuiNeedsKeyboardComponent` (json type `"NeedsKeyboard"`) — ✅ VERIFIED
* `CuiTooltipComponent` (json type `"Tooltip"`) — ✅ VERIFIED
* `CuiDraggableComponent` (json type `"Draggable"`) — ✅ VERIFIED
* `CuiSlotComponent` (json type `"Slot"`) — ✅ VERIFIED
* `CuiMaskComponent` (json type `"UnityEngine.UI.Mask"`) — ✅ VERIFIED
* `CuiHorizontalLayoutGroupComponent` (json type `"UnityEngine.UI.HorizontalLayoutGroup"`) — ✅ VERIFIED
* `CuiVerticalLayoutGroupComponent` (json type `"UnityEngine.UI.VerticalLayoutGroup"`) — ✅ VERIFIED
* `CuiGridLayoutGroupComponent` (json type `"UnityEngine.UI.GridLayoutGroup"`) — ✅ VERIFIED
* `CuiLayoutElementComponent` (json type `"UnityEngine.UI.LayoutElement"`) — ✅ VERIFIED
* `CuiContentSizeFitterComponent` (json type `"UnityEngine.UI.ContentSizeFitter"`) — ✅ VERIFIED

---

## 3. Conclusion
Total components checked: **21**  
Total verified against decompiled source: **21 (100%)**  
Total hallucinated properties or types: **0**
