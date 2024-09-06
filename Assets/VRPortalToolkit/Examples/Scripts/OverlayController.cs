using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Rendering;
using VRPortalToolkit.XRI;

namespace VRPortalToolkit.Examples
{
    public class OverlayController : MonoBehaviour
    {
        [SerializeField] private XRBaseInteractable _button;
        public XRBaseInteractable button
        {
            get => _button;
            set => _button = value;
        }

        [SerializeField] private PortalManager _portalManager;
        public PortalManager portalManager
        {
            get => _portalManager;
            set => _portalManager = value;
        }

        [SerializeField] private TextMeshPro _text;
        public TextMeshPro text
        {
            get => _text;
            set => _text = value;
        }

        [SerializeField] private Material _opactiy;
        public Material opacity
        {
            get => _opactiy;
            set => _opactiy = value;
        }

        [SerializeField] private Material _contours;
        public Material contours
        {
            get => _contours;
            set => _contours = value;
        }

        [SerializeField] private Material _contoursIncrease;
        public Material contoursIncrease
        {
            get => _contoursIncrease;
            set => _contoursIncrease = value;
        }

        [SerializeField] private Material _contoursDecrease;
        public Material contoursDecrease
        {
            get => _contoursDecrease;
            set => _contoursDecrease = value;
        }

        [SerializeField] private Material _absolute;
        public Material absolute
        {
            get => _absolute;
            set => _absolute = value;
        }

        private int state = 1;

        protected void OnEnable()
        {
            _button?.firstSelectEntered?.AddListener(ButtonPressed);
        }

        protected void OnDisable()
        {
            _button?.firstSelectEntered?.RemoveListener(ButtonPressed);
        }

        private void ButtonPressed(SelectEnterEventArgs _)
        {
            state = (state + 1) % 4;

            switch (state)
            {
                case 1: // Contours
                    UpdateState(true, new PortalRendererSettings() { portalStereo = _contours, depthNormalTexture = true });
                    if (_text) _text.text = "Contours";
                    break;
                case 2: // Opacity
                    UpdateState(true, new PortalRendererSettings() { portalStereo = _opactiy });
                    if (_text) _text.text = "Blended";
                    break;
                case 3: // Absolute
                    UpdateState(true, new PortalRendererSettings() { portalStereo = _absolute });
                    if (_text) _text.text = "Absolute";
                    break;
                default: // Default
                    UpdateState(false);
                    if (_text) _text.text = "None";
                    break;
            }
        }

        private void UpdateState(bool state, PortalRendererSettings settings = default)
        {
            if (_portalManager)
            {
                foreach (var portal in _portalManager.portalPairs)
                {
                    foreach (var xrPortalOverlay in portal.GetComponentsInChildren<XRPortalOverlay>(true))
                    {
                        xrPortalOverlay.enabled = state;
                        xrPortalOverlay.overrides = settings;
                    }
                }
            }
        }
    }
}
