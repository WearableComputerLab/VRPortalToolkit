using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.Rendering
{
    public class PortalCameraTransition : MonoBehaviour, IPortalCameraTransition
    {
        private readonly static WaitForFixedUpdate _WaitForFixedUpdate = new WaitForFixedUpdate();

        private Camera _camera;
        public new Camera camera { get => _camera; }

        int IPortalCameraTransition.layer => transition != null && transition.portal != null ? transition.portal.gameObject.layer : 0; // ?

        IPortal IPortalCameraTransition.portal => transition != null ? transition.portal : null;

        private PortalTransition _overrideTransition;
        public PortalTransition transition => _overrideTransition != null ? _overrideTransition : (_transitions.Count > 0 ? _transitions[0] : null);

        private readonly List<PortalTransition> _transitions = new List<PortalTransition>();
        private readonly TriggerHandler<PortalTransition> triggerHandler = new TriggerHandler<PortalTransition>();
        private readonly HashSet<Collider> _stayedColliders = new HashSet<Collider>();
        private IEnumerator _waitFixedUpdateLoop;


        protected virtual void Awake()
        {
            _camera = GetComponent<Camera>();
            _waitFixedUpdateLoop = WaitFixedUpdateLoop();
        }

        protected virtual void OnEnable()
        {
            triggerHandler.valueAdded += OnTriggerEnterTransition;
            triggerHandler.valueRemoved += OnTriggerExitTransition;
            PortalPhysics.AddPostTeleportListener(transform, OnPostTeleport);
            StartCoroutine(_waitFixedUpdateLoop);
        }

        protected virtual void OnDisable()
        {
            triggerHandler.valueAdded -= OnTriggerEnterTransition;
            triggerHandler.valueRemoved -= OnTriggerExitTransition;
            PortalPhysics.RemovePostTeleportListener(transform, OnPostTeleport);
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
            Transform source = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;

            if (source.TryGetComponent(out PortalTransition transition))
                triggerHandler.Add(other, transition);
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

                triggerHandler.UpdateColliders(_stayedColliders);
                _stayedColliders.Clear();

                // If override was the last to be removed, stop the transition
                if (_overrideTransition != null)
                {
                    if (_transitions.Count == 0)
                        PortalRendering.UnregisterCameraTranstion(_camera, this);

                    _overrideTransition = null;
                }
            }
        }

        protected virtual void OnTriggerEnterTransition(PortalTransition other)
        {
            _transitions.Add(other);
            if (_transitions.Count == 1 && _overrideTransition == null)
            {
                // This is the first
                PortalRendering.RegisterCameraTranstion(_camera, this);
            }
        }

        protected virtual void OnTriggerExitTransition(PortalTransition other)
        {
            _transitions.Remove(other);
            if (_transitions.Count == 0 && _overrideTransition == null)
            {
                // This is the last transition
                PortalRendering.UnregisterCameraTranstion(_camera, this);
            }
        }

        void IPortalCameraTransition.GetTransitionPlane(out Vector3 planeCentre, out Vector3 planeNormal)
        {
            PortalTransition current = transition;

            if (current != null)
            {
                if (current.transitionPlane)
                {
                    planeCentre = current.transitionPlane.position;
                    planeNormal = current.transitionPlane.forward;
                }
                else
                {
                    planeCentre = current.transform.position;
                    planeNormal = current.transform.forward;
                }
            }
            else
            {
                planeCentre = default;
                planeNormal = default;
            }
        }

        private void OnPostTeleport(Teleportation teleportation)
        {
            if (_overrideTransition == null)
            {
                foreach (PortalTransition transition in _transitions)
                {
                    if (transition.portal == teleportation.fromPortal)
                    {
                        _overrideTransition = transition.connectedTransition;
                        break;
                    }
                }
            }
            else if (_overrideTransition.portal == teleportation.fromPortal)
                _overrideTransition = _overrideTransition.connectedTransition;
            else
                _overrideTransition = null;

            _stayedColliders.Clear();
            triggerHandler.UpdateColliders(_stayedColliders);
        }
    }
}
