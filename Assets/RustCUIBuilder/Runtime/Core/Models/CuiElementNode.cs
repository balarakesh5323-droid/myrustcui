using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RustCUIBuilder.Runtime.Core.Models
{
    /// <summary>
    /// Represents an AST node for a Rust CUI Element.
    /// Can contain multiple ICuiComponent instances and maintains hierarchy relationships.
    /// </summary>
    [Serializable]
    public class CuiElementNode
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "NewElement";
        public string Parent { get; set; } = "Overlay";
        public string DestroyUi { get; set; }
        public float FadeOut { get; set; } = 0.0f;
        public bool Update { get; set; } = false;
        public bool? ActiveSelf { get; set; }

        public List<ICuiComponent> Components { get; set; } = new List<ICuiComponent>();

        // Editor UI states
        [NonSerialized] public bool IsSelected;
        [NonSerialized] public bool IsExpanded = true;
        [NonSerialized] public bool IsLocked = false;
        [NonSerialized] public bool IsHidden = false;

        public CuiElementNode()
        {
        }

        public CuiElementNode(string name, string parent = "Overlay")
        {
            Id = Guid.NewGuid().ToString("N");
            Name = name;
            Parent = parent;
        }

        public T GetComponent<T>() where T : class, ICuiComponent
        {
            return Components.OfType<T>().FirstOrDefault();
        }

        public T GetOrCreateComponent<T>() where T : class, ICuiComponent, new()
        {
            var comp = GetComponent<T>();
            if (comp == null)
            {
                comp = new T();
                Components.Add(comp);
            }
            return comp;
        }

        public bool RemoveComponent<T>() where T : class, ICuiComponent
        {
            var comp = GetComponent<T>();
            if (comp != null)
            {
                return Components.Remove(comp);
            }
            return false;
        }

        public bool HasComponent<T>() where T : class, ICuiComponent
        {
            return Components.Any(c => c is T);
        }

        public CuiRectTransformComponent RectTransform
        {
            get => GetOrCreateComponent<CuiRectTransformComponent>();
            set
            {
                RemoveComponent<CuiRectTransformComponent>();
                if (value != null) Components.Insert(0, value);
            }
        }

        public CuiElementNode Clone(bool newId = true, string newName = null)
        {
            var clone = new CuiElementNode
            {
                Id = newId ? Guid.NewGuid().ToString("N") : Id,
                Name = newName ?? (newId ? $"{Name}_Copy" : Name),
                Parent = Parent,
                DestroyUi = DestroyUi,
                FadeOut = FadeOut,
                Update = Update,
                ActiveSelf = ActiveSelf,
                Components = Components.Select(c => c.Clone()).ToList(),
                IsLocked = IsLocked,
                IsHidden = IsHidden
            };
            return clone;
        }
    }
}
