using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Rendering;

namespace VRPortalToolkit
{
    /// <summary>
    /// Disables portal renderers and colliders when the portal is close to its connected portal to prevent visual glitches and physics issues.
    /// </summary>
    [DefaultExecutionOrder(300)]
    public class PortalProximityDisabler : MonoBehaviour
    {
        [Tooltip("The portal to monitor for proximity to its connected portal.")]
        [SerializeField] private Portal _portal;
        /// <summary>
        /// The portal to monitor for proximity to its connected portal.
        /// </summary>
        public Portal portal
        {
            get => _portal;
            set => _portal = value;
        }

        [Tooltip("The portal renderer that will be disabled when portals are too close.")]
        [SerializeField] private PortalRendererBase _portalRenderer;
        /// <summary>
        /// The portal renderer that will be disabled when portals are too close.
        /// </summary>
        public PortalRendererBase portalRenderer
        {
            get => _portalRenderer;
            set => _portalRenderer = value;
        }

        [Tooltip("List of colliders that will be disabled when portals are too close.")]
        [SerializeField] private List<Collider> _colliders;
        /// <summary>
        /// List of colliders that will be disabled when portals are too close.
        /// </summary>
        public List<Collider> colliders => _colliders;

        [Tooltip("Whether to use distance threshold for determining portal proximity.")]
        [SerializeField] private bool _useDistanceThreshold = true;
        /// <summary>
        /// Whether to use distance threshold for determining portal proximity.
        /// </summary>
        public bool useDistanceThreshold { get => _useDistanceThreshold; set => _useDistanceThreshold = value; }

        [Tooltip("The minimum distance between a portal and its transformed position (in meters) before disabling it.")]
        [ShowIf(nameof(_useDistanceThreshold))]
        [SerializeField] private float _distanceThreshold = 0.01f;
        /// <summary>
        /// The minimum distance between a portal and its transformed position (in meters) before disabling it.
        /// </summary>
        public float distanceThreshold { get => _distanceThreshold; set => _distanceThreshold = value; }

        [Tooltip("Whether to use angle threshold for determining portal proximity.")]
        [SerializeField] private bool _useAngleThreshold = true;
        /// <summary>
        /// Whether to use angle threshold for determining portal proximity.
        /// </summary>
        public bool useAngleThreshold { get => _useAngleThreshold; set => _useAngleThreshold = value; }

        [Tooltip("The minimum angle between a portal and its transformed rotation (in degrees) before disabling it.")]
        [ShowIf(nameof(_useAngleThreshold))]
        [SerializeField] private float _angleThreshold = 1f;
        /// <summary>
        /// The minimum angle between a portal and its transformed rotation (in degrees) before disabling it.
        /// </summary>
        public float angleThreshold { get => _angleThreshold; set => _angleThreshold = value; }

        /// <summary>
        /// Initializes the component by finding necessary references in the hierarchy.
        /// </summary>
        public void Reset()
        {
            _portal = GetComponentInChildren<Portal>();
            _portalRenderer = GetComponentInChildren<PortalRendererBase>();
            GetComponentsInChildren(_colliders);
        }

        /// <summary>
        /// Updates the enabled state of the portal renderer and colliders based on proximity checks.
        /// </summary>
        public void LateUpdate()
        {
            bool state = GetState();

            if (_portalRenderer && _portalRenderer.enabled != state)
                _portalRenderer.enabled = state;

            foreach (Collider collider in _colliders)
                if (collider && collider.enabled != state)
                    collider.enabled = state;
        }

        /// <summary>
        /// Determines whether the portal should be enabled based on distance and angle thresholds.
        /// </summary>
        /// <returns>True if the portal should be enabled, false if it should be disabled due to proximity issues.</returns>
        private bool GetState()
        {
            if (_portal && _useDistanceThreshold || _useAngleThreshold)
            {
                if (_useDistanceThreshold)
                {
                    if (Vector3.Distance(transform.position, _portal.ModifyPoint(transform.position)) > _distanceThreshold)
                        return true;
                }

                if (_useAngleThreshold)
                {
                    if (Quaternion.Angle(transform.rotation, _portal.ModifyRotation(transform.rotation)) > _angleThreshold)
                        return true;
                }

                return false;
            }

            return true;
        }
    }
}
