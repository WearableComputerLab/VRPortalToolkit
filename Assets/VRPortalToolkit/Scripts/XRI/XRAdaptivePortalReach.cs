using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRPortalToolkit.Physics;
using VRPortalToolkit.XRI;

namespace VRPortalToolkit
{
    [DefaultExecutionOrder(1)]
    [RequireComponent(typeof(XRPortalInteractable))]
    public class XRAdaptivePortalReach : MonoBehaviour, IAdaptivePortalProcessor
    {
        [SerializeField] private XRAdaptivePortalReach _connected;
        public XRAdaptivePortalReach connected
        {
            get => _connected;
            set => _connected = value;
        }

        [SerializeField] private AnimationCurve _gainCurve = AnimationCurve.Linear(0f, 0f, 1f, 0.5f);
        public AnimationCurve gainCurve
        {
            get => _gainCurve;
            set => _gainCurve = value;
        }

        [Range(0f, 1f)]
        [SerializeField] private float _ratio = 1f;
        public float ratio
        {
            get => _ratio;
            set => _ratio = Mathf.Clamp01(value);
        }

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
                if (!positioning.GetPortalsFromOrigin().Contains(_portal))
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
                if (!positioning || !positioning.origin || !positioning.target) continue;

                if (IsInteractor(positioning)) continue;

                if (TryGetPortalIndex(positioning, out int index))
                {
                    Vector3 startPos = positioning.origin.position, endPos = positioning.target.position;

                    // Get start and end positions in this space
                    for (int i = 0; i < index; i++)
                    {
                        positioning.GetPortalFromOrigin(i)?.ModifyPoint(ref startPos);
                        positioning.GetPortalFromOrigin(i)?.ModifyPoint(ref endPos);
                    }

                    Ray ray = new Ray(startPos, endPos - startPos);

                    float distance = -plane.GetDistanceToPoint(endPos);
                    distance = _gainCurve.Evaluate(Mathf.Abs(distance)) * Mathf.Sign(distance);

                    gain = Mathf.Max(gain, distance);
                }
            }

            return gain;
        }

        /*private void SetGain(float gain)
        {
            if (_offset)
            {
                float z = transform.InverseTransformPoint(transform.position + transform.forward * gain).z;

                Vector3 localPos = _offset.localPosition;
                _offset.localPosition = new Vector3(localPos.x, localPos.y, z);
            }
        }*/

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
                Portal portal = positioning.GetPortalFromOrigin(i);
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
