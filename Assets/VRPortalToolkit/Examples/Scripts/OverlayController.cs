using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Rendering;
using VRPortalToolkit.XRI;

namespace VRPortalToolkit.Examples
{
    /// <summary>
    /// Controls different portal overlay visual effects for demonstration purposes.
    /// </summary>
    /// <remarks>
    /// This component allows cycling through various portal overlay rendering modes via a button press:
    /// - None: No overlay effect
    /// - Contours: Shows contour lines to help visualize depth
    /// - Blended: Creates a semi-transparent overlay effect
    /// - Absolute: Shows a solid overlay effect
    /// 
    /// These visual effects help users understand the spatial relationship between local space and portals.
    /// </remarks>
    public class OverlayController : MonoBehaviour
    {
        [SerializeField] private XRBaseInteractable _button;
        /// <summary>
        /// The button interactable that cycles through overlay modes when pressed.
        /// </summary>
        public XRBaseInteractable button
        {
            get => _button;
            set => _button = value;
        }

        [SerializeField] private PortalManager _portalManager;
        /// <summary>
        /// Reference to the PortalManager that manages the portals whose overlay appearance will be modified.
        /// </summary>
        public PortalManager portalManager
        {
            get => _portalManager;
            set => _portalManager = value;
        }

        [SerializeField] private TextMeshPro _text;
        /// <summary>
        /// Text component that displays the current overlay mode.
        /// </summary>
        public TextMeshPro text
        {
            get => _text;
            set => _text = value;
        }

        [SerializeField] private Material _opactiy;
        /// <summary>
        /// Material used for the blended opacity overlay effect.
        /// </summary>
        public Material opacity
        {
            get => _opactiy;
            set => _opactiy = value;
        }

        [SerializeField] private Material _contours;
        /// <summary>
        /// Material used for the contour lines overlay effect.
        /// </summary>
        public Material contours
        {
            get => _contours;
            set => _contours = value;
        }

        [SerializeField] private Material _contoursIncrease;
        /// <summary>
        /// Material used for the increasing contour lines overlay effect.
        /// </summary>
        public Material contoursIncrease
        {
            get => _contoursIncrease;
            set => _contoursIncrease = value;
        }

        [SerializeField] private Material _contoursDecrease;
        /// <summary>
        /// Material used for the decreasing contour lines overlay effect.
        /// </summary>
        public Material contoursDecrease
        {
            get => _contoursDecrease;
            set => _contoursDecrease = value;
        }

        [SerializeField] private Material _absolute;
        /// <summary>
        /// Material used for the absolute (solid) overlay effect.
        /// </summary>
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
