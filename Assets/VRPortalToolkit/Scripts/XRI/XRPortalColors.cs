using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Rendering;

namespace VRPortalToolkit.XRI
{
    /// <summary>
    /// Manages colors for XR portals based on their interaction state.
    /// </summary>
    [RequireComponent(typeof(XRPortalInteractable))]
    public class XRPortalColors : MonoBehaviour
    {
        /// <summary>
        /// Shader property ID for the base color.
        /// </summary>
        public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [Tooltip("The renderers to apply colors to.")]
        [SerializeField] private List<Renderer> _renderers;
        /// <summary>
        /// The renderers to apply colors to.
        /// </summary>
        public List<Renderer> renderers => _renderers;

        [Tooltip("The default color when the portal is not interacted with.")]
        [SerializeField] private Color _defaultColor = Color.cyan;
        /// <summary>
        /// The default color when the portal is not interacted with.
        /// </summary>
        public Color defaultColor
        {
            get => _defaultColor;
            set => _defaultColor = value;
        }

        [Tooltip("The color when the portal is hovered.")]
        [SerializeField] private Color _hoveredColor = Color.white;
        /// <summary>
        /// The color when the portal is hovered.
        /// </summary>
        public Color hoveredColor
        {
            get => _hoveredColor;
            set => _hoveredColor = value;
        }

        [Tooltip("The color when the portal is selected.")]
        [SerializeField] private Color _selectedColor = Color.cyan;
        /// <summary>
        /// The color when the portal is selected.
        /// </summary>
        public Color selectedColor
        {
            get => _selectedColor;
            set => _selectedColor = value;
        }

        private MaterialPropertyBlock _properties;

        private XRPortalInteractable _interactable;

        private State _state;
        private enum State : byte
        {
            None = 0,
            Default = 1,
            Hover = 2,
            Select = 3,
        }

        protected virtual void Reset()
        {
            GetComponentsInChildren(_renderers);
        }

        protected virtual void Awake()
        {
            _interactable = GetComponent<XRPortalInteractable>();
            _properties = new MaterialPropertyBlock();
        }

        protected virtual void OnEnable()
        {
            _state = State.None;
        }

        protected virtual void LateUpdate()
        {
            State newState = State.Default;

            if (_interactable)
            {
                if (_interactable.isSelected || (_interactable.connected && _interactable.connected.isSelected))
                    newState = State.Select;
                else if (_interactable.isHovered || (_interactable.connected && _interactable.connected.isHovered))
                    newState = State.Hover;
            }

            if (_state != newState)
            {
                _state = newState;

                _properties.Clear();

                switch (_state)
                {
                    case State.Hover:
                        _properties.SetColor(BaseColor, _hoveredColor);
                        break;
                    case State.Select:
                        _properties.SetColor(BaseColor, _selectedColor);
                        break;
                    default:
                        _properties.SetColor(BaseColor, _defaultColor);
                        break;
                }

                foreach (Renderer renderer in _renderers)
                {
                    if (!renderer) continue;

                    renderer.SetPropertyBlock(_properties);
                }
            }
        }
    }
}
