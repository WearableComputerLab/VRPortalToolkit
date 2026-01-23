using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Physics;
using Misc.EditorHelpers;
using VRPortalToolkit.Cloning;
using Misc;

namespace VRPortalToolkit
{
    /// <summary>
    /// Handles cloning of objects as they intersect with portals for seamless transitions.
    /// The clones mimic the original object's physics on the other side of the portal,
    /// allowing for a more consistent experience when moving objects through portals.
    /// </summary>
    [DefaultExecutionOrder(1010)]
    public class PortalPhysicsClone : MonoBehaviour
    {
        private static readonly WaitForFixedUpdate _WaitForFixedUpdate = new WaitForFixedUpdate();

        [Tooltip("The original GameObject to be cloned and mimicked.")]
        [SerializeField] private GameObject _original;
        /// <summary>
        /// The original GameObject to be cloned and mimicked.
        /// </summary>
        public virtual GameObject original
        {
            get => _original;
            set
            {
                if (_original != value)
                {
                    Validate.UpdateField(this, nameof(_original), _original = value);

                    _clonePool.Clear();

                    foreach (var pair in currentClones)
                        BeginCloneHandler(pair.Key, pair.Value);
                }
            }
        }

        [Tooltip("The template GameObject used for cloning. Otherwise, the original gameObject will be cloned with only the relevant components.")]
        [SerializeField] private GameObject _template;
        /// <summary>
        /// The template GameObject used for cloning. Otherwise, the original gameObject will be cloned with only the relevant components.
        /// </summary>
        public virtual GameObject template { get => _template; set => _template = value; }

        [Tooltip("The maximum number of clones allowed. Or -1 for unlimited.")]
        [SerializeField] private int _maxCloneCount = -1;
        /// <summary>
        /// The maximum number of clones allowed. Or -1 for unlimited.
        /// </summary>
        public virtual int maxCloneCount
        {
            get => _maxCloneCount;
            set
            {
                if (_maxCloneCount != value)
                {
                    Validate.UpdateField(this, nameof(_maxCloneCount), _maxCloneCount = value);

                    if (isActiveAndEnabled && Application.isPlaying && !teleportOverride) GenerateClones();
                }
            }
        }

        [Tooltip("The layer mode for the original object.")]
        [SerializeField] private PortalLayerMode _originalLayerMode = PortalLayerMode.CollidersOnly;
        /// <summary>
        /// The layer mode for the original object.
        /// </summary>
        public virtual PortalLayerMode originalLayerMode
        {
            get => _originalLayerMode;
            set
            {
                if (_originalLayerMode != value)
                {
                    if (isActiveAndEnabled && Application.isPlaying && localLayer)
                    {
                        PortalLayer.State previous = localState;
                        localState = PortalLayer.State.Outside;

                        Validate.UpdateField(this, nameof(_originalLayerMode), _originalLayerMode = value);

                        localState = previous;
                    }
                    else
                        Validate.UpdateField(this, nameof(_originalLayerMode), _originalLayerMode = value);
                }
            }
        }

        [Tooltip("The layer mode for the clone.")]
        [SerializeField] private PortalLayerMode _cloneLayerMode = PortalLayerMode.CollidersOnly;
        /// <summary>
        /// The layer mode for the clone.
        /// </summary>
        public virtual PortalLayerMode cloneLayerMode
        {
            get => _cloneLayerMode;
            set
            {
                if (_cloneLayerMode != value)
                {
                    Validate.UpdateField(this, nameof(_cloneLayerMode), _cloneLayerMode = value);

                    // NOTE: Dont bother updating this because it will get corrected on the next one
                }
            }
        }

        /// <summary>
        /// Specifies the layer mode for objects interacting with portals.
        /// </summary>
        public enum PortalLayerMode
        {
            /// <summary>
            /// Ignore portal layers.
            /// </summary>
            Ignore = 0,

            /// <summary>
            /// Apply portal layers only to colliders.
            /// </summary>
            CollidersOnly = 1,

            /// <summary>
            /// Apply portal layers to all GameObjects.
            /// </summary>
            AllGameObjects = 2,
        }

        /// <summary>
        /// Stores information about a clone and its associated components.
        /// </summary>
        protected class CloneHandler
        {
            /// <summary>
            /// The original GameObject being cloned.
            /// </summary>
            public GameObject original;

            /// <summary>
            /// The cloned GameObject.
            /// </summary>
            public GameObject clone;

            /// <summary>
            /// The portal associated with this clone.
            /// </summary>
            public Portal portal;

            /// <summary>
            /// List of transform component pairs between original and clone.
            /// </summary>
            public List<PortalCloneInfo<Transform>> transforms = new List<PortalCloneInfo<Transform>>();

            /// <summary>
            /// List of rigidbody component pairs between original and clone.
            /// </summary>
            public List<PortalCloneInfo<Rigidbody>> rigidbodies = new List<PortalCloneInfo<Rigidbody>>();

            /// <summary>
            /// List of collider component pairs between original and clone.
            /// </summary>
            public List<PortalCloneInfo<Collider>> colliders = new List<PortalCloneInfo<Collider>>();
        }

        protected List<Component> sortedTransitionsAndLayers = new List<Component>();
        protected Dictionary<Component, CloneHandler> currentClones = new Dictionary<Component, CloneHandler>();
        protected ObjectPool<CloneHandler> _clonePool = new ObjectPool<CloneHandler>(() => new CloneHandler(), null,
                i => i.clone?.SetActive(false), i => { if (i.clone) Destroy(i.clone); });

        protected readonly TriggerHandler<PortalTransition> transitionHandler = new TriggerHandler<PortalTransition>();
        protected readonly TriggerHandler<PortalLayer> layerHandler = new TriggerHandler<PortalLayer>();
        protected readonly HashSet<Collider> _stayedColliders = new HashSet<Collider>();
        private IEnumerator _waitFixedUpdateLoop;

        private PortalLayer _localLayer;
        /// <summary>
        /// The current local portal layer for this object.
        /// </summary>
        protected PortalLayer localLayer
        {
            get => _localLayer;
            set
            {
                if (_localLayer != value)
                {
                    localState = PortalLayer.State.Outside;
                    _localLayer = value;
                }
            }
        }

        private PortalLayer.State _localState;
        /// <summary>
        /// The current state of the local portal layer.
        /// </summary>
        protected PortalLayer.State localState
        {
            get => _localState;
            set
            {
                if (_localState != value)
                {
                    if (localLayer)
                    {
                        if (currentClones.TryGetValue(localLayer, out CloneHandler handler))
                        {
                            if (originalLayerMode == PortalLayerMode.CollidersOnly)
                            {
                                foreach (PortalCloneInfo<Collider> info in handler.colliders)
                                    if (info.original) info.original.gameObject.layer = _localLayer.ConvertState(_localState, value, info.original.gameObject.layer);
                            }
                            else if (originalLayerMode == PortalLayerMode.AllGameObjects)
                            {
                                foreach (PortalCloneInfo<Transform> info in handler.transforms)
                                    if (info.original) info.original.gameObject.layer = _localLayer.ConvertState(_localState, value, info.original.gameObject.layer);
                            }
                        }
                        else
                        {
                            // TODO: Probably should handle a situation where there is no clone, but heck, I'm not even sure if this should control the local layer
                        }
                    }

                    _localState = value;
                }
            }
        }

        // The purpose of this is incase
        protected bool teleportOverride;

        protected virtual void Reset()
        {
            original = gameObject;
        }

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_maxCloneCount), nameof(maxCloneCount));
            Validate.FieldWithProperty(this, nameof(_originalLayerMode), nameof(originalLayerMode));
            Validate.FieldWithProperty(this, nameof(_cloneLayerMode), nameof(cloneLayerMode));
        }
        protected virtual void Awake()
        {
            _waitFixedUpdateLoop = WaitFixedUpdateLoop();
        }

        protected virtual void OnEnable()
        {
            transitionHandler.valueAdded += OnTriggerEnterTransition;
            transitionHandler.valueRemoved += OnTriggerExitTransition;
            layerHandler.valueAdded += OnTriggerEnterLayer;
            layerHandler.valueRemoved += OnTriggerExitLayer;
            StartCoroutine(_waitFixedUpdateLoop);

            PortalPhysics.lateFixedUpdate += LateFixedUpdate;

            PortalPhysics.AddPreTeleportListener(transform, OnPreTeleport);
            PortalPhysics.AddPostTeleportListener(transform, OnPostTeleport);

            GenerateClones();
        }

        protected virtual void OnDisable()
        {
            transitionHandler.valueAdded -= OnTriggerEnterTransition;
            transitionHandler.valueRemoved -= OnTriggerExitTransition;
            layerHandler.valueAdded -= OnTriggerEnterLayer;
            layerHandler.valueRemoved -= OnTriggerExitLayer;
            StopCoroutine(_waitFixedUpdateLoop);

            PortalPhysics.lateFixedUpdate -= LateFixedUpdate;

            PortalPhysics.RemovePreTeleportListener(transform, OnPreTeleport);
            PortalPhysics.RemovePostTeleportListener(transform, OnPostTeleport);

            ClearClones();
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

        protected virtual void OnDestroy()
        {
            _clonePool.Clear();
        }

        protected virtual void LateFixedUpdate()
        {
            GenerateClones();

            TryUpdateLocalLayer();

            UpdateCloneHandlers();
        }

        /// <summary>
        /// Tries to update the local layer based on the sorted transitions and layers.
        /// </summary>
        /// <returns>True if the local layer was updated, false otherwise.</returns>
        protected virtual bool TryUpdateLocalLayer()
        {
            if (teleportOverride)
                return false;

            foreach (Component component in sortedTransitionsAndLayers)
            {
                PortalLayer layer = component as PortalLayer;

                if (layer)
                {
                    if (localLayer != layer)
                    {
                        localLayer = layer;
                        UpdateLocalLayer();
                        return true;
                    }
                    else
                        return false;
                }
            }

            if (localLayer != null)
            {
                localLayer = null;
                UpdateLocalLayer();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the local layer state based on the current portal transition.
        /// </summary>
        protected virtual void UpdateLocalLayer()
        {
            if (localLayer)
            {
                if (localLayer.portalTransition && transitionHandler.HasValue(localLayer.portalTransition))
                    localState = PortalLayer.State.Inside;
                else
                    localState = PortalLayer.State.Between;
            }
            else localState = PortalLayer.State.Outside;
        }

        protected HashSet<Transform> _ignoreTransform = new HashSet<Transform>();

        /// <summary>
        /// Initializes a clone handler for a specific component.
        /// </summary>
        /// <param name="component">The component to create a clone for.</param>
        /// <param name="handler">The clone handler to initialize.</param>
        protected virtual void BeginCloneHandler(Component component, CloneHandler handler)
        {
            handler.original = original;

            if (handler.original)
            {
                if (!handler.clone)
                {
                    handler.portal = GetPortal(component);

                    Portal[] portalAsArray = new Portal[] { handler.portal };

                    if (template)
                    {
                        handler.clone = Instantiate(template);
                        PortalCloning.AddClones(handler.original, handler.clone, portalAsArray, handler.rigidbodies);
                        PortalCloning.AddClones(handler.original, handler.clone, portalAsArray, handler.colliders);
                    }
                    else
                    {
                        handler.clone = new GameObject();
                        PortalCloning.CreateClones(handler.original, handler.clone, portalAsArray, handler.rigidbodies);
                        PortalCloning.CreateClones(handler.original, handler.clone, portalAsArray, handler.colliders);

                        foreach (PortalCloneInfo<Rigidbody> info in handler.rigidbodies)
                            info.clone.gameObject.AddComponent<CloneCollisionEvents>();

                        foreach (PortalCloneInfo<Collider> info in handler.colliders)
                            if (!info.clone.attachedRigidbody) info.clone.gameObject.AddComponent<CloneCollisionEvents>();
                    }

                    PortalCloning.AddClones(handler.original, handler.clone, portalAsArray, handler.transforms);

                    handler.clone.transform.SetParent(original.transform.parent);

                    handler.clone.name = $"{original.name} (Physics Clone)";
                }
                else UpdateHandlerPortal(component, handler);
            }

            UpdateCloneHandlerNonPhysics(component, handler);
        }

        private static void UpdateHandlerPortal(Component component, CloneHandler handler)
        {
            if (component)
            {
                Portal portal = GetPortal(component);

                if (handler.portal != portal)
                {
                    handler.portal = portal;
                    Portal[] portalAsArray = new Portal[] { handler.portal };

                    PortalCloning.ReplacePortals(handler.rigidbodies, portalAsArray);
                    PortalCloning.ReplacePortals(handler.colliders, portalAsArray);
                    PortalCloning.ReplacePortals(handler.transforms, portalAsArray);
                }
            }
        }

        #region Update Clone

        /// <summary>
        /// Updates all current clone handlers.
        /// </summary>
        protected virtual void UpdateCloneHandlers()
        {
            foreach (var pair in currentClones)
                UpdateCloneHandler(pair.Key, pair.Value);
        }

        /// <summary>
        /// Updates a specific clone handler.
        /// </summary>
        /// <param name="component">The component associated with the clone.</param>
        /// <param name="handler">The clone handler to update.</param>
        protected virtual void UpdateCloneHandler(Component component, CloneHandler handler)
        {
            if (!handler.original || !handler.clone) return;

            if (component)
            {
                UpdateHandlerPortal(component, handler);

                _ignoreTransform.Clear();

                Rigidbody original, clone;

                // Update rigidbodies
                foreach (PortalCloneInfo<Rigidbody> info in handler.rigidbodies)
                {
                    original = info.original;
                    clone = info.clone;

                    clone.isKinematic = original.isKinematic;
                    clone.mass = original.mass;
                    clone.linearDamping = original.linearDamping;
                    clone.angularDamping = original.angularDamping;
                    clone.useGravity = original.useGravity;
                    clone.interpolation = original.interpolation;
                    clone.collisionDetectionMode = original.collisionDetectionMode;
                    clone.inertiaTensor = original.inertiaTensor;
                    clone.inertiaTensorRotation = original.inertiaTensorRotation;

                    if (!original.isKinematic)
                    {
                        _ignoreTransform.Add(original.transform);

                        Vector3 position = original.position, velocity = original.linearVelocity, angularVelocity = original.angularVelocity;
                        Quaternion rotation = original.rotation;

                        foreach (Portal portal in info.GetCloneToOriginalPortals())
                        {
                            if (portal && portal.usesTeleport)
                            {
                                portal.ModifyPoint(ref position);
                                portal.ModifyVector(ref velocity);
                                portal.ModifyVector(ref angularVelocity);
                                portal.ModifyRotation(ref rotation);
                            }
                        }

                        clone.MovePosition(position);
                        clone.MoveRotation(rotation);

                        clone.linearVelocity = velocity;
                        clone.angularVelocity = angularVelocity;
                    }
                }

                InnerUpdateCloneHandlerNonPhysics(component, handler);
            }
            else handler.clone.SetActive(false);
        }

        /// <summary>
        /// Updates non-physics components of a clone handler.
        /// </summary>
        /// <param name="component">The component associated with the clone.</param>
        /// <param name="handler">The clone handler to update.</param>
        protected virtual void UpdateCloneHandlerNonPhysics(Component component, CloneHandler handler)
        {
            if (!handler.original || !handler.clone) return;

            if (component)
            {
                _ignoreTransform.Clear();

                InnerUpdateCloneHandlerNonPhysics(component, handler);
            }
            else handler.clone.SetActive(false);
        }

        private void InnerUpdateCloneHandlerNonPhysics(Component component, CloneHandler handler)
        {
            Portal portal = null;
            PortalLayer layer = null;
            PortalLayer.State state = PortalLayer.State.Outside;

            if (component is PortalLayer)
            {
                layer = (PortalLayer)component;
                state = (layer.portalTransition && transitionHandler.HasValue(layer.portalTransition)) ? PortalLayer.State.Inside : PortalLayer.State.Between;

                portal = layer.portal;
            }
            else if (component is PortalTransition) portal = ((PortalTransition)component).portal;

            // Update transforms
            foreach (PortalCloneInfo<Transform> info in handler.transforms)
            {
                if (info)
                {
                    Transform originalTransform = info.original, cloneTransform = info.clone;

                    // Update the transform (unless this is a rigidbody)
                    if (!_ignoreTransform.Contains(originalTransform))
                    {
                        if (cloneTransform.gameObject == handler.clone)
                            PortalCloning.UpdateTransformWorld(info);
                        else
                            PortalCloning.UpdateTransformLocal(info);
                    }

                    GameObject originalGameObject = originalTransform.gameObject, cloneGameObject = cloneTransform.gameObject;

                    // Update the layer
                    if (cloneLayerMode == PortalLayerMode.AllGameObjects)
                        ConvertLayer(originalGameObject, cloneGameObject, portal, layer, state);
                    else
                        PortalCloning.UpdateLayer(info);

                    PortalCloning.UpdateTag(info);
                    PortalCloning.UpdateActiveAndEnabled(info);
                }
            }

            // Update colliders
            foreach (PortalCloneInfo<Collider> info in handler.colliders)
            {
                if (info)
                {
                    PortalCloning.UpdateCollider(info);

                    if (cloneLayerMode == PortalLayerMode.CollidersOnly)
                        ConvertLayer(info.original.gameObject, info.clone.gameObject, portal, layer, state);
                }
            }
        }

        /// <summary>
        /// Converts a layer from original to clone using portal layer settings.
        /// </summary>
        /// <param name="original">The original GameObject.</param>
        /// <param name="clone">The cloned GameObject.</param>
        /// <param name="portal">The portal used for the conversion.</param>
        /// <param name="layer">The portal layer to use for conversion.</param>
        /// <param name="state">The state of the portal layer.</param>
        protected virtual void ConvertLayer(GameObject original, GameObject clone, Portal portal, PortalLayer layer, PortalLayer.State state)
        {
            int newLayer;

            // Get the original layer
            if (localLayer)
                newLayer = localLayer.ConvertState(localState, PortalLayer.State.Outside, original.layer);
            else
                newLayer = original.layer;

            // Teleport the layer
            if (portal && portal.usesLayers) newLayer = portal.ModifyLayer(newLayer);
            else clone.layer = original.layer;

            // Apply the connected layer if needed
            if (layer && layer.connectedLayer)
                clone.layer = layer.connectedLayer.ConvertState(PortalLayer.State.Outside, state, newLayer);
            else
                clone.layer = newLayer;
        }

        /// <summary>
        /// Gets the portal associated with a component.
        /// </summary>
        /// <param name="component">The component to get the portal from.</param>
        /// <returns>The portal associated with the component, or null if none.</returns>
        private static Portal GetPortal(Component component)
        {
            if (component is PortalLayer) return ((PortalLayer)component).portal;

            if (component is PortalTransition) return ((PortalTransition)component).portal;

            return null;
        }

        #endregion

        #region Trigger Events

        /// <summary>
        /// Called when this enters a PortalLayer's the trigger.
        /// </summary>
        /// <param name="layer">The portal layer that entered.</param>
        protected virtual void OnTriggerEnterLayer(PortalLayer layer)
        {
            if (teleportOverride == layer) return;

            int index = sortedTransitionsAndLayers.FindIndex(i => i == layer.portalTransition);

            // Replace transition if it already exists
            if (index >= 0)
            {
                sortedTransitionsAndLayers[index] = layer;
                ReplaceClone(layer.portalTransition, layer);

                if (localLayer == layer)
                    TryUpdateLocalLayer();
            }
        }

        /// <summary>
        /// Called when this exits a PortalLayer's trigger.
        /// </summary>
        /// <param name="layer">The portal layer exited.</param>
        protected virtual void OnTriggerExitLayer(PortalLayer layer)
        {
            int index = sortedTransitionsAndLayers.FindIndex(i => i == layer);

            if (index >= 0)
            {
                if (currentClones.ContainsKey(layer))
                {
                    // Use the remainding portal transition if its available
                    if (layer.portalTransition && transitionHandler.HasValue(layer.portalTransition))
                    {
                        // Make sure no other layers use this transition
                        foreach (Component component in sortedTransitionsAndLayers)
                        {
                            if (component is PortalLayer other && other.portalTransition == layer.portalTransition)
                            {
                                layer = null;
                                break;
                            }
                        }

                        if (layer != null)
                        {
                            sortedTransitionsAndLayers[index] = layer.portalTransition;
                            ReplaceClone(layer, layer.portalTransition);
                        }
                    }
                }

                if (localLayer == layer)
                {
                    localLayer = null;
                    TryUpdateLocalLayer();
                }
            }

        }

        /// <summary>
        /// Called when this enters a PortalTransition's trigger.
        /// </summary>
        /// <param name="transition">The portal transition entered.</param>
        protected virtual void OnTriggerEnterTransition(PortalTransition transition)
        {
            if (teleportOverride == transition) return;

            foreach (Component component in sortedTransitionsAndLayers)
                if (component is PortalLayer layerX && layerX.portalTransition == transition) return;

            // TODO: Should this be updated here?
        }

        /// <summary>
        /// Called when this exits a PortalTransition's trigger.
        /// </summary>
        /// <param name="transition">The portal transition that exited.</param>
        protected virtual void OnTriggerExitTransition(PortalTransition transition) { }

        #endregion

        /// <summary>
        /// Generates clones based on the current transitions and layers.
        /// </summary>
        protected virtual void GenerateClones()
        {
            // Ignore this for one iteration
            if (teleportOverride)
            {
                teleportOverride = false;
                return;
            }

            // Remove all the transitions/layers that are no longer used
            foreach (PortalTransition transition in transitionHandler.Values)
                sortedTransitionsAndLayers.Remove(transition);

            foreach (PortalLayer layer in layerHandler.Values)
                sortedTransitionsAndLayers.Remove(layer);

            for (int i = 0; i < sortedTransitionsAndLayers.Count; i++)
                RemoveClone(sortedTransitionsAndLayers[i]);

            sortedTransitionsAndLayers.Clear();

            // Add back all the ones that are still in use
            foreach (PortalTransition transition in transitionHandler.Values)
                if (transition) sortedTransitionsAndLayers.Add(transition);

            int transitionCount = sortedTransitionsAndLayers.Count, index;

            foreach (PortalLayer layer in layerHandler.Values)
            {
                if (layer && layer.portalTransition)
                {
                    index = sortedTransitionsAndLayers.FindIndex(0, transitionCount, i => i == layer.portalTransition);

                    if (index >= 0)
                    {
                        sortedTransitionsAndLayers.RemoveAt(index);
                        transitionCount--;

                        RemoveClone(layer.portalTransition);
                    }
                }

                sortedTransitionsAndLayers.Add(layer);
            }

            sortedTransitionsAndLayers.Sort(SortTransitionsAndLayers);

            int maxClones = sortedTransitionsAndLayers.Count;
            if (maxCloneCount >= 0 && maxCloneCount < maxClones) maxClones = maxCloneCount;

            // Remove clones that are too high up
            for (int i = maxClones; i < sortedTransitionsAndLayers.Count; i++)
                RemoveClone(sortedTransitionsAndLayers[i]);

            // Will only add clone if required
            for (int i = 0; i < maxClones; i++)
                AddClone(sortedTransitionsAndLayers[i]);
        }

        private int SortTransitionsAndLayers(Component i, Component j)
            => GetScore(j).CompareTo(GetScore(i));

        /// <summary>
        /// Gets a score for sorting components, with lower scores prioritized.
        /// </summary>
        /// <param name="component">The component to score.</param>
        /// <returns>The score for the component (distance from original).</returns>
        protected virtual float GetScore(Component component)
            => component && original ? Vector3.Distance(component.transform.position, original.transform.position) : float.MaxValue;

        #region Clone Generation

        /// <summary>
        /// Clears all current clones.
        /// </summary>
        protected virtual void ClearClones()
        {
            foreach (PortalTransition transition in transitionHandler.Values)
                RemoveClone(transition);

            foreach (PortalLayer layer in layerHandler.Values)
                RemoveClone(layer);
        }

        /// <summary>
        /// Replaces one component's clone with another component's clone.
        /// </summary>
        /// <param name="original">The original component.</param>
        /// <param name="component">The new component.</param>
        /// <returns>True if the replacement was successful, false otherwise.</returns>
        protected virtual bool ReplaceClone(Component original, Component component)
        {
            if (!original || !component) return false;

            if (!currentClones.ContainsKey(component) && currentClones.TryGetValue(original, out CloneHandler handler))
            {
                if (original == _localLayer) localLayer = null;

                currentClones.Remove(original);
                currentClones[component] = handler;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds a clone for a component.
        /// </summary>
        /// <param name="component">The component to create a clone for.</param>
        /// <returns>True if a clone was added, false otherwise.</returns>
        protected virtual bool AddClone(Component component)
        {
            if (component && !currentClones.ContainsKey(component))
            {
                CloneHandler handler = _clonePool.Get();
                currentClones[component] = handler;
                BeginCloneHandler(component, handler);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes a clone for a component.
        /// </summary>
        /// <param name="component">The component whose clone should be removed.</param>
        /// <returns>True if a clone was removed, false otherwise.</returns>
        protected virtual bool RemoveClone(Component component)
        {
            if (component && currentClones.TryGetValue(component, out CloneHandler handler))
            {
                if (component == localLayer) localLayer = null;

                currentClones.Remove(component);
                _clonePool.Release(handler);

                return true;
            }

            return false;
        }

        #endregion

        /// <summary>
        /// Called before the object teleports through a portal.
        /// </summary>
        /// <param name="args">Teleportation arguments.</param>
        protected virtual void OnPreTeleport(Teleportation args) { }

        /// <summary>
        /// Called after the object teleports through a portal.
        /// </summary>
        /// <param name="args">Teleportation arguments.</param>
        protected virtual void OnPostTeleport(Teleportation args)
        {
            int i = 0;

            teleportOverride = true;

            while (i < sortedTransitionsAndLayers.Count)
            {
                Component component = sortedTransitionsAndLayers[i];

                if (component is PortalTransition transition)
                {
                    if (transition.portal == args.fromPortal)
                    {
                        sortedTransitionsAndLayers[i++] = transition.connectedTransition;
                        ReplaceClone(transition, transition.connectedTransition);

                        localState = PortalLayer.State.Outside;
                        continue;
                    }
                }
                else if (component is PortalLayer layer)
                {
                    if (layer.portal == args.fromPortal)
                    {
                        sortedTransitionsAndLayers[i++] = layer.connectedLayer;
                        ReplaceClone(layer, layer.connectedLayer);

                        localLayer = layer.connectedLayer;
                        localState = transitionHandler.HasValue(layer.portalTransition) ? PortalLayer.State.Inside : PortalLayer.State.Between;
                        continue;
                    }
                }

                RemoveClone(component);
                sortedTransitionsAndLayers.RemoveAt(i);
            }

            UpdateCloneHandlers();
        }
    }
}