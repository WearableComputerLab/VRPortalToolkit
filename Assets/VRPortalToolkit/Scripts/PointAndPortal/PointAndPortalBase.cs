using Misc.EditorHelpers;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.PointAndPortal
{
    public interface IPointAndPortal : IPortalLineRenderable, IPortalCursorRenderable
    {
        Vector2 input { get; }

        bool isPointing { get; }

        bool isTeleporting { get; }
        
        Plane groundPlane { get; }

        Transform connected { get; }

        Plane connectedGroundPlane { get; }

        bool TryGetTeleportConnectedPose(out Pose pose, out bool isValidTarget);
    }

    public abstract class PointAndPortalBase : MonoBehaviour, IPointAndPortal
    {
        private readonly static int MaxPortals = 10;
        private readonly static PortalRay[] castPortalRays = new PortalRay[MaxPortals];

        [SerializeField] private LayerMask _portalMask = 1 << 3;
        public virtual LayerMask portalMask
        {
            get => _portalMask;
            set => _portalMask = value;
        }

        [SerializeField] private QueryTriggerInteraction _portalTriggerInteraction;
        public virtual QueryTriggerInteraction portalTriggerInteraction
        {
            get => _portalTriggerInteraction;
            set => _portalTriggerInteraction = value;
        }

        [SerializeField] private LayerMask _raycastMask = ~0 & ~(1 << 2) & ~(1 << 3);
        public virtual LayerMask raycastMask
        {
            get => _raycastMask;
            set => _raycastMask = value;
        }

        [SerializeField] private LayerMask _validMask = ~0 & ~(1 << 2) & ~(1 << 3);
        public virtual LayerMask validMask
        {
            get => _validMask;
            set => _validMask = value;
        }

        [SerializeField] private QueryTriggerInteraction _raycastTriggerInteraction;
        public virtual QueryTriggerInteraction raycastTriggerInteraction
        {
            get => _raycastTriggerInteraction;
            set => _raycastTriggerInteraction = value;
        }

        [Header("Pointer")]
        [SerializeField] private LineType _lineType;
        public LineType lineType
        {
            get => _lineType;
            set => _lineType = value;
        }

        public enum LineType
        {
            Straight = 0,
            ProjectileCurve = 1,
            BezierCurve = 2,
        }

        [ShowIf(nameof(_lineType), LineType.Straight)]
        [SerializeField] private float _maxRaycastDistance = 30f;
        /// <seealso cref="LineType.StraightLine"/>
        public float maxRaycastDistance
        {
            get => _maxRaycastDistance;
            set => _maxRaycastDistance = value;
        }

#if UNITY_EDITOR
        private bool isProjectileOrBezier => _lineType == LineType.BezierCurve || _lineType == LineType.ProjectileCurve;
        [ShowIf(nameof(isProjectileOrBezier))]
#endif
        [SerializeField] private Transform _referenceFrame;
        /// <seealso cref="LineType.ProjectileCurve"/>
        /// <seealso cref="LineType.BezierCurve"/
        public Transform referenceFrame
        {
            get => _referenceFrame;
            set => _referenceFrame = value;
        }

        [ShowIf(nameof(_lineType), LineType.ProjectileCurve)]
        [SerializeField] private float _velocity = 16f;
        /// <seealso cref="LineType.ProjectileCurve"/>
        public float velocity
        {
            get => _velocity;
            set => _velocity = value;
        }

        [ShowIf(nameof(_lineType), LineType.ProjectileCurve)]
        [SerializeField] private float _acceleration = 9.8f;
        /// <seealso cref="LineType.ProjectileCurve"/>
        public float acceleration
        {
            get => _acceleration;
            set => _acceleration = value;
        }

        [ShowIf(nameof(_lineType), LineType.ProjectileCurve)]
        [SerializeField] private float _additionalGroundHeight = 0.1f;
        /// <summary>
        /// Additional height below ground level that the projectile will continue to.
        /// Increasing this value will make the end point drop lower in height.
        /// </summary>
        /// <seealso cref="LineType.ProjectileCurve"/>
        public float additionalGroundHeight
        {
            get => _additionalGroundHeight;
            set => _additionalGroundHeight = value;
        }

        [ShowIf(nameof(_lineType), LineType.ProjectileCurve)]
        [SerializeField] private float _additionalFlightTime = 0.5f;
        /// <seealso cref="LineType.ProjectileCurve"/>
        public float additionalFlightTime
        {
            get => _additionalFlightTime;
            set => _additionalFlightTime = value;
        }

        [ShowIf(nameof(_lineType), LineType.BezierCurve)]
        [SerializeField] private float _endPointDistance = 30f;
        /// <seealso cref="LineType.BezierCurve"/>
        public float endPointDistance
        {
            get => _endPointDistance;
            set => _endPointDistance = value;
        }
        [ShowIf(nameof(_lineType), LineType.BezierCurve)]

        [SerializeField] private float _endPointHeight = -10f;
        /// <seealso cref="LineType.BezierCurve"/>
        public float endPointHeight
        {
            get => _endPointHeight;
            set => _endPointHeight = value;
        }

        [ShowIf(nameof(_lineType), LineType.BezierCurve)]
        [SerializeField] private float _controlPointDistance = 10f;
        /// <seealso cref="LineType.BezierCurve"/>
        public float controlPointDistance
        {
            get => _controlPointDistance;
            set => _controlPointDistance = value;
        }

        [ShowIf(nameof(_lineType), LineType.BezierCurve)]
        [SerializeField] private float _controlPointHeight = 5f;
        /// <seealso cref="LineType.BezierCurve"/>
        public float controlPointHeight
        {
            get => _controlPointHeight;
            set => _controlPointHeight = value;
        }

        [ShowIf(nameof(_lineType), LineType.BezierCurve)]
        [SerializeField] private int _sampleFrequency = 20;
        /// <seealso cref="LineType.ProjectileCurve"/>
        /// <seealso cref="LineType.BezierCurve"/>
        public int sampleFrequency
        {
            get => _sampleFrequency;
            set => _sampleFrequency = Mathf.Max(value, 2);
        }

        public int portalRayCount => _isPointing ? _portalRaysCount : 0;

        /// <summary>
        /// If pointing is active.
        /// </summary>
        public bool isPointing => _isPointing;
        private bool _isPointing;

        /// <summary>
        /// If this is in the process of teleporting the connected side.
        /// </summary>
        public bool isTeleporting => _isTeleporting;
        private bool _isTeleporting;

        public abstract Vector2 input { get; }

        public abstract bool allowDirection { get; }

        public abstract Transform connected { get; }

        public abstract Plane groundPlane { get; }

        public abstract Plane connectedGroundPlane { get; }

        private readonly Vector3[] m_ControlPoints = new Vector3[3];
        private readonly List<Vector3> _samplePoints = new List<Vector3>();
        private PortalRay[] _portalRays;
        private int _portalRaysCount;
        private int _portalIndex;
        private RaycastHit _hitInfo;
        private bool _isValid;
        private Quaternion _cursorRot;

        // Need a way to manually set the input
        // So that should just be a joystick? or should there be a separate on and off?
        // Need a setting for if this is directional
        // Need a setting to say that the pointer is up
        // Need a setting to say that it is actively performing a teleport?
        // I think rotate will be a different class
        // Probably create master input class that can be incharge of managing everything to do with portal input
        // 

        protected virtual void OnValidate()
        {
            _sampleFrequency = Mathf.Max(_sampleFrequency, 2);
        }

        protected abstract void GetConnectedPointer(out Vector3 position, out Vector3 forward, out Vector3 up);

        protected virtual void TeleportConnected(Pose connectedPose)
        {
            if (connected) PortalPhysics.ForceTeleport(connected, () =>
                connected.SetPositionAndRotation(connectedPose.position, connectedPose.rotation), this);
        }

        protected void BeginPointing()
        {
            if (!_isPointing && !_isTeleporting)
            {
                _isPointing = true;
                _portalRaysCount = 0;
                _portalIndex = -1;
                _isValid = false;
            }
        }

        protected void CompletePointing()
        {
            if (_isPointing && !isTeleporting)
            {
                if (TryGetTeleportConnectedPose(out Pose connectedPose, out bool isValidTarget) && isValidTarget)
                {
                    _isTeleporting = true;
                    TeleportConnected(connectedPose);
                    _isTeleporting = false;
                }

                _isPointing = false;
            }
        }

        protected void CancelPointing()
        {
            if (_isPointing && !_isTeleporting)
                _isPointing = false;
        }

        protected void UpdatePointer()
        {
            _portalRaysCount = 0;
            _portalIndex = -1;
            _isValid = false;

            if (_isPointing)
            {
                GetConnectedPointer(out Vector3 position, out Vector3 forward, out Vector3 up);
                UpdateSamplePoints(position, forward, up);

                if (_portalRays == null || _portalRays.Length - MaxPortals < _samplePoints.Count)
                    _portalRays = new PortalRay[_samplePoints.Count + MaxPortals];

                if (_samplePoints.Count > 1)
                {
                    Matrix4x4 teleportMatrix = Matrix4x4.identity;

                    // Now we portal cast down all those lines, tracking each portal we hit
                    Vector3 from = _samplePoints[0], to;
                    for (int i = 1; i < _samplePoints.Count && _portalRaysCount < _portalRays.Length; i++)
                    {
                        to = teleportMatrix.MultiplyPoint3x4(_samplePoints[i]);
                        Vector3 direction = (to - from).normalized;
                        float maxDistance = Vector3.Distance(to, from);

                        int castRayCount = PortalPhysics.GetRays(from, direction, castPortalRays, maxDistance, _portalMask, _portalTriggerInteraction);

                        for (int j = 0; j < castRayCount && _portalRaysCount < _portalRays.Length; j++)
                        {
                            _portalRays[_portalRaysCount++] = castPortalRays[j];

                            Portal portal = castPortalRays[j].fromPortal;

                            if (portal)
                            {
                                teleportMatrix = castPortalRays[j].fromPortal.teleportMatrix * teleportMatrix;
                                castPortalRays[j].fromPortal.ModifyPoint(ref to);
                            }
                        }

                        from = to;
                    }

                    // Now actually raycast
                    if (PortalPhysics.Raycast(_portalRays, _portalRaysCount, out _hitInfo, out _portalIndex, _raycastMask, _raycastTriggerInteraction))
                    {
                        int layer = _hitInfo.collider.gameObject.layer;
                        _isValid = ((uint)(int)_validMask & (1 << layer)) > 0;

                        Vector3 newForward = forward;
                        for (int i = 0; i < _portalIndex; i++)
                            _portalRays[i].fromPortal?.ModifyDirection(ref newForward);

                        newForward = Vector3.ProjectOnPlane(newForward, _hitInfo.normal);

                        if (newForward == Vector3.zero)
                            newForward = Vector3.Slerp(_hitInfo.normal, -_hitInfo.normal, 0.5f);

                        _cursorRot = Quaternion.LookRotation(newForward, _hitInfo.normal);
                        
                        Vector2 direction = input;

                        if (direction == Vector2.zero || !allowDirection)
                            direction = Vector2.up;

                        _cursorRot *= Quaternion.AngleAxis(Vector2.SignedAngle(direction.normalized, Vector2.up), _hitInfo.normal);
                    }
                }
            }
        }

        public PortalRay GetPortalRay(int portalRayIndex) => _portalRays[portalRayIndex];

        public bool TryGetHitInfo(out Vector3 position, out Vector3 normal, out int portalRayIndex, out bool isValidTarget)
        {
            if (_isPointing && _portalIndex >= 0)
            {
                position = _hitInfo.point;
                normal = _hitInfo.normal;
                portalRayIndex = _portalIndex;
                isValidTarget = _isValid;

                return true;
            }

            position = normal = default;
            portalRayIndex = -1;
            isValidTarget = false;
            return false;
        }

        public bool TryGetCursor(out Pose cursorPose, out bool isValidTarget)
        {
            if (_isPointing && _portalIndex >= 0)
            {
                cursorPose = new Pose(_hitInfo.point, _cursorRot);
                isValidTarget = _isValid;

                return true;
            }

            cursorPose = default;
            isValidTarget = false;
            return false;
        }

        public bool TryGetTeleportConnectedPose(out Pose pose, out bool isValidTarget)
        {
            if (TryGetCursor(out Pose cursorPose, out isValidTarget) && connected)
            {
                Plane groundPlane = this.connectedGroundPlane;

                float height = groundPlane.GetDistanceToPoint(connected.position);
                pose.position = cursorPose.position + cursorPose.rotation * Vector3.up * height;

                pose.rotation = connected.rotation;
                Vector3 flatForward = Vector3.ProjectOnPlane(connected.forward, groundPlane.normal);

                if (flatForward == Vector3.zero)
                    flatForward = Vector3.Slerp(groundPlane.normal, -groundPlane.normal, 0.5f);

                // Remove rotation around forward and ground
                pose.rotation = Quaternion.Inverse(Quaternion.LookRotation(flatForward, groundPlane.normal)) * pose.rotation;

                // Add rotation around cursor
                pose.rotation = cursorPose.rotation * pose.rotation;

                return true;
            }

            pose = default;
            return false;
        }

        private void UpdateSamplePoints(Vector3 origin, Vector3 forward, Vector3 up)
        {
            _samplePoints.Clear();
            _samplePoints.Add(origin);

            switch (_lineType)
            {
                case LineType.ProjectileCurve:
                    {
                        CalculateProjectileParameters(origin, forward, up, out var initialVelocity, out var constantAcceleration, out var flightTime);

                        var interval = flightTime / (_sampleFrequency - 1);
                        for (var i = 1; i < _sampleFrequency; ++i)
                        {
                            var time = i * interval;
                            _samplePoints.Add(SampleProjectilePoint(origin, initialVelocity, constantAcceleration, time));
                        }
                    }
                    break;
                case LineType.BezierCurve:
                    {
                        // Update control points for Bezier curve
                        UpdateBezierControlPoints(origin, forward, up);
                        var p0 = m_ControlPoints[0];
                        var p1 = m_ControlPoints[1];
                        var p2 = m_ControlPoints[2];

                        var interval = 1f / (_sampleFrequency - 1);
                        for (var i = 1; i < _sampleFrequency; ++i)
                        {
                            // Parametric parameter t where 0 <= t <= 1
                            var percent = i * interval;
                            _samplePoints.Add(SampleQuadraticBezierPoint(p0, p1, p2, percent));
                        }
                    }
                    break;
                case LineType.Straight:
                    _samplePoints.Add(origin + forward * _maxRaycastDistance);
                    break;
            }
        }

        private void UpdateBezierControlPoints(Vector3 origin, Vector3 forward, Vector3 up)
        {
            m_ControlPoints[0] = origin;
            m_ControlPoints[1] = m_ControlPoints[0] + forward * _controlPointDistance + up * _controlPointHeight;
            m_ControlPoints[2] = m_ControlPoints[0] + forward * _endPointDistance + up * _endPointHeight;
        }

        private void CalculateProjectileParameters(Vector3 origin, Vector3 forward, Vector3 up, out Vector3 initialVelocity, out Vector3 constantAcceleration, out float flightTime)
        {
            initialVelocity = forward * _velocity;
            var referencePosition = _referenceFrame != null ? _referenceFrame.position : Vector3.zero;
            constantAcceleration = up * -_acceleration;

            Vector3 projectedForward = Vector3.ProjectOnPlane(forward, up);
            float angle = Mathf.Approximately(Vector3.Angle(forward, projectedForward), 0f)
                ? 0f
                : Vector3.SignedAngle(forward, projectedForward, Vector3.Cross(up, forward));

            var vy = _velocity * Mathf.Sin(angle * Mathf.Deg2Rad);
            var height = Vector3.Project(referencePosition - origin, up).magnitude + _additionalGroundHeight;
            if (height < 0f)
                flightTime = _additionalFlightTime;
            else if (Mathf.Approximately(height, 0f))
                flightTime = 2f * vy / _acceleration + _additionalFlightTime;
            else
                flightTime = (vy + Mathf.Sqrt(vy * vy + 2f * _acceleration * height)) / _acceleration + _additionalFlightTime;

            flightTime = Mathf.Max(flightTime, 0f);
        }

        private static Vector3 SampleQuadraticBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            var u = 1f - t; // (1 - t)
            var uu = u * u; // (1 - t)2
            var tt = t * t; // t2

            return (uu * p0) + (2f * u * t * p1) + (tt * p2);
        }

        private static Vector3 SampleProjectilePoint(Vector3 initialPosition, Vector3 initialVelocity, Vector3 constantAcceleration, float time) =>
            initialPosition + initialVelocity * time + constantAcceleration * (0.5f * time * time);
    }
}
