using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRPortalToolkit.XRI
{

    [RequireComponent(typeof(XRPortalInteractable))]
    public class XRPortalVisibleExpand : MonoBehaviour, IPortalRectRequester
    {
        [SerializeField] private Transform _target;
        public Transform target
        {
            get => _target;
            set => _target = value;
        }

        [SerializeField] private Bounds _bounds;
        public Bounds bounds
        {
            get => _bounds;
            set => _bounds = value;
        }

        [SerializeField] private Vector2 _padding = new Vector2(0.05f, 0.05f);
        public Vector2 padding
        {
            get => _padding;
            set => _padding = value;
        }

        [SerializeField] private bool _requiresActive;
        public bool requiresActive
        {
            get => _requiresActive;
            set => _requiresActive = value;
        }

        private XRPortalInteractable _interactable;
        private IXRSelectInteractor _interactor;
        private PortalRelativePosition _interactorPositioning;

        protected void OnDrawGizmos()
        {
            if (_target)
            {
                Gizmos.color = !_requiresActive || _target.gameObject.activeInHierarchy ? Color.green : Color.red;
                Gizmos.matrix = _target.localToWorldMatrix;
                Gizmos.DrawWireCube(_bounds.center, _bounds.size);
            }
        }
        protected void Awake()
        {
            _interactable = GetComponent<XRPortalInteractable>();
        }

        protected void OnEnable()
        {
            _interactable.selectEntered.AddListener(OnSelectEntered);
            _interactable.selectExited.AddListener(OnSelectExited);
        }

        protected void OnDisable()
        {
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
            _interactable.selectExited.RemoveListener(OnSelectExited);
        }

        private void OnSelectEntered(SelectEnterEventArgs _) => UpdateRelativePositioning();

        private void OnSelectExited(SelectExitEventArgs _) => UpdateRelativePositioning();

        private void UpdateRelativePositioning()
        {
            if (_interactor != null)
            {
                if (!_interactable.interactorsSelecting.Contains(_interactor))
                {
                    _interactor = null;
                    _interactorPositioning = null;
                }
            }

            if (_interactor == null)
            {
                foreach (var interactor in _interactable.interactorsSelecting)
                {
                    _interactorPositioning = interactor.transform.GetComponentInParent<PortalRelativePosition>();

                    if (_interactorPositioning)
                    {
                        _interactor = interactor;
                        break;
                    }
                }
            }
        }

        private static readonly Vector3[] BoundsCorner = {
            new Vector3 (1, 1, 1), new Vector3 (-1, 1, 1), new Vector3 (-1, -1, 1), new Vector3 (-1, -1, -1),
            new Vector3 (-1, 1, -1), new Vector3 (1, -1, -1), new Vector3 (1, 1, -1), new Vector3 (1, -1, 1),
        };

        public bool TryGetRect(out Rect rect)
        {
            if (!isActiveAndEnabled || !_interactable || !_interactable.connected || _interactor == null || !_interactorPositioning || !_target || (_requiresActive && !_target.gameObject.activeInHierarchy))
            {
                rect = default;
                return false;
            }

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = new Vector2(float.MinValue, float.MinValue);
            Plane plane = new Plane(_interactable.connected.transform.forward, _interactable.connected.transform.position);

            Vector3 originPosition = _interactorPositioning.origin.position, corner;

            // Get head in connected space
            if (_interactable.portal)
                _interactable.portal.ModifyPoint(ref originPosition);

            for (int i = 0; i < 8; i++)
            {
                // Local space
                corner = _bounds.center + Vector3.Scale(_bounds.extents, BoundsCorner[i]);

                // World space
                corner = _target.TransformPoint(corner);

                Ray ray = new Ray(originPosition, corner - originPosition);

                if (plane.Raycast(ray, out float enter))
                {
                    Vector2 pos = _interactable.connected.transform.InverseTransformPoint(ray.GetPoint(enter));
                    
                    min = Vector2.Min(min, pos - padding);
                    max = Vector2.Max(max, pos + padding);
                }
            }

            rect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            rect.x = -rect.xMax; // so its not on the connected side
            return min.x <= max.x && min.y <= max.y;
        }
    }
}
