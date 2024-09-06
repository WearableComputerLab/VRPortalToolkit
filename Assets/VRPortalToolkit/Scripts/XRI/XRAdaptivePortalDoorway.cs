using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Data;
using VRPortalToolkit.XRI;

namespace VRPortalToolkit
{
    [RequireComponent(typeof(AdaptivePortal))]
    public class XRAdaptivePortalDoorway : MonoBehaviour, IAdaptivePortalProcessor
    {
        [SerializeField] private XRAdaptivePortalDoorway _connected;
        public XRAdaptivePortalDoorway connected
        {
            get => _connected;
            set => _connected = value;
        }

        [SerializeField] private Vector2 _doorwaySize = new Vector2(0.8f, 2f);
        public Vector2 doorwaySize
        {
            get => _doorwaySize;
            set => _doorwaySize = value;
        }

        [SerializeField] private float _transitionTime = 1f;
        public float transformTime
        {
            get => _transitionTime;
            set => _transitionTime = value;
        }

        [Tooltip("If the portal is thrown to the ground faster than this, it will turn into a doorway.")]
        [SerializeField] private float _dropVelocityThreshold = 1f;
        public float dropVelocityThreshold
        {
            get => _dropVelocityThreshold;
            set => _dropVelocityThreshold = value;
        }

        [SerializeField] private bool _isDoorway;
        public bool isDoorway
        {
            get => _isDoorway;
            set
            {
                if (_isDoorway != value)
                {
                    _isDoorway = value;
                    _lastState = value;
                    
                    if (_connected)
                    {
                        _connected._lastState = value;
                        _connected._isDoorway = value;
                    }

                    if (isActiveAndEnabled && Application.isPlaying)
                    {
                        if (_isDoorway)
                        {
                            UnselectAll();
                            _connected?.UnselectAll();

                            UpdateUpright();
                            UpdateGroundLevel();
                            _fromPose = new Pose(transform.position, transform.rotation);

                            if (_connected)
                            {
                                if (_upright % 2 == 0)
                                    _connected._upright = _upright;
                                else
                                    _connected._upright = (_upright + 2) % 4;


                                _connected.UpdateGroundLevel();
                                _connected._fromPose = new Pose(_connected.transform.position, _connected.transform.rotation);
                            }
                        }
                    }
                }
            }
        }

        private Vector3[] _velocityFrames = new Vector3[5];
        private int _velocityIndex = 0;
        private Vector3 _lastPosition; // TODO: Should update this if there is a teleportation

        private bool _lastState;
        private int _upright = 0;
        private float _scale;
        private Pose _fromPose;
        private Pose _groundLevel;

        int IAdaptivePortalProcessor.Order => 100;

        private XRPortalInteractable _interactable;

        private void UnselectAll()
        {
            var manager = _interactable.interactionManager;

            if (manager)
            {
                for (int i = _interactable.interactorsSelecting.Count - 1; i >= 0; i--)
                {
                    if (i < _interactable.interactorsSelecting.Count)
                        manager.SelectExit(_interactable.interactorsSelecting[i], _interactable);
                }
            }
        }

        private readonly Vector3[] _directions = new Vector3[]
        {
            Vector3.up,
            Vector3.right,
            Vector3.down,
            Vector3.left
        };

        private void UpdateUpright()
        {
            Vector3 up = _interactable.groundLevel ? _interactable.groundLevel.up : Vector3.up;

            _upright = 0;
            float bestAngle = float.MaxValue;

            for (int i = 0; i < _directions.Length; i++)
            {
                float angle = Vector3.Angle(transform.TransformDirection(_directions[i]), up);

                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    _upright = i;
                }
            }
        }

        private void UpdateGroundLevel()
        {
            Plane groundPlane = _interactable.groundLevel ? new Plane(_interactable.groundLevel.up, _interactable.groundLevel.position) : new Plane(Vector3.up, 0);

            Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, groundPlane.normal);

            if (projectedForward == Vector3.zero)
                projectedForward = _interactable.groundLevel ? _interactable.groundLevel.forward : Vector3.forward;
            else
                projectedForward = projectedForward.normalized;

            _groundLevel = new Pose(groundPlane.ClosestPointOnPlane(transform.position), Quaternion.LookRotation(projectedForward, groundPlane.normal));
        }

        protected virtual void OnValidate()
        {
            if (Application.isPlaying && _isDoorway != _lastState)
            {
                _isDoorway = !_isDoorway;
                isDoorway = !_isDoorway;
            }
        }
        protected virtual void Awake()
        {
            _lastState = _isDoorway;
            _interactable = GetComponent<XRPortalInteractable>();
        }

        protected virtual void LateUpdate()
        {
            if (_connected && _isDoorway != _connected.isDoorway)
                isDoorway = _connected.isDoorway;

            float step = _transitionTime <= 0f ? 1f : Time.deltaTime / _transitionTime;

            if (_isDoorway)
            {
                _scale = Mathf.Min(1f, _scale + step);

                UpdateGroundLevel();
                if (_connected) _connected.UpdateGroundLevel();

                transform.position = Vector3.Lerp(_fromPose.position, _groundLevel.position + _groundLevel.up * _doorwaySize.y * 0.5f, _scale);
                transform.rotation = Quaternion.Lerp(_fromPose.rotation, _groundLevel.rotation * Quaternion.AngleAxis(90f * _upright, Vector3.forward), _scale);
            }
            else
            {
                _velocityFrames[_velocityIndex] = (transform.position - _lastPosition) / Time.deltaTime;
                _velocityIndex = (_velocityIndex + 1) % _velocityFrames.Length;
                _lastPosition = transform.position;
                _scale = Mathf.Max(0f, _scale - step);
            }
        }

        protected virtual void OnEnable()
        {
            _interactable.selectEntered.AddListener(OnSelectEntered);
            _interactable.selectExited.AddListener(OnSelectExited);
        }

        protected virtual void OnDisable()
        {
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
            _interactable.selectExited.RemoveListener(OnSelectExited);
        }

        private void OnSelectEntered(SelectEnterEventArgs _)
        {
            for (int i = 0; i < _velocityFrames.Length; i++)
                _velocityFrames[i] = Vector3.zero;

            isDoorway = false;
        }

        private void OnSelectExited(SelectExitEventArgs _)
        {
            if (!_isDoorway && _dropVelocityThreshold >= 0f)
            {
                if (_dropVelocityThreshold == 0)
                    isDoorway = true;
                else
                {
                    // Check if its been thrown at the floor
                    Vector3 averageVelocity = Vector3.zero;

                    for (int i = 0; i < _velocityFrames.Length; i++)
                        averageVelocity += _velocityFrames[i];

                    averageVelocity /= _velocityFrames.Length;

                    Vector3 down = _interactable.groundLevel ? -_interactable.groundLevel.up : Vector3.down;

                    float distance = Vector3.Dot(averageVelocity, down);

                    if (distance > _dropVelocityThreshold)
                        isDoorway = true;
                }
            }
        }

        void IAdaptivePortalProcessor.Process(ref AdaptivePortalTransform apTransform)
        {
            if (!isActiveAndEnabled) return;

            apTransform.entryDepth = Mathf.Lerp(apTransform.entryDepth, 0f, _scale);
            apTransform.exitDepth = Mathf.Lerp(apTransform.exitDepth, 0f, _scale);

            Vector2 size = _doorwaySize;

            if (_upright % 2 != 0) // horizontal
                size = new Vector2(size.y, size.x);

            apTransform.min = Vector2.Lerp(apTransform.min, - size * 0.5f, _scale);
            apTransform.max = Vector2.Lerp(apTransform.max, size * 0.5f, _scale);
            apTransform.minSize = Vector2.Lerp(apTransform.minSize, size, _scale);
        }
    }
}
