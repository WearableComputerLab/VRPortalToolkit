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
    /// <summary>
    /// Manages the cursor position and rotation for a portal pointer.
    /// </summary>
    [DefaultExecutionOrder(101)] // Execute after pointer
    public class PortalPointerCursor : MonoBehaviour
    {
        [Tooltip("The PortalPointer that provides raycast data.")]
        [SerializeField] private PortalPointer _raycaster;
        /// <summary>
        /// The PortalPointer that provides raycast data.
        /// </summary>
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

        [Tooltip("The transform to move to the cursor position.")]
        [SerializeField] private Transform _target;
        /// <summary>
        /// The transform to move to the cursor position.
        /// </summary>
        public virtual Transform target
        {
            get => _target;
            set => _target = value;
        }

        /// <summary>
        /// Determines how the cursor is rotated when there is no hit.
        /// </summary>
        public enum DefaultMode
        {
            /// <summary>Only position the cursor, don't change rotation.</summary>
            PositionOnly = 0,
            /// <summary>Orient the cursor in the forward direction.</summary>
            Forward = 1,
            /// <summary>Orient the cursor in the backward direction.</summary>
            Backward = 2
        }

        [Tooltip("How the cursor is rotated when there is no hit.")]
        [SerializeField] public DefaultMode _defaultMode = DefaultMode.Forward;
        /// <summary>
        /// How the cursor is rotated when there is no hit.
        /// </summary>
        public DefaultMode defaultMode
        {
            get => _defaultMode;
            set => _defaultMode = value;
        }

        /// <summary>
        /// Determines how the cursor is rotated when there is a hit.
        /// </summary>
        public enum HitMode
        {
            /// <summary>Ignore hits and use default mode.</summary>
            Ignore = 0,
            /// <summary>Only position the cursor, don't change rotation.</summary>
            PositionOnly = 1,
            /// <summary>Orient the cursor in the forward direction.</summary>
            Forward = 2,
            /// <summary>Orient the cursor in the backward direction.</summary>
            Backward = 3,
            /// <summary>Orient the cursor based on the hit normal.</summary>
            Normal = 4,
            /// <summary>Orient the cursor based on the reversed hit normal.</summary>
            Reversed = 5
        }

        [Tooltip("How the cursor is rotated when there is a hit.")]
        [SerializeField] public HitMode _hitMode = HitMode.Normal;
        /// <summary>
        /// How the cursor is rotated when there is a hit.
        /// </summary>
        public virtual HitMode hitMode
        {
            get => _hitMode;
            set => _hitMode = value;
        }

        [Header("Optional"), Tooltip("Optional transform to use for determining the up direction.")]
        [SerializeField] private Transform _upright;
        /// <summary>
        /// Optional transform to use for determining the up direction.
        /// </summary>
        public virtual Transform upright
        {
            get => _upright;
            set => _upright = value;
        }

        [Tooltip("Whether to use a default scale for the cursor.")]
        [SerializeField] private bool _usesDefaultScale = false;
        /// <summary>
        /// Whether to use a default scale for the cursor.
        /// </summary>
        public virtual bool usesDefaultScale {
            get => _usesDefaultScale;
            set => _usesDefaultScale = value;
        }

        [ShowIf(nameof(usesDefaultScale))]
        [Tooltip("The default scale to use for the cursor.")]
        [SerializeField] private Vector3 _defaultScale = Vector3.one;
        /// <summary>
        /// The default scale to use for the cursor.
        /// </summary>
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

        /// <summary>
        /// Adds teleport listeners to the raycaster.
        /// </summary>
        /// <param name="raycaster">The raycaster to add listeners to.</param>
        protected virtual void AddRaycasterListeners(PortalPointer raycaster)
        {
            if (raycaster) PortalPhysics.AddPostTeleportListener(raycaster.transform, RaycasterPostTeleport);
        }

        /// <summary>
        /// Removes teleport listeners from the raycaster.
        /// </summary>
        /// <param name="raycaster">The raycaster to remove listeners from.</param>
        protected virtual void RemoveRaycasterListeners(PortalPointer raycaster)
        {
            if (raycaster) PortalPhysics.RemovePostTeleportListener(raycaster.transform, RaycasterPostTeleport);
        }

        public virtual void FixedUpdate()
        {
            Apply();
        }

        /// <summary>
        /// Applies the cursor logic, positioning and rotating it based on the raycaster.
        /// </summary>
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

        /// <summary>
        /// Gets a rotation for the cursor based on direction vectors.
        /// </summary>
        /// <param name="forward">The forward direction.</param>
        /// <param name="altUp">The alternative up direction.</param>
        /// <returns>The calculated rotation.</returns>
        protected virtual Quaternion GetRotation(Vector3 forward, Vector3 altUp)
        {
            Vector3 up = upright ? upright.up : Vector3.up;

            float dot = Vector3.Dot(up, forward);

            if (dot == -1f || dot == 1f)
                return Quaternion.LookRotation(forward, altUp);

            return Quaternion.LookRotation(forward, up);
        }

        /// <summary>
        /// Gets the end position and direction of a raycast.
        /// </summary>
        /// <param name="rayIndex">The index of the ray to get the end for.</param>
        /// <param name="rayDistance">The distance along the ray.</param>
        /// <param name="origin">Output parameter for the calculated origin.</param>
        /// <param name="direction">Output parameter for the calculated direction.</param>
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

                if (portal && portal.usesTeleport) endMatrix = portal.connected.ModifyMatrix(endMatrix);
            }

            origin = endMatrix.GetColumn(3);
            direction = endMatrix.GetColumn(2);
        }

        /// <summary>
        /// Performs teleportations for the cursor through portals.
        /// </summary>
        /// <param name="rayCount">The number of rays to consider for teleportation.</param>
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
                        tracePortal = portalTrace[j].connected;
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

        /// <summary>
        /// Converts a direction from world space to the original space before portal transformations.
        /// </summary>
        /// <param name="rayCount">The number of rays to consider for transformations.</param>
        /// <param name="direction">The direction to convert.</param>
        /// <param name="rayIndex">The starting ray index.</param>
        /// <returns>The direction in original space.</returns>
        protected virtual Vector3 GetOriginalDirection(int rayCount, Vector3 direction, int rayIndex = 0)
        {
            for (int i = raycaster.portalRaysCount - 1; i >= 0; i--)
            {
                Portal portal = raycaster.GetPortalRay(i).fromPortal;

                if (portal && portal.usesTeleport) portal.connected.ModifyDirection(ref direction);
            }

            return direction.normalized;
        }

        /// <summary>
        /// Tries to get the next portal in the raycast sequence.
        /// </summary>
        /// <param name="rayIndex">The current ray index, will be incremented if a portal is found.</param>
        /// <param name="rayCount">The total number of rays.</param>
        /// <param name="portal">Output parameter for the found portal.</param>
        /// <returns>True if a portal was found, false otherwise.</returns>
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

        /// <summary>
        /// Called after the raycaster teleports through a portal.
        /// </summary>
        /// <param name="args">The teleportation data.</param>
        protected virtual void RaycasterPostTeleport(Teleportation args)
        {
            if (args.fromPortal && args.fromPortal.connected)
            {
                if (portalTrace.Count > 0 && portalTrace[0].connected == args.fromPortal.connected)
                    portalTrace.RemoveAt(0);
                else
                {
                    raycasterEnd = args.fromPortal.connected.ModifyMatrix(raycasterEnd);
                    portalTrace.Insert(0, args.fromPortal.connected);
                }
            }
            else
                teleportedLastUpdate = true;
        }
    }
}