using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Rendering;

namespace VRPortalToolkit.XRI
{
    [RequireComponent(typeof(XRPortalInteractable))]
    public class XRPortalColors : MonoBehaviour
    {
        public static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private List<Renderer> _renderers;
        public List<Renderer> renderers => _renderers;

        [SerializeField] private Color _defaultColor = Color.cyan;
        public Color defaultColor
        {
            get => _defaultColor;
            set => _defaultColor = value;
        }

        [SerializeField] private Color _hoveredColor = Color.white;
        public Color hoveredColor
        {
            get => _hoveredColor;
            set => _hoveredColor = value;
        }

        [SerializeField] private Color _selectedColor = Color.cyan;
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

        protected virtual void OnDisable()
        {

        }
    }
}
