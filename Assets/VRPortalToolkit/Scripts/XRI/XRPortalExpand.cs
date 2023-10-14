using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.XRI
{
    [RequireComponent(typeof(XRPortalInteractable))]
    public class XRPortalExpand : MonoBehaviour, IPortalRectRequester
    {
        [SerializeField] private Vector2 _padding = new Vector2(0.05f, 0.05f);
        public Vector2 padding
        {
            get => _padding;
            set => _padding = value;
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

        public bool TryGetRect(out Rect rect)
        {
            if (isActiveAndEnabled && _portal)
            {
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue),
                    max = new Vector2(float.MinValue, float.MinValue);

                foreach (PortalRelativePosition positioning in _positionings)
                {
                    if (!positioning || !positioning.origin) continue;

                    if (IsInteractor(positioning)) continue;

                    if (TryGetPortalIndex(positioning, out int index))
                    {
                        Vector3 startPos = positioning.origin.position, endPos = positioning.transform.position;

                        for (int i = 0; i < index; i++)
                            positioning.GetPortalFromOrigin(i)?.ModifyPoint(ref startPos);

                        for (int i = 0; i < positioning.portalCount - index; i++)
                            positioning.GetPortalToOrigin(i)?.ModifyPoint(ref endPos);

                        Plane plane = new Plane(transform.forward, transform.position);

                        Ray ray = new Ray(startPos, endPos - startPos);

                        Debug.DrawLine(startPos, endPos, Color.black);

                        if (plane.Raycast(ray, out float enter) && enter < Vector3.Distance(startPos, endPos))
                        {
                            Vector2 pos = transform.InverseTransformPoint(ray.GetPoint(enter));

                            min = Vector2.Min(min, pos - _padding);
                            max = Vector2.Max(max, pos + _padding);
                        }
                    }
                }

                if (min.x <= max.x && min.y <= max.y)
                {
                    rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                    return true;
                }
            }

            rect = default;
            return false;
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
