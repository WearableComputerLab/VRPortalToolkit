using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VRPortalToolkit.Cloning;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.XRI
{
    /// <summary>
    /// Portal-aware ray interactor that supports raycast interactions through portals.
    /// </summary>
    public class XRPortableRayInteractor : XRRayInteractor, IXRPortableInteractor, IPortalLineRenderable, IPortalCursorRenderable
    {
        private readonly static int MaxPortals = 10;
        private readonly static List<IXRInteractable> _results = new List<IXRInteractable>(1);
        private readonly static PortalRay[] castPortalRays = new PortalRay[MaxPortals];

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

        /// <summary>
        /// Gets the number of portal rays in the current raycast.
        /// </summary>
        public int portalRayCount => _portalRaysCount;

        private IXRInteractable _actualValidTarget;
        private Vector3[] linePoints;
        private PortalRay[] _portalRays;
        private int _portalRaysCount;
        private int _portalHitIndex = -1;
        private RaycastHit _hitInfo;
        private int _portalUIHitIndex = -1;
        private RaycastResult _uiRaycastResult;

        private static TrackedDeviceEventData _trackedDeviceEvent;
        private static List<RaycastResult> _raycastResults;

        /// <summary>
        /// Gets the portals needed to travel to the specified interactable.
        /// </summary>
        /// <param name="interactable">The XR interactable.</param>
        /// <returns>An enumerable of portals.</returns>
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
            for (int i = 1; i <= _portalHitIndex; i++)
                yield return _portalRays[i].fromPortal;

        }

        /// <inheritdoc/>
        protected override void OnSelectEntering(SelectEnterEventArgs args)
        {
            base.OnSelectEntering(args);

            if (!useForceGrab && interactablesSelected.Count == 1 && _portalHitIndex != -1)
            {
                Vector3 point = _hitInfo.point;

                for (int i = _portalHitIndex; i > 0; i--)
                    _portalRays[i].fromPortal?.connected.ModifyPoint(ref point);

                attachTransform.position = point;
            }
        }

        /// <inheritdoc/>
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

            if (_portalRays == null || _portalRays.Length - MaxPortals < numPoints)
                _portalRays = new PortalRay[numPoints + MaxPortals];

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

                if (PortalPhysics.Cast(caster, _portalRays, _portalRaysCount, out _hitInfo, out _portalHitIndex, raycastMask, raycastTriggerInteraction))
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
            else
                _portalHitIndex = -1;

            if (_portalHitIndex == -1)
            {
                // Inform the interactor
                XRUtils.SetRaycastHitsCount(this, -1);
            }
            else
            {
                // Inform the interactor
                XRUtils.SetRaycastHitsCount(this, 1);
                XRUtils.GetRaycastHits(this)[0] = _hitInfo;
            }
        }

        /// <inheritdoc/>
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
        }

        /// <summary>
        /// Tries to get hit information for the current raycast.
        /// </summary>
        /// <param name="position">The hit position.</param>
        /// <param name="normal">The hit normal.</param>
        /// <param name="portalRayIndex">The index of the portal ray that produced the hit.</param>
        /// <param name="isValidTarget">Whether the hit target is valid for interaction.</param>
        /// <returns>True if hit information is available.</returns>
        public new bool TryGetHitInfo(out Vector3 position, out Vector3 normal, out int portalRayIndex, out bool isValidTarget)
        {
            if (_portalUIHitIndex >= 0)
            {
                position = _uiRaycastResult.worldPosition;
                normal = _uiRaycastResult.worldNormal;
                portalRayIndex = _portalUIHitIndex;
                isValidTarget = _uiRaycastResult.isValid; // TODO: Check that
                return true;
            }

            position = _hitInfo.point;
            normal = _hitInfo.normal;
            portalRayIndex = _portalHitIndex;
            isValidTarget = hasSelection && _actualValidTarget != null;

            return _portalHitIndex >= 0;
        }

        /// <summary>
        /// Gets the portal ray at the specified index.
        /// </summary>
        /// <param name="portalRayIndex">The index of the portal ray to retrieve.</param>
        /// <returns>The portal ray at the specified index.</returns>
        public PortalRay GetPortalRay(int portalRayIndex) => _portalRays[portalRayIndex];

        /// <summary>
        /// Tries to get the cursor pose for reticle rendering.
        /// </summary>
        /// <param name="cursorPose">The cursor pose.</param>
        /// <param name="isValidTarget">Whether the cursor is over a valid target.</param>
        /// <returns>True if cursor information is available.</returns>
        public bool TryGetCursor(out Pose cursorPose, out bool isValidTarget)
        {
            if (TryGetHitInfo(out Vector3 position, out Vector3 normal, out _, out isValidTarget))
            {
                cursorPose.position = position;
                Vector3 up = transform.up;
                for (int i = 1; i < _portalHitIndex; i++)
                    _portalRays[i].fromPortal?.ModifyDirection(up);

                cursorPose.rotation = attachTransform.rotation;


                if (this.GetOldestInteractableHovered() is IXRReticleDirectionProvider reticleDirectionProvider)
                {
                    reticleDirectionProvider.GetReticleDirection(this, normal, out var reticleUp, out var reticleForward);

                    if (reticleForward.HasValue)
                        cursorPose.rotation = Quaternion.LookRotation(reticleForward.Value, reticleUp);
                    else
                        cursorPose.rotation = Quaternion.LookRotation(Vector3.Slerp(reticleUp, -reticleUp, 0.5f), reticleUp);
                }
                else
                    cursorPose.rotation = Quaternion.LookRotation(Vector3.Slerp(normal, -normal, 0.5f), normal);

                return true;
            }

            cursorPose = default;
            return false;
        }

        public override void UpdateUIModel(ref TrackedDeviceModel model)
        {
            _portalUIHitIndex = -1;

            if (!isActiveAndEnabled || portalRayCount == 0 || this.IsBlockedByInteractionWithinGroup() || !EventSystem.current)
            {
                model.Reset(false);
                return;
            }

            base.UpdateUIModel(ref model);

            var raycastPoints = model.raycastPoints;
            raycastPoints.Clear();

            _raycastResults ??= new List<RaycastResult>();
            _trackedDeviceEvent ??= new TrackedDeviceEventData(EventSystem.current);
            model.CopyTo(_trackedDeviceEvent);

            int maxRayCount = _portalHitIndex == -1 ? portalRayCount : _portalHitIndex + 1;
            for (int i = 0; i < maxRayCount; i++)
            {
                PortalRay portalRay = _portalRays[i];

                Vector3 origin = portalRay.origin, dirVec = portalRay.direction;

                raycastPoints.Clear();
                raycastPoints.Add(origin);

                if (i == _portalHitIndex)
                    raycastPoints.Add(origin + dirVec.normalized * _hitInfo.distance);
                else
                    raycastPoints.Add(origin + dirVec);

                _trackedDeviceEvent.rayHitIndex = 0;
                EventSystem.current.RaycastAll(_trackedDeviceEvent, _raycastResults);

                // This will be the ui that hits something
                if (_raycastResults.Count > 0)
                {
                    _portalUIHitIndex = i;
                    _uiRaycastResult = _raycastResults[0];
                    break;
                }

                // Something else blocked the UI
                //if (_trackedDeviceEvent.rayHitIndex != 0)
                //    break;
            }
        }
    }
}
