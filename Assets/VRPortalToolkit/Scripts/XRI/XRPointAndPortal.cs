using Misc.EditorHelpers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.PointAndPortal;
using static VRPortalToolkit.XRI.XRPortalInteractable;

namespace VRPortalToolkit.XRI
{
    /// <summary>
    /// XRI implementation of Point & Portal.
    /// </summary>
    [RequireComponent(typeof(XRPortalInteractable))]
    public class XRPointAndPortal : PointAndPortalBase
    {
        [Header("Controls")]
        [Tooltip("How the portal's movement is linked.")]
        [SerializeField] private LinkedMovement _linkedMovement = LinkedMovement.Anchored;
        /// <summary>
        /// How the portal's movement is linked.
        /// </summary>
        public LinkedMovement linkedMovement
        {
            get => _linkedMovement;
            set
            {
                if (_linkedMovement != value)
                {
                    _linkedMovement = value;
                    UpdateLinkedState();
                }
            }
        }

#if UNITY_EDITOR
        private bool usingLink => _linkedMovement != LinkedMovement.None;
        [ShowIf(nameof(usingLink))]
#endif
        [Tooltip("How the portal's link mode is set.")]
        [SerializeField] private LinkedMode _linkedMode = LinkedMode.Active;
        /// <summary>
        /// How the portal's link mode is set.
        /// </summary>
        public LinkedMode linkedMode
        {
            get => _linkedMode;
            set => _linkedMode = value;
        }

        /// <summary>
        /// The mode of linking for the portal.
        /// </summary>
        public enum LinkedMode
        {
            Active = 0,
            ToggleActivated = 1,
            ToggleDeactivated = 2,
        }

#if UNITY_EDITOR
        [ShowIf(nameof(usingLink))]
#endif
        [Tooltip("Whether the portal is active when linked.")]
        [SerializeField] private bool _linkedActiveState = false;
        /// <summary>
        /// Whether the portal is active when linked.
        /// </summary>
        public bool linkedActiveState
        {
            get => _linkedActiveState;
            set => _linkedActiveState = value;
        }

#if UNITY_EDITOR
        [ShowIf(nameof(usingLink))]
#endif
        [Tooltip("Force the portal to be linked while pointing.")]
        [SerializeField] private bool _forceLinkedWhilePointing = false;
        /// <summary>
        /// Force the portal to be linked while pointing.
        /// </summary>
        public bool forceLinkedWhilePointing
        {
            get => _forceLinkedWhilePointing;
            set => _forceLinkedWhilePointing = value;
        }

        [Tooltip("Input threshold for activating pointing.")]
        [SerializeField] private float _inputThreshold = 0.5f;
        /// <summary>
        /// Input threshold for activating pointing.
        /// </summary>
        public float inputThreshold
        {
            get => _inputThreshold;
            set => _inputThreshold = value;
        }

        /// <summary>
        /// Whether directional input is supported for portal placemented.
        /// </summary>
        public override bool allowDirection => _orientationMode == OrientationMode.Directional;

        [Tooltip("How the portal's orientation is handled.")]
        [SerializeField] private OrientationMode _orientationMode = OrientationMode.Directional;
        /// <summary>
        /// How the portal's orientation is handled.
        /// </summary>
        public OrientationMode orientationMode
        {
            get => _orientationMode;
            set => _orientationMode = value;
        }

        /// <summary>
        /// The mode of orientation for the portal.
        /// </summary>
        public enum OrientationMode
        {
            Forward = 0,
            Directional = 1
        }

        private Vector2 _input;
        /// <summary>
        /// The input vector.
        /// </summary>
        public override Vector2 input => _input;

        private XRPortalInteractable _interactable;
        private IXRSelectInteractor _interactor;
        private XRBaseController _interactorController;
        private PortalRelativePosition _interactorPositioning;

        private bool _isActivating;
        /// <summary>
        /// Whether the pointer is currently activating.
        /// </summary>
        public bool isActivating => _isActivating;

        /// <summary>
        /// The connected transform.
        /// </summary>
        public override Transform connected => _interactable != null && _interactable.connected != null ? _interactable.connected.transform : null;

        /// <summary>
        /// The ground plane for this portal.
        /// </summary>
        public override Plane groundPlane
        {
            get
            {
                if (_interactable)
                {
                    if (_interactable.groundLevel)
                        return new Plane(_interactable.groundLevel.up, _interactable.groundLevel.position);
                    else
                        return new Plane(Vector3.up, 0f);
                }

                return default;
            }
        }

        /// <summary>
        /// The ground plane for the connected portal.
        /// </summary>
        public override Plane connectedGroundPlane
        {
            get
            {
                if (_interactable && _interactable.connected)
                {
                    if (_interactable.connected.groundLevel)
                        return new Plane(_interactable.connected.groundLevel.up, _interactable.connected.groundLevel.position);
                    else
                        return new Plane(Vector3.up, 0f);
                }

                return default;
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            UpdateLinkedState();
        }

        protected virtual void Awake()
        {
            _interactable = GetComponent<XRPortalInteractable>();
        }

        protected virtual void OnEnable()
        {
            _interactable.selectEntered.AddListener(OnSelectEntered);
            _interactable.selectExited.AddListener(OnSelectExited);
            _interactable.activated.AddListener(OnActivated);
            _interactable.deactivated.AddListener(OnDeactivated);
        }

        protected virtual void OnDisable()
        {
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
            _interactable.selectExited.RemoveListener(OnSelectExited);
            _interactable.activated.RemoveListener(OnActivated);
            _interactable.deactivated.RemoveListener(OnDeactivated);
        }

        private void OnActivated(ActivateEventArgs args)
        {
            if (_linkedMode == LinkedMode.Active)
            {
                _isActivating = true;
                UpdateLinkedState();
            }
            else if (_linkedMode == LinkedMode.ToggleActivated)
            {
                _isActivating = !_isActivating;
                UpdateLinkedState();
            }
        }

        private void OnDeactivated(DeactivateEventArgs args)
        {
            if (_linkedMode == LinkedMode.Active)
            {
                _isActivating = false;
                UpdateLinkedState();
            }
            else if (_linkedMode == LinkedMode.ToggleDeactivated)
            {
                _isActivating = !_isActivating;
                UpdateLinkedState();
            }
        }

        private void UpdateLinkedState()
        {
            // Just in case we have let go, turn off isActivating
            if (_interactable && !_interactable.isSelected)
                _isActivating = false;

            bool linkedState = _isActivating ? _linkedActiveState : !_linkedActiveState;

            if (_forceLinkedWhilePointing && isPointing)
                linkedState = true;

            if (_interactable && _interactable.connected)
                _interactable.connected.linkedMovement = linkedState ? _linkedMovement : LinkedMovement.None;
        }

        protected virtual void Update()
        {
            Vector2 input = GetInput();

            UpdatePointer();

            if (!isPointing)
            {
                _input = input;

                if (input.y > _inputThreshold)
                    BeginPointing();
            }
            else if (input.magnitude < _inputThreshold)
                CompletePointing();
            else
                _input = input;
        }

        private Vector2 GetInput()
        {
            Vector2 input = Vector2.zero;
            if (_interactorController && _interactorController is ActionBasedController controller)
            {
                InputAction action = controller.directionalAnchorRotationAction.action;
                if (action != null) input = action.ReadValue<Vector2>();
            }

            return input;
        }

        private void OnSelectEntered(SelectEnterEventArgs _) => UpdatePointerState();

        private void OnSelectExited(SelectExitEventArgs _) => UpdatePointerState();

        private void UpdatePointerState()
        {
            if (_interactor != null)
            {
                if (!_interactable.interactorsSelecting.Contains(_interactor))
                {
                    _interactor = null;
                    _interactorController = null;
                    //_interactorPositioning = null;
                }
            }

            if (_interactor == null)
            {
                if (_interactable.isSelected)
                {
                    _interactor = _interactable.interactorsSelecting[0];
                    _interactorController = _interactor.transform.gameObject.GetComponentInParent<XRBaseController>();
                    _interactorPositioning = _interactor.transform.gameObject.GetComponentInParent<PortalRelativePosition>();
                }
                else
                    CancelPointing();
            }
        }

        /// <inheritdoc/>
        protected override void GetConnectedPointer(out Vector3 position, out Vector3 forward, out Vector3 up)
        {
            if (_interactor != null && _interactable)
            {
                forward = -transform.forward;
                up = _interactable.groundLevel ? _interactable.groundLevel.up : Vector3.up;

                Plane plane = new Plane(forward, transform.position);

                position = _interactor.transform.position;

                if (_interactorPositioning && _interactorPositioning.source)
                    position = _interactorPositioning.source.TransformPoint(_interactorPositioning.transform.InverseTransformPoint(position));

                position = plane.ClosestPointOnPlane(position);

                if (_interactable.portal)
                {
                    _interactable.portal.ModifyDirection(ref forward);
                    _interactable.portal.ModifyDirection(ref up);
                    _interactable.portal.ModifyPoint(ref position);
                }
            }
            else
                position = forward = up = Vector3.zero;
        }

    }
}
