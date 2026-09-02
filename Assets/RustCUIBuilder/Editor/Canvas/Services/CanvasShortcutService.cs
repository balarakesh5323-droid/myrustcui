using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RustCUIBuilder.Runtime.Core.Models;

namespace RustCUIBuilder.Editor.Canvas.Services
{
    /// <summary>
    /// Master keyboard shortcut dispatcher for canvas operations.
    /// Handles standard Figma/Photoshop/Unity shortcut combinations cleanly.
    /// </summary>
    public static class CanvasShortcutService
    {
        public static bool ProcessGlobalShortcuts(
            Event currentEvent,
            CuiDocument doc,
            float canvasW,
            float canvasH,
            Action onModified,
            Action<string> onCommitUndo)
        {
            if (doc == null || currentEvent.type != EventType.KeyDown) return false;

            bool isCtrlOrCmd = currentEvent.control || currentEvent.command;

            // 1. Delete / Backspace
            if (currentEvent.keyCode == KeyCode.Delete || currentEvent.keyCode == KeyCode.Backspace)
            {
                if (doc.SelectedElements.Count > 0)
                {
                    onCommitUndo?.Invoke("Delete Selection");
                    var selected = doc.SelectedElements.ToList();
                    foreach (var elem in selected) doc.RemoveElement(elem.Id);
                    onModified?.Invoke();
                    currentEvent.Use();
                    return true;
                }
            }

            // 2. Ctrl + A (Select All)
            if (isCtrlOrCmd && currentEvent.keyCode == KeyCode.A && !currentEvent.shift && !currentEvent.alt)
            {
                doc.SelectAll();
                onModified?.Invoke();
                currentEvent.Use();
                return true;
            }

            // 3. Ctrl + C (Copy)
            if (isCtrlOrCmd && currentEvent.keyCode == KeyCode.C && !currentEvent.shift && !currentEvent.alt)
            {
                if (doc.SelectedElements.Count > 0)
                {
                    CanvasClipboardService.Copy(doc.SelectedElements.ToList(), doc);
                    currentEvent.Use();
                    return true;
                }
            }

            // 4. Ctrl + X (Cut)
            if (isCtrlOrCmd && currentEvent.keyCode == KeyCode.X && !currentEvent.shift && !currentEvent.alt)
            {
                if (doc.SelectedElements.Count > 0)
                {
                    onCommitUndo?.Invoke("Cut Selection");
                    CanvasClipboardService.Cut(doc.SelectedElements.ToList(), doc);
                    onModified?.Invoke();
                    currentEvent.Use();
                    return true;
                }
            }

            // 5. Ctrl + V (Paste)
            if (isCtrlOrCmd && currentEvent.keyCode == KeyCode.V && !currentEvent.shift && !currentEvent.alt)
            {
                if (CanvasClipboardService.HasClipboardData)
                {
                    onCommitUndo?.Invoke("Paste");
                    CanvasClipboardService.Paste(doc, canvasW, canvasH);
                    onModified?.Invoke();
                    currentEvent.Use();
                    return true;
                }
            }

            // 6. Ctrl + D (Duplicate)
            if (isCtrlOrCmd && currentEvent.keyCode == KeyCode.D && !currentEvent.shift && !currentEvent.alt)
            {
                if (doc.SelectedElements.Count > 0)
                {
                    onCommitUndo?.Invoke("Duplicate");
                    CanvasClipboardService.Duplicate(doc.SelectedElements.ToList(), doc, canvasW, canvasH);
                    onModified?.Invoke();
                    currentEvent.Use();
                    return true;
                }
            }

            // 7. Group (Ctrl + G) & Ungroup (Ctrl + Shift + G)
            if (isCtrlOrCmd && currentEvent.keyCode == KeyCode.G)
            {
                if (currentEvent.shift)
                {
                    onCommitUndo?.Invoke("Ungroup");
                    CanvasHierarchyService.UngroupSelection(doc, canvasW, canvasH);
                    onModified?.Invoke();
                    currentEvent.Use();
                    return true;
                }
                else
                {
                    if (doc.SelectedElements.Count >= 2)
                    {
                        onCommitUndo?.Invoke("Group");
                        CanvasHierarchyService.GroupSelection(doc, canvasW, canvasH);
                        onModified?.Invoke();
                        currentEvent.Use();
                        return true;
                    }
                }
            }

            // 8. Ordering Shortcuts: Ctrl + [ (Send Backward), Ctrl + ] (Bring Forward)
            if (isCtrlOrCmd && currentEvent.keyCode == KeyCode.LeftBracket)
            {
                if (currentEvent.shift)
                {
                    onCommitUndo?.Invoke("Send to Back");
                    CanvasHierarchyService.SendToBack(doc.SelectedElements.ToList(), doc);
                }
                else
                {
                    onCommitUndo?.Invoke("Send Backward");
                    CanvasHierarchyService.SendBackward(doc.SelectedElements.ToList(), doc);
                }
                onModified?.Invoke();
                currentEvent.Use();
                return true;
            }

            if (isCtrlOrCmd && currentEvent.keyCode == KeyCode.RightBracket)
            {
                if (currentEvent.shift)
                {
                    onCommitUndo?.Invoke("Bring to Front");
                    CanvasHierarchyService.BringToFront(doc.SelectedElements.ToList(), doc);
                }
                else
                {
                    onCommitUndo?.Invoke("Bring Forward");
                    CanvasHierarchyService.BringForward(doc.SelectedElements.ToList(), doc);
                }
                onModified?.Invoke();
                currentEvent.Use();
                return true;
            }

            // 9. Nudge via Arrow Keys
            if (CanvasNudgeService.ProcessNudgeEvent(currentEvent, doc, canvasW, canvasH, onModified, onCommitUndo))
            {
                return true;
            }

            return false;
        }
    }
}
