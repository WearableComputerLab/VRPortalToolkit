using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRPortalToolkit.Physics;
using VRPortalToolkit.XRI;

namespace VRPortalToolkit
{
    /// <summary>
    /// Handles reach gain for adaptive XR portals.
    /// </summary>
    [DefaultExecutionOrder(1)]
    [RequireComponent(typeof(XRPortalInteractable))]
    public class XRAdaptivePortalReach : MonoBehaviour, IAdaptivePortalProcessor
    {
        [Tooltip("The connected reach processor.")]
        [SerializeField] private XRAdaptivePortalReach _connected;
        /// <summary>
        /// The connected reach processor.
        /// </summary>
        public XRAdaptivePortalReach connected
        {
            get => _connected;
            set => _connected = value;
        }

        [Tooltip("The gain curve for reach calculation.")]
        [SerializeField] private AnimationCurve _gainCurve = AnimationCurve.Linear(0f, 0f, 1f, 0.5f);
        /// <summary>
        /// The gain curve for reach calculation.
        /// </summary>
        public AnimationCurve gainCurve
        {
            get => _gainCurve;
            set => _gainCurve = value;
        }

        [Tooltip("The ratio of reach gain to apply.")]
        [Range(0f, 1f)]
        [SerializeField] private float _ratio = 1f;
        /// <summary>
        /// The ratio of reach gain to apply.
        /// </summary>
        public float ratio
        {
            get => _ratio;
            set => _ratio = Mathf.Clamp01(value);
        }

        /// <inheritdoc/>
        int IAdaptivePortalProcessor.Order => 0;

        private XRPortalInteractable _interactable;
        private Portal _portal;

        private readonly List<PortalRelativePosition> _positionings = new List<PortalRelativePosition>();

        protected virtual void Reset()
        {
            _portal = GetComponentInChildren<Portal>();
        }

        protected virtual void Awake()
        {
            _interactable = GetComponent<XRPortalInteractable>();
        }

        protected virtual void OnEnable()
        {
            _portal = _interactable?.portal;
            AddPortalListener();
        }

        protected virtual void LateUpdate()
        {
            _positionings.RemoveAll(IsInvalid);
        }

        protected virtual void OnDisable()
        {
            RemovePortalListener();
        }

        private bool IsInvalid(PortalRelativePosition positioning)
        {
            if (positioning)
            {
                if (!positioning.GetPortalsFromSource().Contains(_portal))
                    return true;
            }

            return false;
        }

        private void AddPortalListener()
        {
            if (_portal != null) _portal.postTeleport += OnPortalPostTeleport;
        }

        private void RemovePortalListener()
        {
            if (_portal != null) _portal.postTeleport -= OnPortalPostTeleport;
        }

        private void OnPortalPostTeleport(Teleportation teleportation)
        {
            if (teleportation.target && teleportation.target.gameObject.TryGetComponent(out PortalRelativePosition positioning))
                _positionings.Add(positioning);
        }

        /// <inheritdoc/>
        void IAdaptivePortalProcessor.Process(ref AdaptivePortalTransform apTransform)
        {
            if (!isActiveAndEnabled) return;

            bool shouldRun = ShouldRun(), connectedShouldRun = _connected && _connected.ShouldRun();

            // Only one can run
            if (shouldRun == connectedShouldRun) return;

            // I should run
            if (shouldRun)
            {
                float gain = CalculateGain();
                float ratio = _interactable && _interactable.isSelected ? 1f : _ratio;

                apTransform.entryDepth = gain * (1f - ratio);
                apTransform.exitDepth = -gain * ratio;
            }
        }

        private bool ShouldRun() => isActiveAndEnabled && _portal && _gainCurve != null && _positionings.Count != 0;

        private float CalculateGain()
        {
            Plane plane = new Plane(transform.forward, transform.position);

            float gain = 0f;

            foreach (PortalRelativePosition positioning in _positionings)
            {
                if (!positioning || !positioning.origin || !positioning.source) continue;

                if (IsInteractor(positioning)) continue;

                if (TryGetPortalIndex(positioning, out int index))
                {
                    Vector3 startPos = positioning.origin.position, endPos = positioning.source.position;

                    // Get start and end positions in this space
                    for (int i = 0; i < index; i++)
                    {
                        positioning.GetPortalFromSource(i)?.ModifyPoint(ref startPos);
                        positioning.GetPortalFromSource(i)?.ModifyPoint(ref endPos);
                    }

                    Ray ray = new Ray(startPos, endPos - startPos);

                    float distance = -plane.GetDistanceToPoint(endPos);
                    distance = _gainCurve.Evaluate(Mathf.Abs(distance)) * Mathf.Sign(distance);

                    gain = Mathf.Max(gain, distance);
                }
            }

            return gain;
        }

        private bool IsInteractor(PortalRelativePosition positioning)
        {
            if (_interactable && _interactable.isSelected)
            {
                foreach (var interactor in _interactable.interactorsSelecting)
                {
                    if (interactor.transform.IsChildOf(positioning.transform))
                        return true;
                }
            }

            return false;
        }

        private bool TryGetPortalIndex(PortalRelativePosition positioning, out int index)
        {
            for (int i = 0; i < positioning.portalCount; i++)
            {
                Portal portal = positioning.GetPortalFromSource(i);
                if (portal == _portal)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }
    }
}
