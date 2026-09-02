# Rust Oxide CUI Authoritative Architecture & Research Log

> **Source of Truth**: Decompiled Rust Game Source Code (`Assembly-CSharp`, `Oxide.Rust`, `Facepunch.UI`, `UnityEngine.UI`) located at `C:\Users\Bala Rakesh\Documents\rustmcprag\Source_Code` and `C:\Users\Bala Rakesh\Documents\rustmcprag\docs\guides\developers\basic-cui\basic-cui.md`.

---

## 1. Rust CUI Architecture Overview

Rust Community UI (CUI) is a data-driven JSON UI abstraction built on top of Unity's UI hierarchy (`UnityEngine.UI`). It is transmitted over network RPCs from Oxide server plugins (`CuiHelper.AddUi`, `CuiHelper.DestroyUi`) to the Rust game client (`CommunityEntity`).

### Key Dimensions & Coordinates
* **Base Virtual Canvas Resolution**: `1280 x 720` (16:9).
* **Anchor System**: Normalized coordinates `(0.0, 0.0)` [Bottom-Left] to `(1.0, 1.0)` [Top-Right].
* **Offset System**: Pixel offsets relative to anchor positions `[OffsetMin.x OffsetMin.y] [OffsetMax.x OffsetMax.y]`.
* **Colors**: Space-separated normalized floats `r g b a` (e.g., `"0.1 0.1 0.1 0.8"`). Hex colors `#RRGGBB` or `#RRGGBBAA` are also parsed by `CuiColorExtensions`.

---

## 2. Verified CUI Components Index

| Component | Rust/Oxide Type | Unity Equivalent | Key Properties | Preview Support | Export Support |
| :--- | :--- | :--- | :--- | :---: | :---: |
| **`RectTransform`** | `CuiRectTransformComponent` | `UnityEngine.RectTransform` | `AnchorMin`, `AnchorMax`, `OffsetMin`, `OffsetMax`, `Pivot`, `Rotation` | Full | Full |
| **`Text`** | `CuiTextComponent` | `UnityEngine.UI.Text` / `TextMeshPro` | `Text`, `FontSize`, `Font`, `Align`, `Color`, `FadeIn`, `VerticalOverflow` | Full | Full |
| **`Image`** | `CuiImageComponent` | `UnityEngine.UI.Image` | `Sprite`, `Material`, `Color`, `ImageType`, `ItemId`, `FadeIn`, `FillCenter` | Full | Full |
| **`RawImage`** | `CuiRawImageComponent` | `UnityEngine.UI.RawImage` | `Url`, `SteamId`, `Sprite`, `Material`, `Color`, `FadeIn` | Full | Full |
| **`Button`** | `CuiButtonComponent` | `UnityEngine.UI.Button` | `Command`, `Close`, `Sprite`, `Material`, `Color`, `FadeIn` | Full | Full |
| **`InputField`** | `CuiInputFieldComponent` | `UnityEngine.UI.InputField` | `Text`, `FontSize`, `Command`, `CharsLimit`, `IsPassword`, `NeedsKeyboard`, `Autofocus` | Full | Full |
| **`Countdown`** | `CuiCountdownComponent` | `Rust.UI.Countdown` | `StartTime`, `EndTime`, `Step`, `Interval`, `TimerFormat`, `Command`, `DestroyIfDone` | Full | Full |
| **`Outline`** | `CuiOutlineComponent` | `UnityEngine.UI.Outline` | `Color`, `Distance`, `UseGraphicAlpha` | Full | Full |
| **`ScrollView`** | `CuiScrollViewComponent` | `UnityEngine.UI.ScrollRect` | `Horizontal`, `Vertical`, `MovementType`, `Elasticity`, `ScrollSensitivity` | Full | Full |
| **`CanvasGroup`** | `CuiCanvasGroupComponent` | `UnityEngine.CanvasGroup` | `Alpha`, `BlocksRaycasts`, `Interactable` | Full | Full |
| **`NeedsCursor`** | `CuiNeedsCursorComponent` | `Rust.UI.NeedsCursor` | None (Presence indicates cursor unlock) | Full | Full |
| **`NeedsKeyboard`**| `CuiNeedsKeyboardComponent` | `Rust.UI.NeedsKeyboard` | None (Presence captures keyboard input) | Full | Full |
| **`Tooltip`** | `CuiTooltipComponent` | `Rust.UI.Tooltip` | `Text`, `TooltipType`, `Position`, `Offset` | Full | Full |
| **`Draggable`** | `CuiDraggableComponent` | `Rust.UI.Draggable` | `DragAlpha`, `Filter`, `LimitToParent`, `DropAnywhere`, `PositionRPC` | Full | Full |
| **`Slot`** | `CuiSlotComponent` | `Rust.UI.Slot` | `Filter` | Full | Full |
| **`Mask`** | `CuiMaskComponent` | `UnityEngine.UI.Mask` | `ShowMaskGraphic` | Full | Full |
| **`HorizontalLayout`** | `CuiHorizontalLayoutGroupComponent` | `UnityEngine.UI.HorizontalLayoutGroup` | `Spacing`, `ChildAlignment`, `ChildForceExpandWidth`, `ChildForceExpandHeight`, `Padding` | Full | Full |
| **`VerticalLayout`** | `CuiVerticalLayoutGroupComponent` | `UnityEngine.UI.VerticalLayoutGroup` | `Spacing`, `ChildAlignment`, `ChildForceExpandWidth`, `ChildForceExpandHeight`, `Padding` | Full | Full |
| **`GridLayout`** | `CuiGridLayoutGroupComponent` | `UnityEngine.UI.GridLayoutGroup` | `CellSize`, `Spacing`, `Constraint`, `ConstraintCount` | Full | Full |

---

## 3. Verified UI Hierarchy Layers

* **`Overall`**: Root layer spanning entire screen including game HUD and chat.
* **`Overlay`**: Standard top-level UI layer (most common for menus, modals, dialogs).
* **`OverlayNonScaled`**: Unscaled overlay layer.
* **`Hud.Menu`**: HUD sub-layer for in-game menus.
* **`Hud`**: Game HUD layer (health, hydration, hotbar).
* **`Under`**: Layer underneath HUD elements.
* **`UnderNonScaled`**: Unscaled under layer.
* **`Inventory`**: In-game inventory screen layer.
* **`Crafting`**: Crafting menu layer.
* **`Contacts`**: Contacts and clan leaderboards.
* **`Clans`**: Clan UI layer.
* **`TechTree`**: Workbench blueprint tech tree layer.
* **`Map`**: In-game full-screen map layer.

---

## 4. Verified Rust Sprites & Asset Paths

```
assets/content/ui/ui.background.tile.psd
assets/content/ui/ui.background.transparent.psd
assets/content/ui/ui.box.shadow.psd
assets/content/ui/ui.circle.psd
assets/content/ui/ui.circle.gradient.psd
assets/content/ui/ui.rounded.psd
assets/content/ui/ui.white.psd
assets/content/materials/highlight.png
assets/icons/check.png
assets/icons/close.png
assets/icons/cross.png
assets/icons/circle_closed.png
assets/icons/device_add.png
assets/icons/fun.png
assets/icons/facepunch.png
assets/icons/explosion_sprite.png
assets/icons/signal_sprite.png
assets/icons/radiation.png
assets/icons/bleeding.png
assets/icons/cold.png
assets/icons/wet.png
assets/icons/poison.png
assets/icons/starve.png
assets/icons/thirst.png
assets/icons/wound.png
assets/icons/skull.png
assets/icons/lock.png
assets/icons/unlock.png
assets/icons/chat.png
assets/icons/store.png
assets/icons/gear.png
assets/icons/shield.png
assets/icons/swords.png
assets/icons/hammer.png
assets/icons/wrench.png
assets/icons/coin.png
assets/icons/clock.png
assets/icons/map.png
assets/icons/compass.png
assets/icons/server.png
assets/icons/warning.png
assets/icons/info.png
assets/icons/refresh.png
assets/icons/plus.png
assets/icons/minus.png
assets/icons/trash.png
```

---

## 5. Verified Materials & Shaders

```
assets/content/ui/uibackgroundblur.mat
assets/content/ui/uibackgroundblur-ingamemenu.mat
assets/content/ui/uibackgroundblur-mainmenu.mat
assets/content/ui/uibackgroundblur-notice.mat
assets/icons/iconmaterial.mat
assets/content/ui/ui.maskclear.mat
assets/content/ui/ui.saturation.shader
assets/content/ui/ui.thresholdcolor.shader
assets/icons/fogofwar.mat
assets/icons/greyout.mat
```

---

## 6. Verified Fonts

* `RobotoCondensed-Bold.ttf`
* `RobotoCondensed-Regular.ttf`
* `DroidSansMono.ttf`
* `PermanentMarker.ttf`
* `dsk.ttf`
