using System;
using System.Collections.Generic;
using System.IO;
using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Core.Serialization;
using UnityEngine;

namespace RustCUIBuilder.Runtime.Core.Project
{
    /// <summary>
    /// Professional structured Project system for Rust CUI Builder (.rustcui format).
    /// Stores UI Document, Author metadata, Design notes, Resolution preferences, and named Snapshots.
    /// </summary>
    [Serializable]
    public class RustCuiProject
    {
        public string ProjectName = "New CUI Project";
        public string Author = "Developer";
        public string Version = "1.0.0";
        public string CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        public string LastModified = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        public string Description = "";

        public int TargetWidth = 1920;
        public int TargetHeight = 1080;

        public List<CuiElementNode> Elements = new List<CuiElementNode>();
        public List<ProjectSnapshot> Snapshots = new List<ProjectSnapshot>();

        [Serializable]
        public class ProjectSnapshot
        {
            public string Name;
            public string Timestamp;
            public string Description;
            public string CuiJson;
        }

        public CuiDocument ToDocument()
        {
            var doc = new CuiDocument();
            foreach (var elem in Elements)
            {
                doc.AddElement(elem.Clone());
            }
            return doc;
        }

        public void FromDocument(CuiDocument doc)
        {
            Elements.Clear();
            if (doc != null && doc.Elements != null)
            {
                foreach (var elem in doc.Elements)
                {
                    Elements.Add(elem.Clone());
                }
            }
            LastModified = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public void SaveToFile(string filePath)
        {
            string json = JsonUtility.ToJson(this, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"[RustCUIBuilder] Project successfully saved to: {filePath}");
        }

        public static RustCuiProject LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            string json = File.ReadAllText(filePath);
            var proj = JsonUtility.FromJson<RustCuiProject>(json);
            Debug.Log($"[RustCUIBuilder] Project successfully loaded from: {filePath}");
            return proj;
        }
    }
}
