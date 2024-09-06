using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.XRI
{
    [DefaultExecutionOrder(10)]
    [RequireComponent(typeof(XRPortalInteractable))]
    public class XRPortalHandReach : MonoBehaviour
    {
        [SerializeField] private Transform _offset;
        public Transform offset
        {
            get => _offset;
            set => _offset = value;
        }

        [SerializeField] private AnimationCurve _gainCurve = AnimationCurve.Linear(0f, 0f, 1f, 0.5f);
        public AnimationCurve gainCurve
        {
            get => _gainCurve;
            set => _gainCurve = value;
        }

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

        protected virtual void Update()
        {
            UpdateInteractors();
        }

        protected virtual void LateUpdate()
        {
            _positionings.RemoveAll(IsInvalid);
            UpdateInteractors();
        }

        protected virtual void FixedUpdate()
        {
            UpdateInteractors();
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

        public void UpdateInteractors()
        {
            if (isActiveAndEnabled && _portal && _gainCurve != null)
            {
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue),
                    max = new Vector2(float.MinValue, float.MinValue);

                Plane plane = new Plane(transform.forward, transform.position);

                foreach (PortalRelativePosition positioning in _positionings)
                {
                    if (!positioning || !positioning.origin) continue;

                    if (IsInteractor(positioning)) continue;

                    if (TryGetPortalIndex(positioning, out int index))
                    {
                        Vector3 startPos = positioning.origin.position, endPos = positioning.transform.position;

                        // Get start and end positions in the same space
                        for (int i = 0; i < index; i++)
                            positioning.GetPortalFromOrigin(i)?.ModifyPoint(ref startPos);

                        for (int i = 0; i < positioning.portalCount - index; i++)
                            positioning.GetPortalToOrigin(i)?.ModifyPoint(ref endPos);

                        Ray ray = new Ray(startPos, endPos - startPos);

                        //Debug.DrawLine(startPos, endPos, Color.black);

                        float distance = plane.GetDistanceToPoint(endPos);
                        distance = _gainCurve.Evaluate(Mathf.Abs(distance)) * Mathf.Sign(distance);
                        endPos += plane.normal * distance;

                        // Return end position to original space
                        for (int i = index; i < positioning.portalCount; i++)
                            positioning.GetPortalFromOrigin(i)?.ModifyPoint(ref endPos);

                        positioning.transform.position = endPos;
                    }
                }
            }
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