using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Data;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit
{
    /// <summary>
    /// Maintains a transform's position relative to a portal.
    /// </summary>
    public class PortalRelativePosition : MonoBehaviour
    {
        [Tooltip("The transform to raycast from.")]
        [SerializeField] private Transform _origin;
        /// <summary>
        /// The transform to raycast from.
        /// </summary>
        public Transform origin
        {
            get => _origin;
            set
            {
                if (_origin != value)
                {
                    if (isActiveAndEnabled && Application.isPlaying)
                    {
                        PortalPhysics.RemovePostTeleportListener(_origin, OnOriginPostTeleport);
                        Validate.UpdateField(this, nameof(_origin), _origin = value);
                        PortalPhysics.AddPostTeleportListener(_origin, OnOriginPostTeleport);
                    }
                    else
                        Validate.UpdateField(this, nameof(_origin), _origin = value);
                }
            }
        }

        [Tooltip("The transform to replicate.")]
        [SerializeField] private Transform _source;
        /// <summary>
        /// The transform to replicate.
        /// </summary>
        public virtual Transform source
        {
            get => _source;
            set => _source = value;
        }

        [Tooltip("The layer mask used for portal raycasting.")]
        [SerializeField] private LayerMask _portalMask = 1 << 3;
        /// <summary>
        /// The layer mask used for portal raycasting.
        /// </summary>
        public virtual LayerMask portalMask
        {
            get => _portalMask;
            set => _portalMask = value;
        }

        [Tooltip("The trigger interaction mode for portal raycasting.")]
        [SerializeField] private QueryTriggerInteraction _portalTriggerInteraction;
        /// <summary>
        /// The trigger interaction mode for portal raycasting.
        /// </summary>
        public virtual QueryTriggerInteraction portalTriggerInteraction
        {
            get => _portalTriggerInteraction;
            set => _portalTriggerInteraction = value;
        }

        [Tooltip("The maximum number of portals to trace through.")]
        [SerializeField] private int _maxPortals = 16;
        /// <summary>
        /// The maximum number of portals to trace through.
        /// </summary>
        public int maxPortals { get => _maxPortals; set => _maxPortals = value; }

        private PortalRay[] _portalRays;

        private readonly PortalTrace _portalTrace = new PortalTrace();

        protected virtual void OnEnable()
        {
            PortalPhysics.AddPostTeleportListener(transform, OnPostTeleport);
            PortalPhysics.AddPostTeleportListener(_origin, OnOriginPostTeleport);
        }

        protected virtual void OnDisable()
        {
            PortalPhysics.RemovePostTeleportListener(transform, OnPostTeleport);
            PortalPhysics.RemovePostTeleportListener(_origin, OnOriginPostTeleport);
        }

        protected void Update()
        {
            UpdatePose();
        }

        protected void LateUpdate()
        {
            UpdatePose();
        }

        protected void FixedUpdate()
        {
            UpdatePose();
        }

        /// <summary>
        /// Updates the pose of the transform relative to the portal.
        /// </summary>
        protected virtual void UpdatePose()
        {
            Transform origin = _origin ? _origin : _source,
                target = _source ? _source : _origin;

            if (origin)
            {
                // Apply the pose in the local space
                _portalTrace.GetUndoPortals().ModifyTransform(transform);
                transform.SetPositionAndRotation(target.position, target.rotation);
                transform.localScale = target.localScale;

                int portalRayCount;

                Ray ray = new Ray(origin.position, target.position - origin.position);
                float distance = Vector3.Distance(origin.position, target.position);

                // Get the portals from the interactor to the interactable
                if (_maxPortals >= 0)
                {
                    if (_portalRays == null || _portalRays.Length != _maxPortals)
                        _portalRays = new PortalRay[_maxPortals];

                    portalRayCount = PortalPhysics.GetRays(ray, _portalRays, distance, portalMask, portalTriggerInteraction);
                }
                else
                {
                    _portalRays = PortalPhysics.GetRays(ray, distance, portalMask, portalTriggerInteraction);
                    portalRayCount = _portalRays.Length;
                }

                // Revert back and apply the difference
                _portalTrace.GetPortals().ModifyTransform(transform);
                _portalTrace.TeleportDifference(transform, _portalRays, portalRayCount);
            }
        }

        /// <summary>
        /// Called after the transform is teleported.
        /// </summary>
        /// <param name="teleportation">The teleportation data.</param>
        private void OnPostTeleport(Teleportation teleportation)
        {
            _portalTrace.AddEndTeleport(teleportation.fromPortal);
        }

        /// <summary>
        /// Called after the origin is teleported.
        /// </summary>
        /// <param name="teleportation">The teleportation data.</param>
        private void OnOriginPostTeleport(Teleportation teleportation)
        {
            if (teleportation.fromPortal != null)
                _portalTrace.AddStartTeleport(teleportation.fromPortal);
            else
                PortalPhysics.ForceTeleport(transform, UpdatePose, teleportation.source);
        }

        /// <summary>
        /// Gets the number of portals in the current trace.
        /// </summary>
        public int portalCount => _portalTrace.Count;

        /// <summary>
        /// Gets the portal at the specified index from the source.
        /// </summary>
        /// <param name="index">The index of the portal.</param>
        /// <returns>The portal at the specified index.</returns>
        public Portal GetPortalFromSource(int index) => _portalTrace.GetPortal(index);

        /// <summary>
        /// Gets all portals from the origin.
        /// </summary>
        /// <returns>An enumerable of portals from the source.</returns>
        public IEnumerable<Portal> GetPortalsFromSource() => _portalTrace.GetPortals();

        /// <summary>
        /// Gets the portal at the specified index to the source.
        /// </summary>
        /// <param name="index">The index of the portal.</param>
        /// <returns>The portal at the specified index.</returns>
        public Portal GetPortalToSource(int index) => _portalTrace.GetUndoPortal(index);

        /// <summary>
        /// Gets all portals to the origin.
        /// </summary>
        /// <returns>An enumerable of portals to the source.</returns>
        public IEnumerable<Portal> GetPortalsToSource() => _portalTrace.GetUndoPortals();
    }
}
