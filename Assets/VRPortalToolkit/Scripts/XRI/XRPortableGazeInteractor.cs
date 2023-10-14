using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Cloning;
using VRPortalToolkit.Physics;

// TODO: This has not been tested
namespace VRPortalToolkit.XRI
{
    public class XRPortableGazeInteractor : XRGazeInteractor, IXRPortableInteractor
    {
        private readonly static int MaxRaycasts = 10;
        private readonly static List<IXRInteractable> _results = new List<IXRInteractable>(1);
        private readonly PortalRay[] castPortalRays = new PortalRay[MaxRaycasts];

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

        private IXRInteractable _actualValidTarget;
        private Vector3[] linePoints;
        private PortalRay[] _portalRays;
        private int _portalRaysCount;
        private int _portalIndex;
        private RaycastHit _hitInfo;

        public IEnumerable<Portal> GetPortalsToInteractable(IXRInteractable interactable)
        {
            IEnumerable<Portal> from = GetPortalsToRaycastHit(), to = null;

            if (hasSelection && interactablesSelected[0] == interactable && _hitInfo.collider)
            {
                if (PortalCloning.TryGetCloneInfo(_hitInfo.collider.transform, out var info))
                    to = info.GetCloneToOriginalPortals();
            }

            return from.Difference(to);
        }

        private IEnumerable<Portal> GetPortalsToRaycastHit()
        {
            for (int i = 1; i <= _portalIndex; i++)
                yield return _portalRays[i].fromPortal;

        }
        /// <inheritdoc />
        protected override void OnSelectEntering(SelectEnterEventArgs args)
        {
            base.OnSelectEntering(args);

            if (!useForceGrab && interactablesSelected.Count == 1 && _portalIndex != -1)
            {
                Vector3 point = _hitInfo.point;

                for (int i = _portalIndex; i > 0; i--)
                    _portalRays[i].fromPortal?.connected.ModifyPoint(ref point);

                attachTransform.position = point;
            }
        }

        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            // Perform base without actually raycasting
            LayerMask temp = raycastMask;
            raycastMask = 0;
            base.PreprocessInteractor(updatePhase);
            raycastMask = temp;

            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic)
                return;

            // Get the line points for portal casting
            GetLinePoints(ref linePoints, out int numPoints);

            if (_portalRays == null || _portalRays.Length + MaxRaycasts < numPoints)
                _portalRays = new PortalRay[numPoints];

            _portalRaysCount = 0;

            if (!hasSelection) _actualValidTarget = null;

            if (linePoints.Length > 1)
            {
                Matrix4x4 teleportMatrix = Matrix4x4.identity;

                // Now we portal cast down all those lines, tracking each portal we hit
                Vector3 from = linePoints[0], to;
                for (int i = 1; i < numPoints && _portalRaysCount < _portalRays.Length; i++)
                {
                    to = teleportMatrix.MultiplyPoint3x4(linePoints[i]);
                    Vector3 direction = (to - from).normalized;
                    float maxDistance = Vector3.Distance(to, from);

                    int castRayCount = PortalPhysics.GetRays(from, direction, castPortalRays, maxDistance, portalMask, portalTriggerInteraction);


                    for (int j = 0; j < castRayCount && _portalRaysCount < _portalRays.Length; j++)
                    {
                        _portalRays[_portalRaysCount++] = castPortalRays[j];

                        Portal portal = castPortalRays[j].fromPortal;

                        if (portal)
                        {
                            teleportMatrix = castPortalRays[j].fromPortal.teleportMatrix * teleportMatrix;
                            castPortalRays[j].fromPortal.ModifyPoint(ref to);
                        }
                    }

                    from = to;
                }

                // Now actually raycast
                IPhysicsCaster caster;

                if (hitDetectionType == HitDetectionType.SphereCast && sphereCastRadius > 0f)
                    caster = new SphereCaster(sphereCastRadius);
                else
                    caster = new Raycaster();

                if (PortalPhysics.Cast(caster, _portalRays, _portalRaysCount, out _hitInfo, out _portalIndex, raycastMask, raycastTriggerInteraction))
                {
                    Collider collider = PortalCloning.GetOriginal(_hitInfo.collider);

                    if (interactionManager.TryGetInteractableForCollider(collider, out _actualValidTarget, out XRInteractableSnapVolume snapVolume))
                    {
                        bool baseQueryHitsTriggers = raycastTriggerInteraction == QueryTriggerInteraction.Collide ||
                            (raycastTriggerInteraction == QueryTriggerInteraction.UseGlobal && UnityEngine.Physics.queriesHitTriggers);

                        if (raycastSnapVolumeInteraction == QuerySnapVolumeInteraction.Ignore && baseQueryHitsTriggers)
                        {
                            if (snapVolume == null) _actualValidTarget = null;
                        }
                        else if (raycastSnapVolumeInteraction == QuerySnapVolumeInteraction.Collide && !baseQueryHitsTriggers)
                        {
                            if (snapVolume != null) _actualValidTarget = null;
                        }
                    }
                }
            }

            if (_actualValidTarget == null)
                _portalIndex = -1;
        }

        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            targets.Clear();

            if (_actualValidTarget != null)
                targets.Add(_actualValidTarget);

            var filter = targetFilter;
            if (filter != null && filter.canProcess)
            {
                filter.Process(this, targets, _results);
                targets.Clear();
                targets.AddRange(_results);
            }
            //base.GetValidTargets(targets);
        }
    }
}
