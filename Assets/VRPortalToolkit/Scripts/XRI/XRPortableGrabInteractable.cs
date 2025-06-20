using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Data;
using VRPortalToolkit.Physics;
using static UnityEngine.XR.Interaction.Toolkit.XRInteractionUpdateOrder;

// TODO: Should make a disposable for quickly applying a portal or portals to a transform and then undoing it

namespace VRPortalToolkit.XRI
{
    /// <summary>
    /// XR grab interactable that supports portal-aware interactions and teleportation.
    /// </summary>
    public class XRPortableGrabInteractable : XRGrabInteractable
    {
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

        private Pose _preTeleportPose;

        private Rigidbody _rigidbody;

        protected override void Awake()
        {
            base.Awake();

            _rigidbody = GetComponent<Rigidbody>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            PortalPhysics.RegisterPortable(transform);
            PortalPhysics.AddPreTeleportListener(transform, OnInteractablePreTeleport);
            PortalPhysics.AddPostTeleportListener(transform, OnInteractablePostTeleport);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            PortalPhysics.UnregisterPortable(transform);
            PortalPhysics.RemovePreTeleportListener(transform, OnInteractablePreTeleport);
            PortalPhysics.RemovePostTeleportListener(transform, OnInteractablePostTeleport);
        }

        /// <inheritdoc/>
        protected override void OnSelectEntering(SelectEnterEventArgs args)
        {
            PortalPhysics.UnregisterPortable(transform);
            PortalPhysics.AddPreTeleportListener(args.interactorObject.transform, OnInteractorPreTeleport);
            PortalPhysics.AddPostTeleportListener(args.interactorObject.transform, OnInteractorPostTeleport);

            if (args.interactorObject is IXRPortableInteractor portableInteractor)
                _portalTrace.AddEndTeleports(portableInteractor.GetPortalsToInteractable(this));

            Transform interactor = args.interactorObject.transform;

            // Do teleportations
            Pose interactorPose = new Pose(interactor.position, interactor.rotation);
            _portalTrace.GetPortals().ModifyTransform(interactor);

            // Do the actual hard work
            base.OnSelectEntering(args);

            // Restore
            interactor.transform.SetPositionAndRotation(interactorPose.position, interactorPose.rotation);
        }

        /// <inheritdoc/>
        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
        }

        /// <inheritdoc/>
        protected override void OnSelectExiting(SelectExitEventArgs args)
        {
            PortalPhysics.RegisterPortable(transform);
            PortalPhysics.RemovePreTeleportListener(args.interactorObject.transform, OnInteractorPreTeleport);
            PortalPhysics.RemovePostTeleportListener(args.interactorObject.transform, OnInteractorPostTeleport);

            Transform interactor = args.interactorObject.transform;

            // Do teleportations
            Pose interactorPose = new Pose(interactor.position, interactor.rotation);
            _portalTrace.GetPortals().ModifyTransform(interactor);

            // Do the actual hard work
            base.OnSelectExiting(args);

            // Restore
            interactor.transform.SetPositionAndRotation(interactorPose.position, interactorPose.rotation);

            _portalTrace.Clear();
        }        

        /// <inheritdoc/>
        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
        }

        /// <inheritdoc/>
        public override void ProcessInteractable(UpdatePhase updatePhase)
        {
            if (isSelected)
            {
                Transform interactor = interactorsSelecting[0].transform;

                // Do teleportations
                Pose interactorPose = new Pose(interactor.position, interactor.rotation);
                _portalTrace.GetPortals().ModifyTransform(interactor);

                // Do the actual hard work
                base.ProcessInteractable(updatePhase);

                // Restore
                interactor.transform.SetPositionAndRotation(interactorPose.position, interactorPose.rotation);

                // Redo Teleportations
                int portalRayCount;

                Vector3 interactorPos = interactorPose.position, interactablePos = _portalTrace.GetUndoPortals().ModifyPoint(transform.position);

                Ray ray = new Ray(interactorPos, interactablePos - interactorPos);
                float distance = Vector3.Distance(interactorPos, interactablePos);

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
                _portalTrace.TeleportDifference(transform, _portalRays, portalRayCount);
            }
            else
                base.ProcessInteractable(updatePhase);
        }

        /// <summary>
        /// Called before the interactable is teleported.
        /// </summary>
        /// <param name="teleportation">The teleportation data.</param>
        protected virtual void OnInteractablePreTeleport(Teleportation teleportation)
        {
            if (isSelected)
                _portalTrace.AddEndTeleport(teleportation.fromPortal);

            _preTeleportPose = new Pose(transform.position, transform.rotation);
        }

        /// <summary>
        /// Called after the interactable is teleported.
        /// </summary>
        /// <param name="teleportation">The teleportation data.</param>
        protected virtual void OnInteractablePostTeleport(Teleportation teleportation)
        {
            // Fix the pose to be in interactor space
            if (teleportation.fromPortal)
            {
                Pose pose = XRUtils.GetTargetPose(this);
                teleportation.fromPortal.ModifyPose(ref pose);
                XRUtils.SetTargetPose(this, pose);
            }

            // Tell the interactable about the teleportation
            XRUtils.OnTeleported(this, new Pose(transform.position - _preTeleportPose.position,
                transform.rotation * Quaternion.Inverse(_preTeleportPose.rotation)));
        }

        /// <summary>
        /// Called before the interactor is teleported.
        /// </summary>
        /// <param name="teleportation">The teleportation data.</param>
        protected virtual void OnInteractorPreTeleport(Teleportation teleportation)
        {

        }

        /// <summary>
        /// Called after the interactor is teleported.
        /// </summary>
        /// <param name="teleportation">The teleportation data.</param>
        protected virtual void OnInteractorPostTeleport(Teleportation teleportation)
        {
            _portalTrace.AddStartTeleport(teleportation.fromPortal);
        }
    }
}
