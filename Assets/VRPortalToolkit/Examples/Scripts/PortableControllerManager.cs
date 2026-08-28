using System.Collections;
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

        [SerializeField] private XRPortableRayInteractor _rayInteractor;
        /// <summary>
        /// The ray interactor used for grabbing and manipulating objects.
        /// </summary>
        public XRPortableRayInteractor rayInteractor
        {
            get => _rayInteractor;
            set => _rayInteractor = value;
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

        private bool _isTeleporting = false;
        private bool _canTeleport = true;
        private InteractorState _directState;
        private InteractorState _rayState;

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

            if (_rayInteractor)
            {
                _rayInteractor.selectEntered.AddListener(OnRayInteractorSelectEntered);
                _rayInteractor.selectExited.AddListener(OnRayInteractorSelectExited);
            }

            if (_teleportModeActivate.action != null)
            {
                _teleportModeActivate.EnableDirectAction();
                _teleportModeActivate.action.performed += StartTeleport;
                _teleportModeActivate.action.canceled += CancelTeleport;
            }

            UpdateState();
        }

        protected virtual void OnDisable()
        {
            if (_directInteractor)
            {
                _directInteractor.selectEntered.RemoveListener(OnDirectInteractorSelectEntered);
                _directInteractor.selectExited.RemoveListener(OnDirectInteractorSelectExited);
            }

            if (_rayInteractor)
            {
                _rayInteractor.selectEntered.RemoveListener(OnRayInteractorSelectEntered);
                _rayInteractor.selectExited.RemoveListener(OnRayInteractorSelectExited);
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

                if (_rayInteractor)
                {
                    if ((_isTeleporting && _rayState == InteractorState.Empty)
                        || _directState != InteractorState.Empty)
                        _rayInteractor.gameObject.SetActive(false);
                    else
                        _rayInteractor.gameObject.SetActive(true);
                }

                if (_directInteractor)
                {
                    if (_rayState != InteractorState.Empty)
                        _directInteractor.gameObject.SetActive(false);
                    else
                        _directInteractor.gameObject.SetActive(true);
                }

                if (_teleportInteractor && !_isTeleporting && _teleportInteractor.gameObject.activeSelf)
                    _teleportInteractor.gameObject.SetActive(false);
            }
        }

        private void OnRayInteractorSelectEntered(SelectEnterEventArgs _) => UpdateState();

        private void OnRayInteractorSelectExited(SelectExitEventArgs _) => UpdateState();

        private void OnDirectInteractorSelectEntered(SelectEnterEventArgs _) => UpdateState();

        private void OnDirectInteractorSelectExited(SelectExitEventArgs _) => UpdateState();

        private void UpdateState()
        {
            _directState = GetInteractorState(_directInteractor);
            _rayState = GetInteractorState(_rayInteractor);

            if (_directState == InteractorState.SelectingPortal || _rayState == InteractorState.SelectingPortal)
            {
                _canTeleport = false;

                if (_snapTurnAction && _snapTurnAction.action != null && _snapTurnAction.action.enabled)
                    _snapTurnAction.action.Disable();
            }
            else
            {
                _canTeleport = true;

                if (_snapTurnAction && _snapTurnAction.action != null && !_snapTurnAction.action.enabled)
                    _snapTurnAction.action.Enable();
            }
        }

        private InteractorState GetInteractorState(XRBaseInteractor interactor)
        {
            if (interactor && interactor.isActiveAndEnabled)
            {
                foreach (var interactable in interactor.interactablesSelected)
                {
                    if (interactable.transform.GetComponent<XRPointAndPortal>())
                        return InteractorState.SelectingPortal;
                }

                if (interactor.interactablesSelected.Count > 0)
                    return InteractorState.SelectingInteractable;
            }

            return InteractorState.Empty;
        }

        private enum InteractorState : byte
        {
            Empty = 0,
            SelectingInteractable = 1,
            SelectingPortal = 2,
        }
    }
}
