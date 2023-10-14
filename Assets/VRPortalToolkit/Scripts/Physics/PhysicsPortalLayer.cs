using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit
{
    [DefaultExecutionOrder(1000)]
    public class PhysicsPortalLayer : MonoBehaviour
    {
        private static readonly WaitForFixedUpdate _WaitForFixedUpdate = new WaitForFixedUpdate();

        [SerializeField] private PortalLayerMode _layerMode = PortalLayerMode.CollidersOnly;
        public virtual PortalLayerMode layerMode
        {
            get => _layerMode;
            set
            {
                if (_layerMode != value)
                {
                    if (isActiveAndEnabled && Application.isPlaying && _portalLayer)
                    {
                        PortalLayer.State previous = _state;
                        SetState(PortalLayer.State.Outside);

                        Validate.UpdateField(this, nameof(_layerMode), _layerMode = value);

                        SetState(previous);
                    }
                    else
                        Validate.UpdateField(this, nameof(_layerMode), _layerMode = value);
                }
            }
        }

        protected readonly TriggerHandler<PortalTransition> transitionHandler = new TriggerHandler<PortalTransition>();
        protected readonly TriggerHandler<PortalLayer> layerHandler = new TriggerHandler<PortalLayer>();
        protected readonly HashSet<Collider> _stayedColliders = new HashSet<Collider>();
        private IEnumerator _waitFixedUpdateLoop;

        public PortalLayer portalLayer => _portalLayer;

        public PortalLayer.State portalLayerState => _state;

        private PortalLayer _portalLayer;

        private PortalLayer.State _state;

        private Collider[] _colliders;

        private void SetState(PortalLayer.State state)
        {
            if (_state != state)
            {
                if (_portalLayer)
                {
                    if (layerMode == PortalLayerMode.CollidersOnly)
                    {
                        if (_colliders != null)
                        {
                            foreach (Collider collider in _colliders)
                                if (collider) collider.gameObject.layer = _portalLayer.ConvertState(_state, state, collider.gameObject.layer);
                        }
                    }
                    else if (layerMode == PortalLayerMode.AllGameObjects)
                    {
                        gameObject.layer = _portalLayer.ConvertState(_state, state, gameObject.layer);

                        foreach (Transform other in transform)
                            if (other) other.gameObject.layer = _portalLayer.ConvertState(_state, state, other.gameObject.layer);
                    }
                }

                _state = state;
            }
        }

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_layerMode), nameof(layerMode));
        }

        protected virtual void Awake()
        {
            _waitFixedUpdateLoop = WaitFixedUpdateLoop();
        }

        protected virtual void Start()
        {
            _colliders = GetComponentsInChildren<Collider>(true);
        }

        protected virtual void OnEnable()
        {
            transitionHandler.valueAdded += OnTriggerEnterTransition;
            transitionHandler.valueRemoved += OnTriggerExitTransition;
            layerHandler.valueAdded += OnTriggerEnterLayer;
            layerHandler.valueRemoved += OnTriggerExitLayer;
            StartCoroutine(_waitFixedUpdateLoop);

            PortalPhysics.AddPreTeleportListener(transform, OnPreTeleport);
            PortalPhysics.AddPostTeleportListener(transform, OnPostTeleport);
        }

        protected virtual void OnDisable()
        {
            transitionHandler.valueAdded -= OnTriggerEnterTransition;
            transitionHandler.valueRemoved -= OnTriggerExitTransition;
            layerHandler.valueAdded -= OnTriggerEnterLayer;
            layerHandler.valueRemoved -= OnTriggerExitLayer;
            StopCoroutine(_waitFixedUpdateLoop);

            PortalPhysics.RemovePreTeleportListener(transform, OnPreTeleport);
            PortalPhysics.RemovePostTeleportListener(transform, OnPostTeleport);
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            AddTransition(other);
            AddLayer(other);
        }

        protected virtual void OnTriggerStay(Collider other)
        {
            if (!transitionHandler.HasCollider(other))
                AddTransition(other);

            if (!layerHandler.HasCollider(other))
                AddLayer(other);

            _stayedColliders.Add(other);
        }

        private void AddTransition(Collider other)
        {
            PortalTransition transition = other.attachedRigidbody ? other.attachedRigidbody.GetComponent<PortalTransition>() : other.GetComponent<PortalTransition>();
            if (transition) transitionHandler.Add(other, transition);
        }

        private void AddLayer(Collider other)
        {
            PortalLayer layer = other.attachedRigidbody ? other.attachedRigidbody.GetComponent<PortalLayer>() : other.GetComponent<PortalLayer>();
            if (layer) layerHandler.Add(other, layer);
        }

        protected virtual void OnTriggerExit(Collider other)
        {
            transitionHandler.RemoveCollider(other);
            layerHandler.RemoveCollider(other);
        }

        private IEnumerator WaitFixedUpdateLoop()
        {
            while (true)
            {
                yield return _WaitForFixedUpdate;

                transitionHandler.UpdateColliders(_stayedColliders);
                layerHandler.UpdateColliders(_stayedColliders);
                _stayedColliders.Clear();
            }
        }

        #region Trigger Events

        protected virtual void OnTriggerEnterLayer(PortalLayer layer)
            => RefreshPortalLayer();

        protected virtual void OnTriggerExitLayer(PortalLayer layer)
            => RefreshPortalLayer();

        protected virtual void OnTriggerEnterTransition(PortalTransition transition)
            => RefreshPortalLayer();

        protected virtual void OnTriggerExitTransition(PortalTransition transition)
            => RefreshPortalLayer();

        private void RefreshPortalLayer()
        {
            // Layer may no longer be available
            if (_portalLayer != null && !layerHandler.HasValue(_portalLayer))
            {
                SetState(PortalLayer.State.Outside);
                _portalLayer = null;
            }
            else if (_portalLayer && _portalLayer && transitionHandler.HasValue(_portalLayer.portalTransition))
            {
                // Might just need to enter the transition
                SetState(PortalLayer.State.Inside);
                return;
            }

            PortalLayer current = _portalLayer;

            foreach (PortalLayer layer in layerHandler.Values)
            {
                if (layer)
                {
                    // Inside layer so use this one
                    if (layer.portalTransition && transitionHandler.HasValue(_portalLayer.portalTransition))
                    {
                        // Leave the current one
                        if (_portalLayer != null)
                        {
                            SetState(PortalLayer.State.Outside);
                            _portalLayer = null;
                        }

                        // Enter the new one
                        _portalLayer = layer;
                        SetState(PortalLayer.State.Inside);
                        return;
                    }

                    // Use this one if theres nothing better
                    if (current == null) current = layer;
                }

                return;
            }

            if (current != _portalLayer)
            {
                _portalLayer = current;
                SetState(PortalLayer.State.Between);
            }
        }

        #endregion

        private PortalLayer.State preTeleportState;
        protected virtual void OnPreTeleport(Teleportation args)
        {
            if (_portalLayer && _portalLayer.portal && _portalLayer.portal == args.fromPortal)
            {
                preTeleportState = _state;
                SetState(PortalLayer.State.Outside);
            }
        }

        protected virtual void OnPostTeleport(Teleportation args)
        {
            if (_portalLayer && _portalLayer.portal && _portalLayer.portal == args.fromPortal)
            {
                _portalLayer = _portalLayer.connectedLayer;
                SetState(preTeleportState);
            }
        }
    }
}
