using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;
using VRPortalToolkit.Utilities;
using VRPortalToolkit.Data;
using VRPortalToolkit.Physics;

// TODO: When teleporting to a different portal, theres a moment where its no longer in any portal.
// During that time, if it were to move backwards and try to return back through the same portal,
// the system could miss it and just let it walk backwards, need to fix this.

// TODO: Recording previous position is problematic if you run parallel with the portal for a frame.
// It would no longer no what side of the portal you started on.

namespace VRPortalToolkit.Portables
{
    public class Portable : MonoBehaviour, IPortable
    {
        [SerializeField] private Transform _origin;
        public Transform origin {
            get => _origin;
            set => _origin = value;
        }

        [SerializeField] private LayerMask _portalLayerMask = 1 << 3;
        public LayerMask portalLayerMask {
            get => _portalLayerMask;
            set => _portalLayerMask = value;
        }

        private Rigidbody _rigidbody;
        public new Rigidbody rigidbody => _rigidbody ? _rigidbody : _rigidbody = transform.GetComponent<Rigidbody>();

        /// <summary>Should children's layer and tags also be updated during teleportation?<summary/>
        [SerializeField] private bool _applyToChildren;
        public bool applyToChildren {
            get => _applyToChildren;
            set => _applyToChildren = value;
        }

        public enum Mode
        {
            ModifyPortalLayer = 1 << 1,
            ApplyLayerToChildren = 1 << 2,
            ApplyTagToChildren = 1 << 3,
        }

        [SerializeField] private OverrideMode _overridePortalsMode;
        public OverrideMode overridePortalsMode {
            get => _overridePortalsMode;
            set => _overridePortalsMode = value;
        }

        [SerializeField] private List<Portal> _overridePortals;
        public List<Portal> overridePortals {
            get => _overridePortals;
            set => _overridePortals = value;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<Portal> validPortals {
            get {
                switch (_overridePortalsMode)
                {
                    case OverrideMode.Ignore:
                        foreach (Portal portal in PortalPhysics.allPortals)
                        {
                            if (!_overridePortals.Contains(portal))
                                yield return portal;
                        }
                        break;

                    case OverrideMode.Replace:
                        foreach (Portal portal in _overridePortals)
                            yield return portal;
                        break;

                    default:
                        foreach (Portal renderer in PortalPhysics.allPortals)
                            yield return renderer;
                        break;
                }
            }
        }

        // These are deprecated
        //[Header("Portal Events")]
        [HideInInspector] public UnityEvent<Portal> preTeleport;
        [HideInInspector] public UnityEvent<Portal> postTeleport;

        /*protected class PortableHandler : IPortable
        {
            public Portable portable;

            public PortableHandler(Portable portable)
            {
                this.portable = portable;
            }

            public LayerMask portalLayerMask => (portable) ? (portable.portalLayerMask) : ((LayerMask)1 << 3);

            public Vector3 GetOrigin()
            {
                if (portable)
                {
                    if (portable.origin)
                        return portable.origin.position;

                    return portable.transform.position;
                }

                return Vector3.zero;
            }

            public void Teleport(Portal portal) => portable?.Teleport(portal);
        }*/

        protected virtual void Reset()
        {
            portalLayerMask = PortalPhysics.defaultPortalLayerMask;
        }

        protected virtual void Awake()
        {

        }

        protected virtual void OnEnable()
        {
            AddTeleportListeners(transform);
            PortalPhysics.TrackPortable(transform, this);
        }

        protected virtual void OnDisable()
        {
            RemoveTeleportListeners(transform);
            PortalPhysics.UntrackPortable(transform, this);
        }

        protected virtual void AddTeleportListeners(Transform source)
        {
            if (source)
            {
                PortalPhysics.AddPreTeleportListener(source, PreTeleport);
                PortalPhysics.AddPostTeleportListener(source, PostTeleport);
            }
        }

        protected virtual void RemoveTeleportListeners(Transform source)
        {
            if (source)
            {
                PortalPhysics.RemovePreTeleportListener(source, PreTeleport);
                PortalPhysics.RemovePostTeleportListener(source, PostTeleport);
            }
        }

        protected virtual void PreTeleport(Teleportation args)
        {
            if (preTeleport != null) preTeleport.Invoke(args.fromPortal);
        }

        protected virtual void PostTeleport(Teleportation args)
        {
            if (postTeleport != null) postTeleport.Invoke(args.fromPortal);
        }

        /// <inheritdoc/>
        public virtual void Teleport(Portal portal)
        {
            if (portal && IsValid(portal))
                PortalPhysics.ForceTeleport(transform, () => TeleportLogic(portal), this, portal);
        }

        protected virtual void TeleportLogic(Portal portal)
        {
            if (portal.usesTeleport)
            {
                Matrix4x4 matrix = portal.ModifyMatrix(transform.localToWorldMatrix);

                transform.position = matrix.GetColumn(3);
                transform.rotation = matrix.rotation;
                transform.localScale = matrix.lossyScale;

                if (rigidbody)
                {
                    _rigidbody.velocity = portal.ModifyVector(_rigidbody.velocity);
                    _rigidbody.angularVelocity = portal.ModifyVector(_rigidbody.angularVelocity);
                }
            }

            if (_applyToChildren)
            {
                foreach (Transform child in transform)
                {
                    if (portal.usesTag)
                        child.tag = portal.ModifyTag(child.tag);

                    if (portal.usesLayers)
                        child.gameObject.layer = portal.ModifyLayer(child.gameObject.layer);
                }
            }
            else
            {
                if (portal.usesTag)
                    transform.tag = portal.ModifyTag(transform.tag);

                if (portal.usesLayers)
                    transform.gameObject.layer = portal.ModifyLayer(transform.gameObject.layer);
            }
        }

        /// <inheritdoc/>
        public virtual bool IsValid(Portal portal)
        {
            switch (_overridePortalsMode)
            {
                case OverrideMode.Ignore:
                    return !_overridePortals.Contains(portal);

                case OverrideMode.Replace:
                    return _overridePortals.Contains(portal);

                default:
                    return true;
            }
        }

        public Vector3 GetOrigin() => origin ? origin.position : transform.position;
    }
}