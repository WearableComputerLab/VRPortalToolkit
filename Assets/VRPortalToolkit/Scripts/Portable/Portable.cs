using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VRPortalToolkit.Data;
using VRPortalToolkit.Physics;

// TODO: When teleporting to a different portal, theres a moment where its no longer in any portal.
// During that time, if it were to move backwards and try to return back through the same portal,
// the system could miss it and just let it walk backwards, need to fix this.

// TODO: Recording previous position is problematic if you run parallel with the portal for a frame.
// It would no longer no what side of the portal you started on.

namespace VRPortalToolkit.Portables
{
    /// <summary>
    /// Represents an object that can be teleported through portals and interact with portal systems.
    /// </summary>
    public class Portable : MonoBehaviour, IPortable
    {
        [SerializeField, Tooltip("The origin used for tracking when a portable passes through a portal.")]
        private Transform _origin;
        /// <summary>
        /// The origin used for tracking when a portable passes through a portal.
        /// </summary>
        public Transform origin {
            get => _origin;
            set => _origin = value;
        }

        [SerializeField, Tooltip("The layer mask used to determine which portals this object can interact with.")]
        private LayerMask _portalLayerMask = 1 << 3;
        /// <summary>
        /// The layer mask used to determine which portals this object can interact with.
        /// </summary>
        public LayerMask portalLayerMask {
            get => _portalLayerMask;
            set => _portalLayerMask = value;
        }

        private Rigidbody _rigidbody;
        /// <summary>
        /// The Rigidbody attached to this portable object, if any.
        /// </summary>
        public new Rigidbody rigidbody => _rigidbody ? _rigidbody : _rigidbody = transform.GetComponent<Rigidbody>();

        [SerializeField, Tooltip("Should children's layer and tags also be updated during teleportation.")]
        private bool _applyToChildren;
        /// <summary>
        /// Should children's layer and tags also be updated during teleportation.
        /// </summary>
        public bool applyToChildren {
            get => _applyToChildren;
            set => _applyToChildren = value;
        }

        /// <summary>
        /// Flags defining different modes for portal interactions.
        /// </summary>
        public enum Mode
        {
            /// <summary>Modify the portal's layer when teleporting.</summary>
            ModifyPortalLayer = 1 << 1,
            /// <summary>Apply layer changes to all children.</summary>
            ApplyLayerToChildren = 1 << 2,
            /// <summary>Apply tag changes to all children.</summary>
            ApplyTagToChildren = 1 << 3,
        }

        [SerializeField, Tooltip("Determines how the override portals list is used.")]
        private OverrideMode _overridePortalsMode;
        /// <summary>
        /// The override mode for which portals this object can use.
        /// </summary>
        public OverrideMode overridePortalsMode {
            get => _overridePortalsMode;
            set => _overridePortalsMode = value;
        }

        [SerializeField, Tooltip("The list of portals to override the default portal set.")]
        private List<Portal> _overridePortals;
        /// <summary>
        /// The list of portals to override the default portal set.
        /// </summary>
        public List<Portal> overridePortals {
            get => _overridePortals;
            set => _overridePortals = value;
        }

        /// <inheritdoc/>
        /// <summary>
        /// Gets the set of valid portals for this portable object based on the override mode.
        /// </summary>
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
        [Header("Portal Events")]
        /// <summary>
        /// Event invoked before teleportation occurs through a portal.
        /// </summary>
        [HideInInspector] public UnityEvent<Portal> preTeleport;
        /// <summary>
        /// Event invoked after teleportation occurs through a portal.
        /// </summary>
        [HideInInspector] public UnityEvent<Portal> postTeleport;


        protected virtual void Reset()
        {
            portalLayerMask = PortalPhysics.defaultPortalLayerMask;
        }

        protected virtual void Awake()
        {

        }

        protected virtual void OnEnable()
        {
            PortalPhysics.RegisterPortable(transform, this);
            AddTeleportListeners(transform);
        }

        protected virtual void OnDisable()
        {
            PortalPhysics.UnregisterPortable(transform, this);
            RemoveTeleportListeners(transform);
        }

        /// <summary>
        /// Adds teleport event listeners to the specified transform.
        /// </summary>
        /// <param name="source">The transform to add listeners to.</param>
        protected virtual void AddTeleportListeners(Transform source)
        {
            if (source)
            {
                PortalPhysics.AddPreTeleportListener(source, PreTeleport);
                PortalPhysics.AddPostTeleportListener(source, PostTeleport);
            }
        }

        /// <summary>
        /// Removes teleport event listeners from the specified transform.
        /// </summary>
        /// <param name="source">The transform to remove listeners from.</param>
        protected virtual void RemoveTeleportListeners(Transform source)
        {
            if (source)
            {
                PortalPhysics.RemovePreTeleportListener(source, PreTeleport);
                PortalPhysics.RemovePostTeleportListener(source, PostTeleport);
            }
        }

        /// <summary>
        /// Called before teleportation occurs.
        /// </summary>
        /// <param name="args">Information about the teleportation event.</param>
        protected virtual void PreTeleport(Teleportation args)
        {
            if (preTeleport != null) preTeleport.Invoke(args.fromPortal);
        }

        /// <summary>
        /// Called after teleportation occurs.
        /// </summary>
        /// <param name="args">Information about the teleportation event.</param>
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

        /// <summary>
        /// Implements the teleportation logic for this portable object.
        /// </summary>
        /// <param name="portal">The portal to teleport through.</param>
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
                    _rigidbody.linearVelocity = portal.ModifyVector(_rigidbody.linearVelocity);
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

        /// <inheritdoc/>
        public Vector3 GetOrigin() => origin ? origin.position : transform.position;
    }
}