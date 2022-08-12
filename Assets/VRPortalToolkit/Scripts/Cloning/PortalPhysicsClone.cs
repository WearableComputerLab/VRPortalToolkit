using System;
using Misc.Physics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRPortalToolkit.Physics;
using Misc.EditorHelpers;
using VRPortalToolkit.Cloning;
using Misc;

namespace VRPortalToolkit
{
    // TODO: get rid of local layer
    // I dont know how, I just know I dont like it (and right now its broken)

    public class PortalPhysicsClone : Misc.Physics.TriggerHandler
    {
        [SerializeField] private GameObject _original;
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
        public void ClearOriginal() => original = null;

        [SerializeField] private GameObject _template;
        public virtual GameObject template { get => _template; set => _template = value; }
        public void ClearTemplate() => template = null;

        [SerializeField] private int _maxCloneCount = -1;
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

        [SerializeField] private PortalLayerMode _originalLayerMode = PortalLayerMode.CollidersOnly;
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

        [SerializeField] private PortalLayerMode _cloneLayerMode = PortalLayerMode.CollidersOnly;
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

        public enum PortalLayerMode
        {
            Ignore = 0,
            CollidersOnly = 1,
            AllGameObjects = 2,
        }

        protected class CloneHandler
        {
            public GameObject original;
            public GameObject clone;
            public Portal portal;

            public List<PortalCloneInfo<Transform>> transforms = new List<PortalCloneInfo<Transform>>();
            public List<PortalCloneInfo<Rigidbody>> rigidbodies = new List<PortalCloneInfo<Rigidbody>>();
            public List<PortalCloneInfo<Collider>> colliders = new List<PortalCloneInfo<Collider>>();
        }

        protected List<Component> sortedTransitionsAndLayers = new List<Component>();
        protected Dictionary<Component, CloneHandler> currentClones = new Dictionary<Component, CloneHandler>();
        protected ObjectPool<CloneHandler> _clonePool = new ObjectPool<CloneHandler>(() => new CloneHandler(), null,
                i => i.clone?.SetActive(false), i => { if (i.clone) Destroy(i.clone); });

        protected ComponentHandler<Transform, PortalTransition> transitionHandler = new ComponentHandler<Transform, PortalTransition>();
        protected ComponentHandler<Transform, PortalLayer> layerHandler = new ComponentHandler<Transform, PortalLayer>();

        private PortalLayer _localLayer;
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
            transitionHandler.componentEntered = OnTriggerEnterTransition;
            transitionHandler.componentExited = OnTriggerExitTransition;
            transitionHandler.getComponentsMode = GetComponentsMode.GetComponents;
            transitionHandler.exitOnComponentDisabled = transitionHandler.exitOnSourceDestroyed = false;
            transitionHandler.enabled = true;

            layerHandler.componentEntered = OnTriggerEnterLayer;
            layerHandler.componentExited = OnTriggerExitLayer;
            layerHandler.getComponentsMode = GetComponentsMode.GetComponents;
            layerHandler.exitOnComponentDisabled = transitionHandler.exitOnSourceDestroyed = false;
            layerHandler.enabled = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            PortalPhysics.lateFixedUpdate += LateFixedUpdate;

            PortalPhysics.AddPreTeleportListener(transform, OnPreTeleport);
            PortalPhysics.AddPostTeleportListener(transform, OnPostTeleport);

            GenerateClones();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            PortalPhysics.lateFixedUpdate -= LateFixedUpdate;

            PortalPhysics.RemovePreTeleportListener(transform, OnPreTeleport);
            PortalPhysics.RemovePostTeleportListener(transform, OnPostTeleport);

            ClearClones();
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

        protected virtual void UpdateLocalLayer()
        {
            if (localLayer)
            {
                if (localLayer.portalTransition && transitionHandler.HasComponent(localLayer.portalTransition))
                    localState = PortalLayer.State.Inside;
                else
                    localState = PortalLayer.State.Between;
            }
            else localState = PortalLayer.State.Outside;
        }

        protected HashSet<Transform> _ignoreTransform = new HashSet<Transform>();

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

        protected virtual void UpdateCloneHandlers()
        {
            foreach (var pair in currentClones)
                UpdateCloneHandler(pair.Key, pair.Value);
        }

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
                    clone.drag = original.drag;
                    clone.angularDrag = original.angularDrag;
                    clone.useGravity = original.useGravity;
                    clone.interpolation = original.interpolation;
                    clone.collisionDetectionMode = original.collisionDetectionMode;
                    clone.inertiaTensor = original.inertiaTensor;
                    clone.inertiaTensorRotation = original.inertiaTensorRotation;

                    if (!original.isKinematic)
                    {
                        _ignoreTransform.Add(original.transform);

                        Vector3 position = original.position, velocity = original.velocity, angularVelocity = original.angularVelocity;
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

                        clone.velocity = velocity;
                        clone.angularVelocity = angularVelocity;
                    }
                }

                InnerUpdateCloneHandlerNonPhysics(component, handler);
            }
            else handler.clone.SetActive(false);
        }

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
                state = (layer.portalTransition && transitionHandler.HasComponent(layer.portalTransition)) ? PortalLayer.State.Inside : PortalLayer.State.Between;

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

        private static Portal GetPortal(Component component)
        {
            if (component is PortalLayer) return ((PortalLayer)component).portal;

            if (component is PortalTransition) return ((PortalTransition)component).portal;

            return null;
        }

        #endregion

        #region Trigger Events

        protected override void OnTriggerEnterContainer(Transform container)
        {
            transitionHandler.EnterSource(container);
            layerHandler.EnterSource(container);
        }

        protected override void OnTriggerExitContainer(Transform container)
        {
            transitionHandler.ExitSource(container);
            layerHandler.ExitSource(container);
        }

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

        protected virtual void OnTriggerExitLayer(PortalLayer layer)
        {
            int index = sortedTransitionsAndLayers.FindIndex(i => i == layer);

            if (index >= 0)
            {
                if (currentClones.ContainsKey(layer))
                {
                    // Use the remainding portal transition if its available
                    if (layer.portalTransition && transitionHandler.HasComponent(layer.portalTransition))
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

        protected virtual void OnTriggerEnterTransition(PortalTransition transition)
        {
            if (teleportOverride == transition) return;

            foreach (Component component in sortedTransitionsAndLayers)
                if (component is PortalLayer layerX && layerX.portalTransition == transition) return;

            // TODO: Should this be updated here?
        }

        protected virtual void OnTriggerExitTransition(PortalTransition transition) { }

        #endregion

        protected virtual void GenerateClones()
        {
            // Ignore this for one iteration
            if (teleportOverride)
            {
                teleportOverride = false;
                return;
            }

            // Remove all the transitions/layers that are no longer used
            foreach (PortalTransition transition in transitionHandler.GetComponents())
                sortedTransitionsAndLayers.Remove(transition);

            foreach (PortalLayer layer in layerHandler.GetComponents())
                sortedTransitionsAndLayers.Remove(layer);

            for (int i = 0; i < sortedTransitionsAndLayers.Count; i++)
                RemoveClone(sortedTransitionsAndLayers[i]);

            sortedTransitionsAndLayers.Clear();

            // Add back all the ones that are still in use
            foreach (PortalTransition transition in transitionHandler.GetComponents())
                if (transition) sortedTransitionsAndLayers.Add(transition);

            int transitionCount = sortedTransitionsAndLayers.Count, index;

            foreach (PortalLayer layer in layerHandler.GetComponents())
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

        protected virtual float GetScore(Component component)
            => component && original ? Vector3.Distance(component.transform.position, original.transform.position) : float.MaxValue;

        #region Clone Generation

        protected virtual void ClearClones()
        {
            foreach (PortalTransition transition in transitionHandler.GetComponents())
                RemoveClone(transition);

            foreach (PortalLayer layer in layerHandler.GetComponents())
                RemoveClone(layer);
        }

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

        protected virtual void OnPreTeleport(Teleportation args) { }

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
                        localState = transitionHandler.HasComponent(layer.portalTransition) ? PortalLayer.State.Inside : PortalLayer.State.Between;
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