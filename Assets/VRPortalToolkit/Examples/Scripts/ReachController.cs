using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.XRI;

namespace VRPortalToolkit.Examples
{
    public class ReachController : MonoBehaviour
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
            state = (state + 1) % 5;

            switch (state)
            {
                case 1: // Exit
                    UpdateState(true, false, 1f);
                    if (_text) _text.text = "AP-Reach\n(Exit)";
                    break;
                case 2: // Entry
                    UpdateState(true, false, 0f);
                    if (_text) _text.text = "AP-Reach\n(Entry)";
                    break;
                case 3: // Composite
                    UpdateState(true, false, 0.5f);
                    if (_text) _text.text = "AP-Reach\n(Composite)";
                    break;
                case 4: // Hand
                    UpdateState(false, true);
                    if (_text) _text.text = "PH-Reach";
                    break;
                default: // Default
                    UpdateState(false, false);
                    if (_text) _text.text = "Natural Reach";
                    break;
            }
        }

        private void UpdateState(bool apReach, bool phReach, float ratio = 1f)
        {
            if (_portalManager)
            {
                foreach (var portal in _portalManager.portalPairs)
                {
                    foreach (var xrAdaptivePortalReach in portal.GetComponentsInChildren<XRAdaptivePortalReach>(true))
                    {
                        xrAdaptivePortalReach.enabled = apReach;
                        xrAdaptivePortalReach.ratio = ratio;
                    }

                    foreach (var xrPortalHandReach in portal.GetComponentsInChildren<XRPortalHandReach>(true))
                        xrPortalHandReach.enabled = phReach;
                }
            }
        }
    }
}
