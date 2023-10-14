using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Physics;
using VRPortalToolkit.Rendering;

namespace VRPortalToolkit
{
    [DefaultExecutionOrder(1020)]
    public class RendererPortalClipping : MonoBehaviour
    {
        private static readonly WaitForFixedUpdate _WaitForFixedUpdate = new WaitForFixedUpdate();

        [SerializeField] private float _clippingOffset = -0.001f;
        public float clippingOffset { get => _clippingOffset; set => _clippingOffset = value; }

        protected readonly TriggerHandler<PortalTransition> triggerHandler = new TriggerHandler<PortalTransition>();
        protected readonly HashSet<Collider> _stayedColliders = new HashSet<Collider>();
        private IEnumerator _waitFixedUpdateLoop;

        private PortalTransition _currentTransition;

        [SerializeField] private List<Renderer> _renderers;
        public List<Renderer> renderers => _renderers;

        private MaterialPropertyBlock _propertyBlock;

        protected virtual void Awake()
        {
            _waitFixedUpdateLoop = WaitFixedUpdateLoop();

            if (_renderers.Count == 0) GetComponentsInChildren(_renderers);
        }

        protected virtual void OnEnable()
        {
            triggerHandler.valueAdded += OnTriggerEnterTransition;
            triggerHandler.valueRemoved += OnTriggerExitTransition;
            StartCoroutine(_waitFixedUpdateLoop);

            PortalPhysics.AddPostTeleportListener(transform, OnPostTeleport);
        }

        protected virtual void LateUpdate()
        {
            if (_renderers == null) return;

            if (_currentTransition && TryGetSlice(_currentTransition, out Vector3 centre, out Vector3 normal))
                UpdateRenderers(centre, normal + normal * _clippingOffset);
            else
                UpdateRenderers(Vector3.zero, Vector3.zero);
        }

        protected virtual void OnDisable()
        {
            triggerHandler.valueAdded -= OnTriggerEnterTransition;
            triggerHandler.valueRemoved -= OnTriggerExitTransition;
            StopCoroutine(_waitFixedUpdateLoop);

            PortalPhysics.RemovePostTeleportListener(transform, OnPostTeleport);
        }

        private void UpdateRenderers(Vector3 centre, Vector3 normal)
        {
            if (_renderers == null) return;

            if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();

            foreach (Renderer renderer in _renderers)
            {
                if (!renderer) continue;

                renderer.GetPropertyBlock(_propertyBlock);

                _propertyBlock.SetVector(PropertyID.ClippingCentre, centre);
                _propertyBlock.SetVector(PropertyID.ClippingNormal, normal);

                renderer.SetPropertyBlock(_propertyBlock);
            }
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            AddTransition(other);
        }

        protected virtual void OnTriggerStay(Collider other)
        {
            if (!triggerHandler.HasCollider(other))
                AddTransition(other);

            _stayedColliders.Add(other);
        }

        private void AddTransition(Collider other)
        {
            PortalTransition transition = other.attachedRigidbody ? other.attachedRigidbody.GetComponent<PortalTransition>() : other.GetComponent<PortalTransition>();
            if (transition) triggerHandler.Add(other, transition);
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
            }
        }

        protected virtual void OnTriggerEnterTransition(PortalTransition transition)
        {
            if (_currentTransition == null)
                _currentTransition = transition;
        }

        protected virtual void OnTriggerExitTransition(PortalTransition transition)
            => RefreshCurrentTransition();

        private void RefreshCurrentTransition()
        {
            _currentTransition = null;

            foreach (PortalTransition transition in triggerHandler.Values)
            {
                _currentTransition = transition;
                return;
            }
        }

        protected virtual bool TryGetSlice(PortalTransition transition, out Vector3 centre, out Vector3 normal)
        {
            if (transition && transition.transitionPlane)
            {
                centre = transition.transitionPlane.position;
                normal = -transition.transitionPlane.forward;

                if (_clippingOffset != 0f)
                    centre -= normal * _clippingOffset;

                return true;
            }

            centre = Vector3.zero;
            normal = Vector3.zero;
            return false;
        }

        protected virtual void OnPostTeleport(Teleportation args)
        {
            if (_currentTransition && _currentTransition.portal && args.fromPortal == _currentTransition.portal)
            {
                _currentTransition = _currentTransition.connectedTransition;
                StartCoroutine(DisableOverrideAfterFixedUpdate());
            }
        }

        protected virtual IEnumerator DisableOverrideAfterFixedUpdate()
        {
            yield return _WaitForFixedUpdate;

            RefreshCurrentTransition();
        }
    }
}
