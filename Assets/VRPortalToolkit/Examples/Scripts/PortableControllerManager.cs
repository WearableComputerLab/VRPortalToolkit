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
    public class PortableControllerManager : MonoBehaviour
    {
        [SerializeField] private XRPortableDirectInteractor _directInteractor;
        public XRPortableDirectInteractor directInteractor
        {
            get => _directInteractor;
            set => _directInteractor = value;
        }

        [SerializeField] private XRPortableRayInteractor _teleportInteractor;
        public XRPortableRayInteractor teleportInteractor
        {
            get => _teleportInteractor;
            set => _teleportInteractor = value;
        }

        public InputActionReference _snapTurnAction;
        public InputActionReference snapTurnAction
        {
            get => _snapTurnAction;
            set => _snapTurnAction = value;
        }

        [SerializeField] private InputActionProperty _teleportModeActivate;
        public InputActionProperty teleportModeActivate
        {
            get => _teleportModeActivate;
            set => _teleportModeActivate = value;
        }

        [SerializeField] private InputActionProperty _teleportModeCancel;
        public InputActionProperty teleportModeCancel
        {
            get => _teleportModeCancel;
            set => _teleportModeCancel = value;
        }

        private bool _isTeleporting = false;
        private bool _canTeleport = true;
        private IEnumerator _waitThenCancel;

        protected virtual void Awake()
        {
            StartCoroutine(WaitForEndOfFrame());
        }

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
