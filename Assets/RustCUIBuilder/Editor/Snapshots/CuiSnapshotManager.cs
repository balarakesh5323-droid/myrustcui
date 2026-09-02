using System;
using System.Collections.Generic;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Core.Project;
using RustCUIBuilder.Runtime.Core.Serialization;
using UnityEditor;
using UnityEngine;

namespace RustCUIBuilder.Editor.Snapshots
{
    /// <summary>
    /// Named snapshot manager for Rust CUI Builder.
    /// Stores named design checkpoints independent of undo history,
    /// enabling rapid branching, testing, and rollback of complex CUI layouts.
    /// </summary>
    public class CuiSnapshotManager
    {
        private string _newSnapshotName = "Snapshot 1";
        private string _newSnapshotDesc = "";
        private Vector2 _scrollPos;

        public void Draw(Rect rect, RustCuiProject project, CuiDocument doc, Action onModified)
        {
            if (project == null) return;

            GUILayout.BeginArea(rect);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Design Snapshots & Checkpoints", EditorStyles.boldLabel);

            // Create Snapshot Section
            EditorGUILayout.BeginVertical("helpBox");
            _newSnapshotName = EditorGUILayout.TextField("Snapshot Name", _newSnapshotName);
            _newSnapshotDesc = EditorGUILayout.TextField("Description", _newSnapshotDesc);

            if (GUILayout.Button("📸 Create Named Snapshot", GUILayout.Height(24)))
            {
                if (string.IsNullOrEmpty(_newSnapshotName)) _newSnapshotName = $"Snapshot {project.Snapshots.Count + 1}";
                string json = CuiJsonSerializer.SerializeDocument(doc, true);

                project.Snapshots.Add(new RustCuiProject.ProjectSnapshot
                {
                    Name = _newSnapshotName,
                    Description = _newSnapshotDesc,
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    CuiJson = json
                });

                _newSnapshotName = $"Snapshot {project.Snapshots.Count + 1}";
                _newSnapshotDesc = "";
                Debug.Log($"[RustCUIBuilder] Snapshot created: {project.Snapshots[project.Snapshots.Count - 1].Name}");
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField($"Saved Snapshots ({project.Snapshots.Count}):", EditorStyles.miniBoldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            for (int i = 0; i < project.Snapshots.Count; i++)
            {
                var snap = project.Snapshots[i];
                EditorGUILayout.BeginHorizontal("box");

                EditorGUILayout.BeginVertical();
                GUILayout.Label($"<b>{snap.Name}</b> ({snap.Timestamp})", new GUIStyle(EditorStyles.label) { richText = true });
                if (!string.IsNullOrEmpty(snap.Description))
                {
                    GUILayout.Label(snap.Description, EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();

                if (GUILayout.Button("Restore", GUILayout.Width(65)))
                {
                    if (EditorUtility.DisplayDialog("Restore Snapshot", $"Are you sure you want to restore snapshot '{snap.Name}'? Any unsaved changes in current canvas will be replaced.", "Restore", "Cancel"))
                    {
                        var result = CuiParser.ParseJson(snap.CuiJson);
                        if (result.Success && result.Document != null)
                        {
                            doc.Elements.Clear();
                            foreach (var elem in result.Document.Elements)
                            {
                                doc.AddElement(elem);
                            }
                            onModified?.Invoke();
                            Debug.Log($"[RustCUIBuilder] Snapshot '{snap.Name}' restored successfully!");
                        }
                    }
                }

                if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                {
                    project.Snapshots.RemoveAt(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
