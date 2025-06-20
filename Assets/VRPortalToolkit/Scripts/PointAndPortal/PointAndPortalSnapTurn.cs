using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.PointAndPortal
{
    /// <summary>
    /// Provides snap turning functionality during Point & Portal.
    /// Inspired by XR Interaction Toolkit's Snap Turn Provider.
    /// </summary>
    public class PointAndPortalSnapTurn : MonoBehaviour
    {
        [SerializeField, Tooltip("Minimum input amount required to trigger a turn.")]
        private float _turnThreshold = 0.5f;
        /// <summary>
        /// Minimum input amount required to trigger a turn.
        /// </summary>
        public float turnThreshold
        {
            get => _turnThreshold;
            set => _turnThreshold = value;
        }

        [SerializeField, Tooltip("Number of degrees to turn when triggering a left or right turn.")]
        private float _turnAmount = 30f;
        /// <summary>
        /// Number of degrees to turn when triggering a left or right turn.
        /// </summary>
        public float turnAmount
        {
            get => _turnAmount;
            set => _turnAmount = value;
        }

        [SerializeField, Tooltip("Time in seconds to wait before allowing another turn.")]
        private float _debounceTime = 0.5f;
        /// <summary>
        /// Time in seconds to wait before allowing another turn.
        /// </summary>
        public float debounceTime
        {
            get => _debounceTime;
            set => _debounceTime = value;
        }

        [SerializeField, Tooltip("Whether to enable left/right snap turns.")]
        private bool _enableTurnLeftRight = true;
        /// <summary>
        /// Whether to enable left/right snap turns.
        /// </summary>
        public bool enableTurnLeftRight
        {
            get => _enableTurnLeftRight;
            set => _enableTurnLeftRight = value;
        }

        [SerializeField, Tooltip("Whether to enable 180-degree snap turns.")]
        private bool _enableTurnAround = true;
        /// <summary>
        /// Whether to enable 180-degree snap turns.
        /// </summary>
        public bool enableTurnAround
        {
            get => _enableTurnAround;
            set => _enableTurnAround = value;
        }

        private float _lastTurnTime;

        private IPointAndPortal _pointAndPortal;
        /// <summary>
        /// Reference to the Point & Portal component.
        /// </summary>
        private IPointAndPortal pointAndPortal => _pointAndPortal;

        protected virtual void Awake()
        {
            _pointAndPortal = GetComponent<IPointAndPortal>();

            if (_pointAndPortal == null) Debug.LogError("IPointAndPortal not found!");
        }

        protected virtual void Update()
        {
            // Wait for a certain amount of time before allowing another turn.
            if (_lastTurnTime != 0f && _lastTurnTime + _debounceTime >= Time.time)
                return;

            _lastTurnTime = 0f;

            if (_pointAndPortal == null || _pointAndPortal.connected == null || _pointAndPortal.isPointing || _pointAndPortal.isTeleporting)
                return;

            float turn = GetTurnAmount(_pointAndPortal.input);

            if (Mathf.Abs(turn) > 0f)
            {
                _lastTurnTime = Time.time;

                PortalPhysics.ForceTeleport(_pointAndPortal.connected, () =>
                {
                    Plane groundPlane = _pointAndPortal.connectedGroundPlane;
                    Vector3 origin = pointAndPortal.connectedGroundPlane.ClosestPointOnPlane(pointAndPortal.connected.position);

                    _pointAndPortal.connected.RotateAround(origin, groundPlane.normal, turn);
                }, this);
            }
        }

        private float GetTurnAmount(Vector2 input)
        {
            if (input == Vector2.zero || input.magnitude < _turnThreshold)
                return 0f;

            float angle = Vector2.Angle(Vector2.up, input);

            if (angle < 45f) return 0f;

            if (angle > 135f) return _enableTurnAround ? 180f : 0f;

            if (_enableTurnLeftRight)
            {
                if (input.x < 0)
                    return -_turnAmount;

                return _turnAmount;
            }

            return 0f;
        }
    }
}
