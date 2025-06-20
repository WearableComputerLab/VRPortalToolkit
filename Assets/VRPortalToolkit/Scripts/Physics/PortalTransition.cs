using Misc.EditorHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRPortalToolkit.Physics;
using VRPortalToolkit.Portables;
using static UnityEngine.GraphicsBuffer;

namespace VRPortalToolkit
{
    /// <summary>
    /// Handles the transition of portables through a portal, including trigger logic and teleportation.
    /// </summary>
    public class PortalTransition : MonoBehaviour, IPortableHandler
    {
        private readonly static WaitForFixedUpdate _WaitForFixedUpdate = new WaitForFixedUpdate();

        /// <summary>
        /// The portal associated with this transition.
        /// </summary>
        [Tooltip("The portal associated with this transition.")]
        [SerializeField] private Portal _portal;
        /// <summary>
        /// Gets or sets the portal associated with this transition.
        /// </summary>
        public Portal portal
        {
            get => _portal;
            set => _portal = value;
        }

        /// <summary>
        /// The connected transition on the other side of the portal.
        /// </summary>
        [Tooltip("The connected transition on the other side of the portal.")]
        [SerializeField] private PortalTransition _connectedTransition;
        /// <summary>
        /// Gets or sets the connected transition on the other side of the portal.
        /// </summary>
        public PortalTransition connectedTransition
        {
            get => _connectedTransition;
            set => _connectedTransition = value;
        }

        /// <summary>
        /// The transform representing the transition plane.
        /// </summary>
        [Tooltip("The transform representing the transition plane.")]
        [SerializeField] private Transform _transitionPlane;
        /// <summary>
        /// Gets or sets the transform representing the transition plane.
        /// </summary>
        public Transform transitionPlane
        {
            get => _transitionPlane;
            set => _transitionPlane = value;
        }

        // Used by the connected to tell this what else its also tracking
        private Dictionary<Transform, bool> _overrideTracked = new Dictionary<Transform, bool>();

        /// <summary>
        /// Trigger handler for managing tracked transforms.
        /// </summary>
        protected readonly TriggerHandler<Transform> triggerHandler = new TriggerHandler<Transform>();
        /// <summary>
        /// Set of colliders that stayed during the last frame.
        /// </summary>
        protected readonly HashSet<Collider> _stayedColliders = new HashSet<Collider>();
        private IEnumerator _waitFixedUpdateLoop;

        protected virtual void Reset()
        {
            //PortalPhysics.TrackPortable
            portal = GetComponentInChildren<Portal>(true);
            if (!portal) portal = GetComponentInParent<Portal>();

            if (portal && portal.connected)
            {
                connectedTransition = portal.connected.GetComponentInChildren<PortalTransition>(true);
                if (!connectedTransition) connectedTransition = portal.connected.gameObject.GetComponentInParent<PortalTransition>(true);
            }

            transitionPlane = transform;
        }

        protected virtual void Awake()
        {
            _waitFixedUpdateLoop = WaitFixedUpdateLoop();
        }

        protected virtual void OnEnable()
        {
            triggerHandler.valueAdded += OnTriggerEnterContainer;
            triggerHandler.valueRemoved += OnTriggerExitContainer;
            StartCoroutine(_waitFixedUpdateLoop);
        }

        protected virtual void OnDisable()
        {
            triggerHandler.valueAdded -= OnTriggerEnterContainer;
            triggerHandler.valueRemoved -= OnTriggerExitContainer;
            StopCoroutine(_waitFixedUpdateLoop);
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            AddContainer(other);
        }

        protected virtual void OnTriggerStay(Collider other)
        {
            if (!triggerHandler.HasCollider(other))
                AddContainer(other);

            _stayedColliders.Add(other);
        }

        private void AddContainer(Collider other)
        {
            triggerHandler.Add(other, other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform);
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            triggerHandler.RemoveCollider(other);
        }

        private IEnumerator WaitFixedUpdateLoop()
        {
            while (true)
            {
                yield return _WaitForFixedUpdate;

                foreach (var pair in _overrideTracked)
                {
                    if (triggerHandler.HasValue(pair.Key))
                    {
                        if (!pair.Value) // Has not been registered, needs to be registered
                            PortalPhysics.RegisterPortableHandler(this, pair.Key);
                    }
                    else
                    {
                        if (pair.Value) // Has been registered, needs to be unregistered
                            PortalPhysics.UnregisterPortableHandler(this, pair.Key);
                    }
                }
                _overrideTracked.Clear();

                triggerHandler.UpdateColliders(_stayedColliders);
                _stayedColliders.Clear();
            }
        }

        /// <summary>
        /// Called when a transform enters the trigger container.
        /// </summary>
        /// <param name="other">The transform that entered the container.</param>
        protected virtual void OnTriggerEnterContainer(Transform other)
        {
            if (!_overrideTracked.ContainsKey(other))
                PortalPhysics.RegisterPortableHandler(this, other);
        }

        /// <summary>
        /// Called when a transform exits the trigger container.
        /// </summary>
        /// <param name="other">The transform that exited the container.</param>
        protected virtual void OnTriggerExitContainer(Transform other)
        {
            if (!_overrideTracked.ContainsKey(other))
                PortalPhysics.UnregisterPortableHandler(this, other);
        }

        /// <inheritdoc/>
        public bool TryTeleportPortable(Transform target, IPortable portable)
        {
            if (!_transitionPlane) return false;

            bool passedThrough = _transitionPlane.InverseTransformPoint(portable.GetOrigin()).z < 0f;

            if (passedThrough)
            {
                // Remove it from mine
                if (!_overrideTracked.ContainsKey(target))
                {
                    _overrideTracked.Add(target, false);

                    if (triggerHandler.HasValue(target))
                        PortalPhysics.UnregisterPortableHandler(this, target);
                }

                // Pass it to the other
                if (_connectedTransition && !_connectedTransition._overrideTracked.ContainsKey(target))
                {
                    _connectedTransition._overrideTracked.Add(target, true);

                    if (!_connectedTransition.triggerHandler.HasValue(target))
                        PortalPhysics.RegisterPortableHandler(_connectedTransition, target);
                }

                portable.Teleport(portal);

                return true;
            }

            return false;
        }
    }
}
