using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using VRPortalToolkit.XRI;

namespace VRPortalToolkit.Examples
{
    /// <summary>
    /// Manages interactions between different controller modes in a VR environment with portals.
    /// Handles teleportation, direct interaction, and snap turning based on the current context.
    /// </summary>
    /// <remarks>
    /// This component coordinates between different interaction modes to prevent conflicts:
    /// - Manages activation of teleport ray when appropriate
    /// - Disables teleportation and snap turning when holding certain objects
    /// - Ensures proper transitions between interaction states
    /// </remarks>
    public class PortableControllerManager : MonoBehaviour
    {
        [SerializeField] private XRPortableDirectInteractor _directInteractor;
        /// <summary>
        /// The direct interactor used for grabbing and manipulating objects.
        /// </summary>
        public XRPortableDirectInteractor directInteractor
        {
            get => _directInteractor;
            set => _directInteractor = value;
        }

        [SerializeField] private XRPortableRayInteractor _teleportInteractor;
        /// <summary>
        /// The ray interactor used for teleportation.
        /// </summary>
        public XRPortableRayInteractor teleportInteractor
        {
            get => _teleportInteractor;
            set => _teleportInteractor = value;
        }

        public InputActionReference _snapTurnAction;
        /// <summary>
        /// Input action for snap turning, which will be disabled when holding certain objects.
        /// </summary>
        public InputActionReference snapTurnAction
        {
            get => _snapTurnAction;
            set => _snapTurnAction = value;
        }

        [SerializeField] private InputActionProperty _teleportModeActivate;
        /// <summary>
        /// Input action that activates teleportation mode.
        /// </summary>
        public InputActionProperty teleportModeActivate
        {
            get => _teleportModeActivate;
            set => _teleportModeActivate = value;
        }

        [SerializeField] private InputActionProperty _teleportModeCancel;
        /// <summary>
        /// Input action that cancels teleportation mode.
        /// </summary>
        public InputActionProperty teleportModeCancel
        {
            get => _teleportModeCancel;
            set => _teleportModeCancel = value;
        }

        /// <summary>
        /// Whether teleportation mode is currently active.
        /// </summary>
        private bool _isTeleporting = false;
        
        /// <summary>
        /// Whether teleportation is currently allowed.
        /// </summary>
        private bool _canTeleport = true;
        
        /// <summary>
        /// Coroutine reference for delayed teleportation cancellation.
        /// </summary>
        private IEnumerator _waitThenCancel;

        /// <summary>
        /// Initializes the component and starts the update coroutine.
        /// </summary>
        protected virtual void Awake()
        {
            StartCoroutine(WaitForEndOfFrame());
        }

        /// <summary>
        /// Registers event handlers and initializes input actions when this component is enabled.
        /// </summary>
        protected virtual void OnEnable()
        {
            if (_directInteractor)
            {
                _directInteractor.selectEntered.AddListener(OnDirectInteractorSelectEntered);
                _directInteractor.selectExited.AddListener(OnDirectInteractorSelectExited);
            }

            if (_teleportModeActivate.action != null)
            {
                _teleportModeActivate.EnableDirectAction();
                _teleportModeActivate.action.performed += StartTeleport;
                _teleportModeActivate.action.canceled += CancelTeleport;
            }

            UpdateCanTeleport();
        }

        protected virtual void OnDisable()
        {
            if (_directInteractor)
            {
                _directInteractor.selectEntered.AddListener(OnDirectInteractorSelectEntered);
                _directInteractor.selectExited.AddListener(OnDirectInteractorSelectExited);
            }
        }

        private void StartTeleport(InputAction.CallbackContext _)
        {
            if (_canTeleport)
            {
                _isTeleporting = true;

                if (_teleportInteractor && !_teleportInteractor.gameObject.activeSelf)
                    _teleportInteractor.gameObject.SetActive(true);
            }
        }

        private void CancelTeleport(InputAction.CallbackContext _) => _isTeleporting = false;

        private IEnumerator WaitForEndOfFrame()
        {
            while (true)
        {
                yield return null;

                if (_isTeleporting && !_canTeleport) _isTeleporting = false;

                if (_teleportInteractor && !_isTeleporting && _teleportInteractor.gameObject.activeSelf)
                    _teleportInteractor.gameObject.SetActive(false);
            }
        }

        private void OnDirectInteractorSelectEntered(SelectEnterEventArgs _) => UpdateCanTeleport();

        private void OnDirectInteractorSelectExited(SelectExitEventArgs _) => UpdateCanTeleport();

        private void UpdateCanTeleport()
        {
            if (_directInteractor)
            {
                foreach (var interactable in _directInteractor.interactablesSelected)
                {
                    if (interactable.transform.GetComponent<XRPointAndPortal>())
                    {
                        _canTeleport = false;

                        if (_snapTurnAction && _snapTurnAction.action != null && _snapTurnAction.action.enabled)
                            _snapTurnAction.action.Disable();

                        return;
                    }
                }
            }

            _canTeleport = true;

            if (_snapTurnAction && _snapTurnAction.action != null && !_snapTurnAction.action.enabled)
                _snapTurnAction.action.Enable();
        }
    }
}
