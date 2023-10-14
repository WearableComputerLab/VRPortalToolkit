using Misc.EditorHelpers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.PointAndPortal;
using static VRPortalToolkit.XRI.XRPortalInteractable;

namespace VRPortalToolkit.XRI
{
    [RequireComponent(typeof(XRPortalInteractable))]
    public class XRPointAndPortal : PointAndPortalBase
    {
        [Header("Controls")]
        [SerializeField] private LinkedMovement _linkedMovement = LinkedMovement.Anchored;
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
        [SerializeField] private LinkedMode _linkedMode = LinkedMode.Active;
        public LinkedMode linkedMode
        {
            get => _linkedMode;
            set => _linkedMode = value;
        }

        public enum LinkedMode
        {
            Active = 0,
            ToggleActivated = 1,
            ToggleDeactivated = 2,
        }

#if UNITY_EDITOR
        [ShowIf(nameof(usingLink))]
#endif
        [SerializeField] private bool _linkedActiveState = false;
        public bool linkedActiveState
        {
            get => _linkedActiveState;
            set => _linkedActiveState = value;
        }

#if UNITY_EDITOR
        [ShowIf(nameof(usingLink))]
#endif
        [SerializeField] private bool _forceLinkedWhilePointing = false;
        public bool forceLinkedWhilePointing
        {
            get => _forceLinkedWhilePointing;
            set => _forceLinkedWhilePointing = value;
        }

        [SerializeField] private float _inputThreshold = 0.5f;
        public float inputThreshold
        {
            get => _inputThreshold;
            set => _inputThreshold = value;
        }

        public override bool allowDirection => _orientationMode == OrientationMode.Directional;

        [SerializeField] private OrientationMode _orientationMode = OrientationMode.Directional;
        public OrientationMode orientationMode
        {
            get => _orientationMode;
            set => _orientationMode = value;
        }

        public enum OrientationMode
        {
            Forward = 0,
            Directional = 1
        }

        private Vector2 _input;
        public override Vector2 input => _input;

        private XRPortalInteractable _interactable;
        private IXRSelectInteractor _interactor;
        private XRBaseController _interactorController;
        private PortalRelativePosition _interactorPositioning;

        private bool _isActivating;
        public bool isActivating => _isActivating;

        public override Transform connected => _interactable != null && _interactable.connected != null ? _interactable.connected.transform : null;

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

        protected override void GetConnectedPointer(out Vector3 position, out Vector3 forward, out Vector3 up)
        {
            if (_interactor != null && _interactable)
            {
                forward = -transform.forward;
                up = _interactable.groundLevel ? _interactable.groundLevel.up : Vector3.up;

                Plane plane = new Plane(forward, transform.position);

                position = _interactor.transform.position;

                if (_interactorPositioning && _interactorPositioning.target)
                    position = _interactorPositioning.target.TransformPoint(_interactorPositioning.transform.InverseTransformPoint(position));

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

        /*private void PerformTeleportConnected(Vector3 cursorPos, Quaternion cursorRot)
        {
            if (_interactable && _interactable.connected)
            {
                float height;
                Quaternion rotation = _interactable.connected.transform.rotation;

                if (_interactable.connected.groundLevel)
                {
                    height = new Plane(_interactable.connected.groundLevel.up, _interactable.connected.groundLevel.position).GetDistanceToPoint(_interactable.connected.transform.position);
                    rotation = rotation * Quaternion.Inverse(_interactable.connected.groundLevel.rotation);
                }
                else
                    height = _interactable.connected.transform.position.y;

                Vector3 position = cursorPos + cursorRot * Vector3.up * height;

                // Remove rotation around forward
                rotation = Quaternion.AngleAxis(Vector3.SignedAngle(rotation * Vector3.forward, Vector3.forward, Vector3.up), Vector3.up) * rotation;
                rotation = cursorRot * rotation;

                _interactable.connected.transform.SetPositionAndRotation(position, rotation);
            }
        }*/
    }
}
