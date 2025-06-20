using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace VRPortalToolkit.XRI
{
    /// <summary>
    /// Expands the bounds bounds of the adaptive portals to maintain visibility of a target transform.
    /// </summary>
    [RequireComponent(typeof(XRPortalInteractable))]
    public class XRAdapativePortalVisualBounds : MonoBehaviour, IAdaptivePortalProcessor
    {
        [Tooltip("The target transform for bounds calculation.")]
        [SerializeField] private Transform _target;
        /// <summary>
        /// The target transform to keep visible.
        /// </summary>
        public Transform target
        {
            get => _target;
            set => _target = value;
        }

        [Tooltip("The local bounds to use for the visual bounds.")]
        [SerializeField] private Bounds _bounds;
        /// <summary>
        /// The local bounds to use for the visual bounds.
        /// </summary>
        public Bounds bounds
        {
            get => _bounds;
            set => _bounds = value;
        }

        [Tooltip("Padding to apply around the calculated bounds.")]
        [SerializeField] private Vector2 _padding = new Vector2(0.05f, 0.05f);
        /// <summary>
        /// Padding to apply around the calculated bounds.
        /// </summary>
        public Vector2 padding
        {
            get => _padding;
            set => _padding = value;
        }

        [Tooltip("Whether the target must be active for bounds to be considered.")]
        [SerializeField] private bool _requiresActive;
        /// <summary>
        /// Whether the target must be active for bounds to be considered.
        /// </summary>
        public bool requiresActive
        {
            get => _requiresActive;
            set => _requiresActive = value;
        }

        /// <inheritdoc/>
        int IAdaptivePortalProcessor.Order => 0;

        private XRPortalInteractable _interactable;
        private IXRSelectInteractor _interactor;
        private PortalRelativePosition _interactorPositioning;

        protected void OnDrawGizmosSelected()
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

        /// <inheritdoc/>
        void IAdaptivePortalProcessor.Process(ref AdaptivePortalTransform apTransform)
        {
            if (!isActiveAndEnabled || !_interactable || !_interactable.connected || _interactor == null || !_interactorPositioning || !_target || (_requiresActive && !_target.gameObject.activeInHierarchy))
                return;

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

            if (min.x <= max.x && min.y <= max.y)
                apTransform.AddMinMax(min, max);
        }
    }
}
