using Misc.EditorHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit;
using VRPortalToolkit.Physics;
using VRPortalToolkit.Portables;

namespace VRPortalToolkit.Pointers
{
    [DefaultExecutionOrder(101)] // Execute after pointer
    public class PortalPointerCursor : MonoBehaviour
    {
        [SerializeField] private PortalPointer _raycaster;
        public PortalPointer raycaster
        {
            get => _raycaster;
            set
            {
                if (_raycaster != value)
                {
                    if (isActiveAndEnabled && Application.isPlaying)
                    {
                        RemoveRaycasterListeners(_raycaster);
                        Validate.UpdateField(this, nameof(_raycaster), _raycaster = value);
                        AddRaycasterListeners(_raycaster);
                    }
                    else
                        Validate.UpdateField(this, nameof(_raycaster), _raycaster = value);
                }
            }
        }

        [SerializeField] private Transform _target;
        public virtual Transform target
        {
            get => _target;
            set => _target = value;
        }

        public enum DefaultMode
        {
            PositionOnly = 0,
            Forward = 1,
            Backward = 2
        }

        [SerializeField] public DefaultMode _defaultMode = DefaultMode.Forward;
        public DefaultMode defaultMode
        {
            get => _defaultMode;
            set => _defaultMode = value;
        }

        public enum HitMode
        {
            Ignore = 0,
            PositionOnly = 1,
            Forward = 2,
            Backward = 3,
            Normal = 4,
            Reversed = 5
        }

        [SerializeField] public HitMode _hitMode = HitMode.Normal;
        public virtual HitMode hitMode
        {
            get => _hitMode;
            set => _hitMode = value;
        }

        [Header("Optional"), SerializeField] private Transform _upright;
        public virtual Transform upright
        {
            get => _upright;
            set => _upright = value;
        }

        [SerializeField] private bool _usesDefaultScale = false;
        public virtual bool usesDefaultScale {
            get => _usesDefaultScale;
            set => _usesDefaultScale = value;
        }

        [ShowIf(nameof(usesDefaultScale))]
        [SerializeField] private Vector3 _defaultScale = Vector3.one;
        public virtual Vector3 defaultScale {
            get => _defaultScale;
            set => _defaultScale = value;
        }

        protected Matrix4x4 raycasterEnd;
        protected Matrix4x4 previousOrigin;
        protected List<Portal> portalTrace = new List<Portal>();
        protected bool teleportedLastUpdate = false;

        protected virtual void Reset()
        {
            target = transform;
        }

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_raycaster), nameof(raycaster));
        }

        protected virtual void Awake()
        {
            target = _target;
        }

        protected virtual void OnEnable()
        {
            AddRaycasterListeners(_raycaster);

            Apply();
        }

        protected virtual void OnDisable()
        {
            RemoveRaycasterListeners(_raycaster);

            PeformTeleports(0);
        }

        protected virtual void AddRaycasterListeners(PortalPointer raycaster)
        {
            if (raycaster) PortalPhysics.AddPostTeleportListener(raycaster.transform, RaycasterPostTeleport);
        }

        protected virtual void RemoveRaycasterListeners(PortalPointer raycaster)
        {
            if (raycaster) PortalPhysics.RemovePostTeleportListener(raycaster.transform, RaycasterPostTeleport);
        }

        public virtual void FixedUpdate()
        {
            Apply();
        }

        public virtual void Apply()
        {
            if (!raycaster || raycaster.portalRaysCount <= 0) return;

            if (_hitMode != HitMode.Ignore && raycaster.TryGetHitInfo(out RaycastHit hitInfo, out int hitIndex))
            {
                GetRaycastEnd(hitIndex, hitInfo.distance, out Vector3 origin, out Vector3 forward);
                target.transform.position = origin;

                switch (_hitMode)
                {
                    case HitMode.Forward:
                        _target.transform.rotation = GetRotation(forward, _raycaster.actualOrigin.forward);
                        break;

                    case HitMode.Backward:
                        _target.transform.rotation = GetRotation(-forward, -_raycaster.actualOrigin.forward);
                        break;

                    case HitMode.Normal:
                        _target.transform.rotation = GetRotation(GetOriginalDirection(hitIndex + 1, hitInfo.normal), -_raycaster.actualOrigin.forward);
                        break;

                    case HitMode.Reversed:
                        _target.transform.rotation = GetRotation(GetOriginalDirection(hitIndex + 1 , -hitInfo.normal), -_raycaster.actualOrigin.forward);
                        break;
                }

                PeformTeleports(hitIndex + 1);
            }
            else
            {
                GetRaycastEnd(raycaster.portalRaysCount - 1, float.MaxValue, out Vector3 origin, out Vector3 forward);
                target.transform.position = origin;

                switch (_defaultMode)
                {
                    case DefaultMode.Forward:
                        _target.transform.rotation = GetRotation(forward, _raycaster.actualOrigin.forward);
                        break;

                    case DefaultMode.Backward:
                        _target.transform.rotation = GetRotation(-forward, -_raycaster.actualOrigin.forward);
                        break;
                }
                
                PeformTeleports(raycaster.portalRaysCount);

            }
        }

        // altUp is used as a backup if forwad is parallel to up. Normal may also be parallel, so this isnt perfect
        protected virtual Quaternion GetRotation(Vector3 forward, Vector3 altUp)
        {
            Vector3 up = upright ? upright.up : Vector3.up;

            float dot = Vector3.Dot(up, forward);

            if (dot == -1f || dot == 1f)
                return Quaternion.LookRotation(forward, altUp);

            return Quaternion.LookRotation(forward, up);
        }

        protected virtual void GetRaycastEnd(int rayIndex, float rayDistance, out Vector3 origin, out Vector3 direction)
        {
            PortalRay portalRay = raycaster.GetPortalRay(rayIndex);

            Matrix4x4 endMatrix = portalRay.localToWorldMatrix;
            Vector4 column3 = endMatrix.GetColumn(3) + (Vector4)(portalRay.direction.normalized * Mathf.Min(portalRay.direction.magnitude, rayDistance));
            endMatrix.SetColumn(3, column3);

            Portal portal;

            for (int i = raycaster.portalRaysCount - 1; i >= 0; i--)
            {
                portal = raycaster.GetPortalRay(i).fromPortal;

                if (portal && portal.usesTeleport) endMatrix = portal.connectedPortal.ModifyMatrix(endMatrix);
            }

            origin = endMatrix.GetColumn(3);
            direction = endMatrix.GetColumn(2);
        }

        protected virtual void PeformTeleports(int rayCount)
        {
            if (_usesDefaultScale) _target.localScale = defaultScale;

            Matrix4x4 localToWorld = _target.localToWorldMatrix;

            foreach (Portal portal in portalTrace)
            {
                if (!portal)
                {
                    // TODO: This is a way to handle this I guess
                    teleportedLastUpdate = true;
                    continue;
                }

                if (portal.usesTeleport)
                    localToWorld = portal.ModifyMatrix(localToWorld);
            }

            if (teleportedLastUpdate)
            {
                // This position cannot be reached naturally
                PortalPhysics.ForceTeleport(_target, () =>
                {
                    _target.SetPositionAndRotation(localToWorld.GetColumn(3), localToWorld.rotation);
                    if (_usesDefaultScale) _target.localScale = localToWorld.lossyScale;
                });
                teleportedLastUpdate = false;
            }
            else
            {
                _target.SetPositionAndRotation(localToWorld.GetColumn(3), localToWorld.rotation);
                if (_usesDefaultScale) _target.localScale = localToWorld.lossyScale;
            }

            Portal tracePortal, rayPortal;
            int rayIndex = 0;

            // Backtrack portals where required
            for (int portalIndex = 0; portalIndex < portalTrace.Count; portalIndex++)
            {
                TryGetNextPortal(ref rayIndex, rayCount, out rayPortal);

                tracePortal = portalTrace[portalIndex];

                if (rayPortal != tracePortal)
                {
                    // Need to unteleport
                    for (int j = portalTrace.Count - 1; j >= portalIndex; j--)
                    {
                        tracePortal = portalTrace[j].connectedPortal;
                        portalTrace.RemoveAt(j);

                        if (tracePortal) PortalPhysics.Teleport(_target, tracePortal);
                    }

                    break;
                }
            }

            // Forward track raycast teleports
            while (rayIndex < rayCount)
            {
                if (TryGetNextPortal(ref rayIndex, rayCount, out rayPortal))
                {
                    portalTrace.Add(rayPortal);
                    PortalPhysics.Teleport(_target, rayPortal);
                }
            }
        }

        protected virtual Vector3 GetOriginalDirection(int rayCount, Vector3 direction, int rayIndex = 0)
        {
            for (int i = raycaster.portalRaysCount - 1; i >= 0; i--)
            {
                Portal portal = raycaster.GetPortalRay(i).fromPortal;

                if (portal && portal.usesTeleport) portal.connectedPortal.ModifyDirection(ref direction);
            }

            return direction.normalized;
        }

        protected virtual bool TryGetNextPortal(ref int rayIndex, int rayCount, out Portal portal)
        {
            if (rayIndex >= rayCount)
            {
                portal = null;
                return false;
            }

            PortalRay portalRay;

            do
            {
                portalRay = raycaster.GetPortalRay(rayIndex);
                rayIndex++;
            } while (portalRay.fromPortal == null && rayIndex < rayCount);

            portal = portalRay.fromPortal;

            return portal;
        }

        protected virtual void RaycasterPostTeleport(Teleportation args)
        {
            if (args.fromPortal && args.fromPortal.connectedPortal)
            {
                if (portalTrace.Count > 0 && portalTrace[0].connectedPortal == args.fromPortal.connectedPortal)
                    portalTrace.RemoveAt(0);
                else
                {
                    raycasterEnd = args.fromPortal.connectedPortal.ModifyMatrix(raycasterEnd);
                    portalTrace.Insert(0, args.fromPortal.connectedPortal);
                }
            }
            else
                teleportedLastUpdate = true;
        }
    }
}