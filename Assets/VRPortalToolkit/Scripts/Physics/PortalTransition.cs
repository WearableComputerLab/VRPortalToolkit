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
    // TODO: Why is the trigger logic and stuff on the transition, not the transition handler?

    public class PortalTransition : MonoBehaviour, IPortableHandler
    {
        private readonly static WaitForFixedUpdate _WaitForFixedUpdate = new WaitForFixedUpdate();

        [SerializeField] private Portal _portal;
        public Portal portal
        {
            get => _portal;
            set => _portal = value;
        }

        [SerializeField] private PortalTransition _connectedTransition;
        public PortalTransition connectedTransition
        {
            get => _connectedTransition;
            set => _connectedTransition = value;
        }

        [SerializeField] private Transform _transitionPlane;
        public Transform transitionPlane
        {
            get => _transitionPlane;
            set => _transitionPlane = value;
        }

        // Used by the connected to tell this what else its also tracking
        private Dictionary<Transform, bool> _overrideTracked = new Dictionary<Transform, bool>();

        protected readonly TriggerHandler<Transform> triggerHandler = new TriggerHandler<Transform>();
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

        protected virtual void OnTriggerEnterContainer(Transform other)
        {
            if (!_overrideTracked.ContainsKey(other))
                PortalPhysics.RegisterPortableHandler(this, other);
        }

        protected virtual void OnTriggerExitContainer(Transform other)
        {
            if (!_overrideTracked.ContainsKey(other))
                PortalPhysics.UnregisterPortableHandler(this, other);
        }

        public bool TryTeleportPortable(Transform target, IPortable portable)
        {
            if (!_transitionPlane) return false;

            bool passedThrough = _transitionPlane.InverseTransformPoint(portable.GetOrigin()).z < 0f;

            if (passedThrough)// && _tracked.Contains(target))
            {
                //StartCoroutine(TempPass(target));

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

            /*if (passedThrough && _tracked.Contains(target))
            {
                StartCoroutine(TempPass(target));
                portable.Teleport(portal);

                return true;
            }

            if (passedThrough)
                _tracked.Remove(target);
            else
                _tracked.Add(target);*/

            return false;
        }

        /*private IEnumerator TempPass(Transform target)
        {
            PortalTransition connected;

            if (!connected || connected._tracked.Contains(target))
                yield break;

            triggerHandler.RemoveCollider
            Debug.Log("XQ");
            _tracked.Remove(transform);
            PortalPhysics.UnregisterPortableHandler(this, target);

            if (_connectedTransition)
            {
                _connectedTransition._tracked.Add(target);
                PortalPhysics.RegisterPortableHandler(_connectedTransition, target);
            }

            yield return _WaitForFixedUpdate;

            if (triggerHandler.HasValue(target))
            {
                Debug.Log("AQ");
                PortalPhysics.RegisterPortableHandler(this, target);
            }

            // If it never got added to the connected transition, remove it
            if (_connectedTransition && !_connectedTransition.triggerHandler.HasValue(target))
            {
                Debug.Log("BQ");
                _connectedTransition._tracked.Remove(target);
                PortalPhysics.UnregisterPortableHandler(_connectedTransition, target);
            }
        }*/
    }
}
