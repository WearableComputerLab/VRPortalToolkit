using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Data;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit
{
    public class PortalRelativePosition : MonoBehaviour
    {
        [Tooltip("The transform to raycast from.")]

        [SerializeField] private Transform _origin;
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
        [SerializeField] private Transform _target;
        public virtual Transform target
        {
            get => _target;
            set => _target = value;
        }

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

        [SerializeField] private int _maxPortals = 16;
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

        public void Update()
        {
            UpdatePose();
        }

        public void LateUpdate()
        {

            UpdatePose();
        }

        public void FixedUpdate()
        {
            UpdatePose();
        }

        protected virtual void UpdatePose()
        {
            Transform origin = _origin ? _origin : _target,
                target = _target ? _target : _origin;

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

        private void OnPostTeleport(Teleportation teleportation)
        {
            _portalTrace.AddEndTeleport(teleportation.fromPortal);
        }

        private void OnOriginPostTeleport(Teleportation teleportation)
        {
            if (teleportation.fromPortal != null)
            {
                _portalTrace.AddStartTeleport(teleportation.fromPortal);

                // Swap to connected portal if we went through a portal we were holding.
                /*if (IsSelectedPortal(teleportation.fromPortal) && teleportation.fromPortal.connected)
                {
                    BaseInteractable interactable = teleportation.fromPortal.connected.GetComponentInParent<BaseInteractable>();
                    UpdatePose();

                    Select(interactable);
                }*/
            }
            else
                PortalPhysics.ForceTeleport(transform, UpdatePose, teleportation.source);
        }

        public int portalCount => _portalTrace.Count;

        public Portal GetPortalFromOrigin(int index) => _portalTrace.GetPortal(index);

        public IEnumerable<Portal> GetPortalsFromOrigin() => _portalTrace.GetPortals();

        public Portal GetPortalToOrigin(int index) => _portalTrace.GetUndoPortal(index);

        public IEnumerable<Portal> GetPortalsToOrigin() => _portalTrace.GetUndoPortals();
    }
}
