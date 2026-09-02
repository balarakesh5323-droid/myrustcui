using System;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Core.Registry;
using UnityEditor;
using UnityEngine;

namespace RustCUIBuilder.Editor.Toolbox
{
    /// <summary>
    /// Component and template toolbox providing one-click creation of CUI primitives and composite templates.
    /// </summary>
    public class CuiToolboxView
    {
        private Vector2 _scrollPos;

        public void Draw(Rect rect, CuiDocument doc, Action onModified)
        {
            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Toolbox", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // Primitives
            EditorGUILayout.LabelField("Primitives", EditorStyles.boldLabel);
            DrawToolboxButton("Panel", () => CreateAndAddPreset("panel", doc, onModified));
            DrawToolboxButton("Label (Text)", () => CreateAndAddPreset("label", doc, onModified));
            DrawToolboxButton("Button", () => CreateAndAddPreset("button", doc, onModified));
            DrawToolboxButton("Input Field", () => CreateAndAddPreset("inputfield", doc, onModified));
            DrawToolboxButton("Image (Item/Sprite)", () => CreateAndAddPreset("image", doc, onModified));
            DrawToolboxButton("Raw Image (URL/Steam)", () => CreateAndAddPreset("rawimage", doc, onModified));
            DrawToolboxButton("Countdown Timer", () => CreateAndAddPreset("countdown", doc, onModified));
            DrawToolboxButton("Scroll View", () => CreateAndAddPreset("scrollview", doc, onModified));

            EditorGUILayout.Space(8);

            // Ready-to-use Templates
            EditorGUILayout.LabelField("UI Templates", EditorStyles.boldLabel);
            DrawToolboxButton("Modal Dialog with Close", () => CreateModalTemplate(doc, onModified));
            DrawToolboxButton("Notification Toast", () => CreateNotificationTemplate(doc, onModified));
            DrawToolboxButton("Inventory Grid (6 Slots)", () => CreateInventoryTemplate(doc, onModified));
            DrawToolboxButton("Navigation Tab Bar", () => CreateTabBarTemplate(doc, onModified));

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawToolboxButton(string label, Action onClick)
        {
            if (GUILayout.Button(label, GUILayout.Height(24)))
            {
                onClick?.Invoke();
            }
        }

        private void CreateAndAddPreset(string presetName, CuiDocument doc, Action onModified)
        {
            var node = CuiComponentRegistry.CreatePresetElement(presetName, "Overlay");
            doc.AddElement(node);
            doc.Select(node.Id);
            onModified?.Invoke();
        }

        private void CreateModalTemplate(CuiDocument doc, Action onModified)
        {
            string panelName = "ModalDialog_" + Guid.NewGuid().ToString("N").Substring(0, 4);

            // Background Panel
            var panel = new CuiElementNode(panelName, "Overlay");
            panel.Components.Add(new CuiImageComponent { Color = "0.08 0.09 0.12 0.94", Sprite = "assets/content/ui/ui.background.tile.psd" });
            panel.Components.Add(new CuiRectTransformComponent { AnchorMin = "0.25 0.2", AnchorMax = "0.75 0.8" });
            panel.Components.Add(new CuiNeedsCursorComponent());
            doc.AddElement(panel);

            // Header Title
            var title = new CuiElementNode($"{panelName}_Title", panelName);
            title.Components.Add(new CuiTextComponent { Text = "<b>SERVER CONTROL PANEL</b>", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = "0.95 0.95 0.98 1.0" });
            title.Components.Add(new CuiRectTransformComponent { AnchorMin = "0.05 0.88", AnchorMax = "0.85 0.98" });
            doc.AddElement(title);

            // Close Button
            var closeBtn = new CuiElementNode($"{panelName}_CloseBtn", panelName);
            closeBtn.Components.Add(new CuiButtonComponent { Color = "0.75 0.2 0.2 0.9", Close = panelName, Command = "myplugin.close" });
            closeBtn.Components.Add(new CuiTextComponent { Text = "✕", FontSize = 14, Align = TextAnchor.MiddleCenter });
            closeBtn.Components.Add(new CuiRectTransformComponent { AnchorMin = "0.90 0.88", AnchorMax = "0.97 0.96" });
            doc.AddElement(closeBtn);

            doc.Select(panel.Id);
            onModified?.Invoke();
        }

        private void CreateNotificationTemplate(CuiDocument doc, Action onModified)
        {
            string toastName = "Notification_" + Guid.NewGuid().ToString("N").Substring(0, 4);

            var toast = new CuiElementNode(toastName, "Overlay");
            toast.Components.Add(new CuiImageComponent { Color = "0.15 0.16 0.2 0.9", Sprite = "assets/content/ui/ui.background.tile.psd" });
            toast.Components.Add(new CuiRectTransformComponent { AnchorMin = "0.75 0.85", AnchorMax = "0.98 0.95" });
            toast.FadeOut = 0.5f;
            doc.AddElement(toast);

            var msg = new CuiElementNode($"{toastName}_Msg", toastName);
            msg.Components.Add(new CuiTextComponent { Text = "<b>Notification:</b> Event started!", FontSize = 13, Align = TextAnchor.MiddleLeft, Color = "0.9 0.9 0.9 1.0" });
            msg.Components.Add(new CuiRectTransformComponent { AnchorMin = "0.05 0.1", AnchorMax = "0.95 0.9" });
            doc.AddElement(msg);

            doc.Select(toast.Id);
            onModified?.Invoke();
        }

        private void CreateInventoryTemplate(CuiDocument doc, Action onModified)
        {
            string invName = "InventoryGrid_" + Guid.NewGuid().ToString("N").Substring(0, 4);

            var root = new CuiElementNode(invName, "Overlay");
            root.Components.Add(new CuiImageComponent { Color = "0.1 0.1 0.12 0.9" });
            root.Components.Add(new CuiRectTransformComponent { AnchorMin = "0.3 0.3", AnchorMax = "0.7 0.7" });
            root.Components.Add(new CuiNeedsCursorComponent());
            doc.AddElement(root);

            // 6 slot items
            for (int i = 0; i < 6; i++)
            {
                float xMin = 0.05f + (i % 3) * 0.32f;
                float xMax = xMin + 0.28f;
                float yMin = (i < 3) ? 0.55f : 0.1f;
                float yMax = yMin + 0.38f;

                var slot = new CuiElementNode($"{invName}_Slot_{i}", invName);
                slot.Components.Add(new CuiImageComponent { Color = "0.2 0.22 0.26 0.85" });
                slot.Components.Add(new CuiButtonComponent { Command = $"myplugin.selectslot {i}" });
                slot.Components.Add(new CuiRectTransformComponent { AnchorMin = $"{xMin:0.##} {yMin:0.##}", AnchorMax = $"{xMax:0.##} {yMax:0.##}" });
                doc.AddElement(slot);
            }

            doc.Select(root.Id);
            onModified?.Invoke();
        }

        private void CreateTabBarTemplate(CuiDocument doc, Action onModified)
        {
            string tabName = "TabBar_" + Guid.NewGuid().ToString("N").Substring(0, 4);

            var tabRoot = new CuiElementNode(tabName, "Overlay");
            tabRoot.Components.Add(new CuiImageComponent { Color = "0.12 0.13 0.16 0.95" });
            tabRoot.Components.Add(new CuiRectTransformComponent { AnchorMin = "0.2 0.82", AnchorMax = "0.8 0.90" });
            doc.AddElement(tabRoot);

            string[] tabLabels = { "MAIN", "SHOP", "RANKS", "SETTINGS" };
            for (int i = 0; i < tabLabels.Length; i++)
            {
                float xMin = i * 0.25f;
                float xMax = xMin + 0.25f;

                var tabBtn = new CuiElementNode($"{tabName}_Tab_{i}", tabName);
                tabBtn.Components.Add(new CuiButtonComponent { Command = $"myplugin.tab {tabLabels[i].ToLowerInvariant()}", Color = (i == 0) ? "0.85 0.45 0.15 0.9" : "0.25 0.26 0.3 0.8" });
                tabBtn.Components.Add(new CuiTextComponent { Text = $"<b>{tabLabels[i]}</b>", FontSize = 13, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" });
                tabBtn.Components.Add(new CuiRectTransformComponent { AnchorMin = $"{xMin:0.##} 0", AnchorMax = $"{xMax:0.##} 1" });
                doc.AddElement(tabBtn);
            }

            doc.Select(tabRoot.Id);
            onModified?.Invoke();
        }
    }
}
