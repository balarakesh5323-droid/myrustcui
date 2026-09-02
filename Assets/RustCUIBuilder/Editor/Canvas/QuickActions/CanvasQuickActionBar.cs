using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Editor.Canvas.Services;

namespace RustCUIBuilder.Editor.Canvas.QuickActions
{
    /// <summary>
    /// Contextual 1-click Quick Action Toolbar.
    /// Provides Figma/Photoshop style quick controls for Alignment, Spacing, Hierarchy Ordering, and Layout Presets.
    /// </summary>
    public class CanvasQuickActionBar
    {
        private bool _alignToCanvas;

        public void Draw(Rect barRect, CuiDocument doc, float canvasW, float canvasH, Action onModified, Action<string> onCommitUndo)
        {
            if (doc == null) return;
            var selected = doc.SelectedElements.ToList();
            int count = selected.Count;

            GUILayout.BeginArea(barRect);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Align To Canvas Toggle
            _alignToCanvas = GUILayout.Toggle(_alignToCanvas, new GUIContent("Align: Canvas", "Toggle Align to Canvas bounds vs Selection bounds"), EditorStyles.toolbarButton, GUILayout.Width(82));
            var target = _alignToCanvas ? AlignmentTarget.Canvas : AlignmentTarget.SelectionBounds;

            GUILayout.Space(4);

            // --- 1. ALIGNMENT BUTTONS ---
            bool canAlign = _alignToCanvas ? count >= 1 : count >= 2;
            using (new EditorGUI.DisabledScope(!canAlign))
            {
                if (GUILayout.Button(new GUIContent("Left", "Align Left (Ctrl+Alt+L)"), EditorStyles.toolbarButton, GUILayout.Width(36)))
                {
                    onCommitUndo?.Invoke("Align Left");
                    CanvasAlignmentService.AlignLeft(selected, doc, canvasW, canvasH, target);
                    onModified?.Invoke();
                }
                if (GUILayout.Button(new GUIContent("Center", "Align Center Horizontally (Ctrl+Alt+C)"), EditorStyles.toolbarButton, GUILayout.Width(46)))
                {
                    onCommitUndo?.Invoke("Align Center H");
                    CanvasAlignmentService.AlignCenterH(selected, doc, canvasW, canvasH, target);
                    onModified?.Invoke();
                }
                if (GUILayout.Button(new GUIContent("Right", "Align Right (Ctrl+Alt+R)"), EditorStyles.toolbarButton, GUILayout.Width(40)))
                {
                    onCommitUndo?.Invoke("Align Right");
                    CanvasAlignmentService.AlignRight(selected, doc, canvasW, canvasH, target);
                    onModified?.Invoke();
                }

                GUILayout.Space(2);

                if (GUILayout.Button(new GUIContent("Top", "Align Top (Ctrl+Alt+T)"), EditorStyles.toolbarButton, GUILayout.Width(34)))
                {
                    onCommitUndo?.Invoke("Align Top");
                    CanvasAlignmentService.AlignTop(selected, doc, canvasW, canvasH, target);
                    onModified?.Invoke();
                }
                if (GUILayout.Button(new GUIContent("Middle", "Align Center Vertically (Ctrl+Alt+M)"), EditorStyles.toolbarButton, GUILayout.Width(46)))
                {
                    onCommitUndo?.Invoke("Align Center V");
                    CanvasAlignmentService.AlignCenterV(selected, doc, canvasW, canvasH, target);
                    onModified?.Invoke();
                }
                if (GUILayout.Button(new GUIContent("Bottom", "Align Bottom (Ctrl+Alt+B)"), EditorStyles.toolbarButton, GUILayout.Width(48)))
                {
                    onCommitUndo?.Invoke("Align Bottom");
                    CanvasAlignmentService.AlignBottom(selected, doc, canvasW, canvasH, target);
                    onModified?.Invoke();
                }
            }

            GUILayout.Space(6);

            // --- 2. DISTRIBUTION & SPACING BUTTONS ---
            using (new EditorGUI.DisabledScope(count < 3))
            {
                if (GUILayout.Button(new GUIContent("Dist H", "Distribute Horizontally (Equal Spacing)"), EditorStyles.toolbarButton, GUILayout.Width(46)))
                {
                    onCommitUndo?.Invoke("Distribute Horizontally");
                    CanvasDistributionService.EqualHorizontalSpacing(selected, doc, canvasW, canvasH);
                    onModified?.Invoke();
                }
                if (GUILayout.Button(new GUIContent("Dist V", "Distribute Vertically (Equal Spacing)"), EditorStyles.toolbarButton, GUILayout.Width(46)))
                {
                    onCommitUndo?.Invoke("Distribute Vertically");
                    CanvasDistributionService.EqualVerticalSpacing(selected, doc, canvasW, canvasH);
                    onModified?.Invoke();
                }
            }

            GUILayout.Space(6);

            // --- 3. MATCH DIMENSIONS ---
            using (new EditorGUI.DisabledScope(count < 2))
            {
                if (GUILayout.Button(new GUIContent("Same W", "Make Same Width"), EditorStyles.toolbarButton, GUILayout.Width(54)))
                {
                    onCommitUndo?.Invoke("Make Same Width");
                    CanvasDistributionService.MakeSameWidth(selected, doc, canvasW, canvasH);
                    onModified?.Invoke();
                }
                if (GUILayout.Button(new GUIContent("Same H", "Make Same Height"), EditorStyles.toolbarButton, GUILayout.Width(52)))
                {
                    onCommitUndo?.Invoke("Make Same Height");
                    CanvasDistributionService.MakeSameHeight(selected, doc, canvasW, canvasH);
                    onModified?.Invoke();
                }
            }

            GUILayout.Space(6);

            // --- 4. HIERARCHY / GROUPING ---
            using (new EditorGUI.DisabledScope(count == 0))
            {
                if (GUILayout.Button(new GUIContent("Front", "Bring to Front (Ctrl+Shift+])"), EditorStyles.toolbarButton, GUILayout.Width(40)))
                {
                    onCommitUndo?.Invoke("Bring to Front");
                    CanvasHierarchyService.BringToFront(selected, doc);
                    onModified?.Invoke();
                }
                if (GUILayout.Button(new GUIContent("Back", "Send to Back (Ctrl+Shift+[)"), EditorStyles.toolbarButton, GUILayout.Width(40)))
                {
                    onCommitUndo?.Invoke("Send to Back");
                    CanvasHierarchyService.SendToBack(selected, doc);
                    onModified?.Invoke();
                }
            }

            using (new EditorGUI.DisabledScope(count < 2))
            {
                if (GUILayout.Button(new GUIContent("Group", "Group Selection (Ctrl+G)"), EditorStyles.toolbarButton, GUILayout.Width(44)))
                {
                    onCommitUndo?.Invoke("Group");
                    CanvasHierarchyService.GroupSelection(doc, canvasW, canvasH);
                    onModified?.Invoke();
                }
            }

            using (new EditorGUI.DisabledScope(count == 0))
            {
                if (GUILayout.Button(new GUIContent("Ungroup", "Ungroup Selection (Ctrl+Shift+G)"), EditorStyles.toolbarButton, GUILayout.Width(56)))
                {
                    onCommitUndo?.Invoke("Ungroup");
                    CanvasHierarchyService.UngroupSelection(doc, canvasW, canvasH);
                    onModified?.Invoke();
                }
            }

            GUILayout.FlexibleSpace();

            // --- 5. PARENT / LAYOUT PRESETS POPUP ---
            using (new EditorGUI.DisabledScope(count == 0))
            {
                if (GUILayout.Button(new GUIContent("Layout Presets ▼", "Quick Parent Layout Presets"), EditorStyles.toolbarDropDown, GUILayout.Width(105)))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Center in Parent"), false, () =>
                    {
                        onCommitUndo?.Invoke("Center in Parent");
                        CanvasLayoutService.CenterInParent(selected, doc, canvasW, canvasH);
                        onModified?.Invoke();
                    });
                    menu.AddItem(new GUIContent("Fill Parent"), false, () =>
                    {
                        onCommitUndo?.Invoke("Fill Parent");
                        CanvasLayoutService.FillParent(selected, doc, canvasW, canvasH);
                        onModified?.Invoke();
                    });
                    menu.AddItem(new GUIContent("Stretch Horizontally"), false, () =>
                    {
                        onCommitUndo?.Invoke("Stretch Horizontally");
                        CanvasLayoutService.StretchH(selected, doc, canvasW, canvasH);
                        onModified?.Invoke();
                    });
                    menu.AddItem(new GUIContent("Stretch Vertically"), false, () =>
                    {
                        onCommitUndo?.Invoke("Stretch Vertically");
                        CanvasLayoutService.StretchV(selected, doc, canvasW, canvasH);
                        onModified?.Invoke();
                    });
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Padding/8px All Around"), false, () =>
                    {
                        onCommitUndo?.Invoke("Apply 8px Padding");
                        CanvasLayoutService.ApplyPadding(selected, doc, canvasW, canvasH, 8f, 8f, 8f, 8f);
                        onModified?.Invoke();
                    });
                    menu.AddItem(new GUIContent("Padding/16px All Around"), false, () =>
                    {
                        onCommitUndo?.Invoke("Apply 16px Padding");
                        CanvasLayoutService.ApplyPadding(selected, doc, canvasW, canvasH, 16f, 16f, 16f, 16f);
                        onModified?.Invoke();
                    });
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Columns/2-Column Split"), false, () =>
                    {
                        onCommitUndo?.Invoke("2-Column Layout");
                        CanvasLayoutService.ApplyTwoColumnLayout(selected, doc, canvasW, canvasH);
                        onModified?.Invoke();
                    });
                    menu.AddItem(new GUIContent("Columns/3-Column Split"), false, () =>
                    {
                        onCommitUndo?.Invoke("3-Column Layout");
                        CanvasLayoutService.ApplyThreeColumnLayout(selected, doc, canvasW, canvasH);
                        onModified?.Invoke();
                    });
                    menu.AddItem(new GUIContent("Grid/Card Grid (3 cols)"), false, () =>
                    {
                        onCommitUndo?.Invoke("Card Grid Layout");
                        CanvasLayoutService.ApplyCardGridLayout(selected, doc, canvasW, canvasH, 3, 12f);
                        onModified?.Invoke();
                    });
                    menu.AddItem(new GUIContent("Stack/Vertical Stack"), false, () =>
                    {
                        onCommitUndo?.Invoke("Vertical Stack");
                        CanvasLayoutService.ApplyVerticalStack(selected, doc, canvasW, canvasH, 8f);
                        onModified?.Invoke();
                    });
                    menu.AddItem(new GUIContent("Stack/Horizontal Stack"), false, () =>
                    {
                        onCommitUndo?.Invoke("Horizontal Stack");
                        CanvasLayoutService.ApplyHorizontalStack(selected, doc, canvasW, canvasH, 8f);
                        onModified?.Invoke();
                    });
                    menu.ShowAsContext();
                }
            }

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
