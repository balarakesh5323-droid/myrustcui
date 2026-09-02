using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RustCUIBuilder.Runtime.Core.Models
{
    /// <summary>
    /// Root document container representing an entire Rust CUI screen / plugin interface.
    /// Manages the full list of CuiElementNode elements, parent-child hierarchies, and selection state.
    /// </summary>
    [Serializable]
    public class CuiDocument
    {
        public string ProjectName { get; set; } = "MyRustCUI";
        public string Author { get; set; } = "Rust Developer";
        public string RustProtocol { get; set; } = "2632.287.1";
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
        public string ModifiedAt { get; set; } = DateTime.UtcNow.ToString("o");

        public List<CuiElementNode> Elements { get; set; } = new List<CuiElementNode>();

        [NonSerialized]
        private readonly HashSet<string> _selectedIds = new HashSet<string>();

        public event Action OnDocumentModified;
        public event Action OnSelectionChanged;

        public IReadOnlyCollection<string> SelectedIds => _selectedIds;

        public CuiElementNode PrimarySelectedElement
        {
            get
            {
                var firstId = _selectedIds.FirstOrDefault();
                return firstId != null ? FindById(firstId) : null;
            }
        }

        public List<CuiElementNode> SelectedElements => _selectedIds.Select(FindById).Where(e => e != null).ToList();

        public bool IsSelected(string id) => !string.IsNullOrEmpty(id) && _selectedIds.Contains(id);

        public void NotifyModified()
        {
            ModifiedAt = DateTime.UtcNow.ToString("o");
            OnDocumentModified?.Invoke();
        }

        public CuiElementNode FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return Elements.FirstOrDefault(e => e.Id == id);
        }

        public CuiElementNode FindByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Elements.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public CuiElementNode FindElementByName(string name) => FindByName(name);

        public List<CuiElementNode> GetChildrenOf(string parentName)
        {
            if (string.IsNullOrEmpty(parentName)) return new List<CuiElementNode>();
            return Elements.Where(e => string.Equals(e.Parent, parentName, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public List<CuiElementNode> GetRootElements()
        {
            // Elements whose parent is a known layer (Overall, Overlay, Hud, etc.) or not in the document
            var allNames = new HashSet<string>(Elements.Select(e => e.Name), StringComparer.OrdinalIgnoreCase);
            return Elements.Where(e => string.IsNullOrEmpty(e.Parent) || !allNames.Contains(e.Parent)).ToList();
        }

        public void AddElement(CuiElementNode node, int index = -1)
        {
            if (node == null) return;
            EnsureUniqueName(node);

            if (index >= 0 && index < Elements.Count)
                Elements.Insert(index, node);
            else
                Elements.Add(node);

            NotifyModified();
        }

        public bool RemoveElement(string id, bool removeDescendants = true)
        {
            var node = FindById(id);
            if (node == null) return false;

            if (removeDescendants)
            {
                var descendants = GetDescendants(node.Name);
                foreach (var d in descendants)
                {
                    _selectedIds.Remove(d.Id);
                    Elements.Remove(d);
                }
            }

            _selectedIds.Remove(node.Id);
            bool removed = Elements.Remove(node);
            if (removed)
            {
                NotifyModified();
                OnSelectionChanged?.Invoke();
            }
            return removed;
        }

        public List<CuiElementNode> GetDescendants(string parentName)
        {
            var result = new List<CuiElementNode>();
            var directChildren = GetChildrenOf(parentName);
            foreach (var child in directChildren)
            {
                result.Add(child);
                result.AddRange(GetDescendants(child.Name));
            }
            return result;
        }

        public void Select(string id, bool additive = false)
        {
            if (!additive)
            {
                _selectedIds.Clear();
                foreach (var e in Elements) e.IsSelected = false;
            }

            if (!string.IsNullOrEmpty(id))
            {
                _selectedIds.Add(id);
                var elem = FindById(id);
                if (elem != null) elem.IsSelected = true;
            }

            OnSelectionChanged?.Invoke();
        }

        public void Deselect(string id)
        {
            if (_selectedIds.Remove(id))
            {
                var elem = FindById(id);
                if (elem != null) elem.IsSelected = false;
                OnSelectionChanged?.Invoke();
            }
        }

        public void ClearSelection()
        {
            _selectedIds.Clear();
            foreach (var e in Elements) e.IsSelected = false;
            OnSelectionChanged?.Invoke();
        }

        public void SelectAll()
        {
            _selectedIds.Clear();
            foreach (var e in Elements)
            {
                _selectedIds.Add(e.Id);
                e.IsSelected = true;
            }
            OnSelectionChanged?.Invoke();
        }

        public void EnsureUniqueName(CuiElementNode node)
        {
            if (node == null) return;
            string baseName = string.IsNullOrWhiteSpace(node.Name) ? "Element" : node.Name;
            string candidate = baseName;
            int counter = 1;

            var existingNames = new HashSet<string>(
                Elements.Where(e => e.Id != node.Id).Select(e => e.Name),
                StringComparer.OrdinalIgnoreCase
            );

            while (existingNames.Contains(candidate))
            {
                candidate = $"{baseName}_{counter++}";
            }

            node.Name = candidate;
        }

        public CuiDocument Clone()
        {
            var doc = new CuiDocument
            {
                ProjectName = ProjectName,
                Author = Author,
                RustProtocol = RustProtocol,
                CreatedAt = CreatedAt,
                ModifiedAt = ModifiedAt,
                Elements = Elements.Select(e => e.Clone(false, e.Name)).ToList()
            };
            return doc;
        }
    }
}
