using RustCUIBuilder.Runtime.Core.Models;
using RustCUIBuilder.Runtime.Core.Serialization;
using UnityEngine;

namespace RustCUIBuilder.Runtime.Rendering.Canvas
{
    /// <summary>
    /// Preview Harness component for placing inside a Unity Scene.
    /// Can load CUI JSON files, render them using RustViewportRenderer,
    /// and simulate real in-game behavior.
    /// </summary>
    [RequireComponent(typeof(RustViewportRenderer))]
    public class RustCuiPreviewHarness : MonoBehaviour
    {
        [TextArea(5, 15)]
        [SerializeField] private string _cuiJson = "";
        [SerializeField] public int SelectedResolutionPresetIndex = 3; // 1920x1080 default

        private RustViewportRenderer _renderer;
        private CuiDocument _currentDocument;

        public CuiDocument CurrentDocument => _currentDocument;

        private void Awake()
        {
            _renderer = GetComponent<RustViewportRenderer>();
            if (!string.IsNullOrEmpty(_cuiJson))
            {
                LoadJson(_cuiJson);
            }
        }

        public void LoadJson(string json)
        {
            _cuiJson = json;
            var result = CuiParser.ParseJson(json, "PreviewDocument");
            if (result.Success && result.Document != null)
            {
                _currentDocument = result.Document;
                if (_renderer == null) _renderer = GetComponent<RustViewportRenderer>();
                _renderer.RenderDocument(_currentDocument);
            }
            else
            {
                Debug.LogWarning("[RustCuiPreviewHarness] Failed to parse CUI JSON: " + string.Join("; ", result.Errors));
            }
        }

        public void RenderCurrentDocument()
        {
            if (_currentDocument != null && _renderer != null)
            {
                _renderer.RenderDocument(_currentDocument);
            }
        }
    }
}
