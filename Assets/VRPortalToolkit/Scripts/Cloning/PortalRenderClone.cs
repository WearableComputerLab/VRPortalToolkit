using Misc.Physics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Misc.EditorHelpers;
using VRPortalToolkit.Physics;
using VRPortalToolkit.Cloning;
using Misc;

namespace VRPortalToolkit
{
    // TODO: Can still accidently make one more clone than it needs when teleported
    // This happens because you enter a transition, before you leave another one

    public class PortalRenderClone : Misc.Physics.TriggerHandler
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

                    clonePool.Clear();

                    foreach (var pair in currentClones)
                        BeginCloneHandler(pair.Key, pair.Value);
                }
            }
        }

        [SerializeField] private GameObject _template;
        public virtual GameObject template { get => _template; set => _template = value; }

        [SerializeField] private int _maxCloneCount = -1;
        public virtual int maxCloneCount
        {
            get => _maxCloneCount;
            set
            {
                if (_maxCloneCount != value)
                {
                    Validate.UpdateField(this, nameof(_maxCloneCount), _maxCloneCount = value);

                    if (isActiveAndEnabled && Application.isPlaying)
                    {
                        if (_maxCloneCount < 0)
                        {
                            foreach (PortalTransition transition in transitionHandler.GetComponents())
                                AddClone(transition);
                        }

                        GenerateClones();
                    }
                }
            }
        }

        protected class CloneHandler
        {
            public GameObject original;
            public GameObject clone;
            public Portal portal;

            public List<PortalCloneInfo<Transform>> transforms = new List<PortalCloneInfo<Transform>>();
            public List<PortalCloneInfo<Renderer>> renderers = new List<PortalCloneInfo<Renderer>>();
            public List<PortalCloneInfo<MeshFilter>> filters = new List<PortalCloneInfo<MeshFilter>>();
        }

        /*[Header("Transition Events")]
        public SerializableEvent<PortalTransition> enteredTransition = new SerializableEvent<PortalTransition>();
        public SerializableEvent<PortalTransition> exitedTransition = new SerializableEvent<PortalTransition>();*/

        protected List<PortalTransition> sortedTransitions = new List<PortalTransition>();
        protected Dictionary<PortalTransition, CloneHandler> currentClones = new Dictionary<PortalTransition, CloneHandler>();
        protected ObjectPool<CloneHandler> clonePool = new ObjectPool<CloneHandler>(() => new CloneHandler(), null,
                i => i.clone?.SetActive(false), i => { if (i.clone) Destroy(i.clone); }, 0);

        protected ComponentHandler<Transform, PortalTransition> transitionHandler = new ComponentHandler<Transform, PortalTransition>();

        protected bool teleportOverride;

        protected virtual void Reset()
        {
            original = gameObject;
        }

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_maxCloneCount), nameof(maxCloneCount));
            Validate.FieldWithProperty(this, nameof(_original), nameof(original));
        }

        protected virtual void Awake()
        {
            transitionHandler.componentEntered = OnTriggerEnterTransition;
            transitionHandler.componentExited = OnTriggerExitTransition;
            transitionHandler.getComponentsMode = GetComponentsMode.GetComponents;
            transitionHandler.exitOnComponentDisabled = transitionHandler.exitOnSourceDestroyed = false;
            transitionHandler.enabled = true;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            PortalPhysics.AddPreTeleportListener(transform, OnPreTeleport);
            PortalPhysics.AddPostTeleportListener(transform, OnPostTeleport);

            GenerateClones();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            PortalPhysics.RemovePreTeleportListener(transform, OnPreTeleport);
            PortalPhysics.RemovePostTeleportListener(transform, OnPostTeleport);

            ClearClones();
        }

        protected virtual void OnDestroy()
        {
            clonePool.Clear();
        }

        protected virtual void LateUpdate()
        {
            Apply();
        }

        public virtual void Apply()
        {
            GenerateClones();

            foreach (var pair in currentClones)
                UpdateCloneHandler(pair.Key, pair.Value);
        }

        protected override void OnTriggerEnterContainer(Transform other)
            => transitionHandler.EnterSource(other);

        protected virtual void OnTriggerEnterTransition(PortalTransition transition) { }

        protected override void OnTriggerExitContainer(Transform other)
            => transitionHandler.ExitSource(other);

        protected virtual void OnTriggerExitTransition(PortalTransition transition) { }

        protected virtual void GenerateClones()
        {
            if (teleportOverride) return;

            // Remove all the transitions that are no longer used
            foreach (PortalTransition transition in transitionHandler.GetComponents())
                sortedTransitions.Remove(transition);

            for (int i = 0; i < sortedTransitions.Count; i++)
                RemoveClone(sortedTransitions[i]);

            sortedTransitions.Clear();

            // Add back all the ones that are still in use
            foreach (PortalTransition transition in transitionHandler.GetComponents())
                if (transition) sortedTransitions.Add(transition);

            sortedTransitions.Sort(SortTransitions);

            int maxClones = sortedTransitions.Count;
            if (maxCloneCount >= 0 && maxCloneCount < maxClones) maxClones = maxCloneCount;

            // Remove clones that are too high up
            for (int i = maxClones; i < sortedTransitions.Count; i++)
                RemoveClone(sortedTransitions[i]);

            // TODO: Will only add clone if required
            for (int i = 0; i < maxClones; i++)
                AddClone(sortedTransitions[i]);
        }

        private int SortTransitions(PortalTransition i, PortalTransition j)
            => GetScore(j).CompareTo(GetScore(i));

        protected virtual float GetScore(PortalTransition transition)
            => transition && original ? Vector3.Distance(transition.transform.position, original.transform.position) : float.MaxValue;

        protected virtual void ClearClones()
        {
            foreach (PortalTransition transition in transitionHandler.GetComponents())
                RemoveClone(transition);
        }

        protected virtual bool ReplaceClone(PortalTransition original, PortalTransition replacement)
        {
            if (!original || !replacement) return false;

            if (!currentClones.ContainsKey(replacement) && currentClones.TryGetValue(original, out CloneHandler handler))
            {
                currentClones.Remove(original);
                currentClones[replacement] = handler;

                return true;
            }

            return false;
        }

        protected virtual bool AddClone(PortalTransition transition)
        {
            if (!currentClones.ContainsKey(transition))
            {
                CloneHandler handler = clonePool.Get();
                currentClones[transition] = handler;
                BeginCloneHandler(transition, handler);

                return true;
            }

            return false;
        }

        protected virtual bool RemoveClone(PortalTransition transition)
        {
            if (transition != null && currentClones.TryGetValue(transition, out CloneHandler handler))
            {
                clonePool.Release(handler);
                currentClones.Remove(transition);
                return true;
            }

            return false;
        }

        protected virtual void BeginCloneHandler(PortalTransition transition, CloneHandler handler)
        {
            handler.original = original;

            if (handler.original)
            {
                if (!handler.clone)
                {
                    handler.portal = transition.portal;

                    Portal[] portalAsArray = new Portal[] { handler.portal };

                    if (template)
                    {
                        handler.clone = Instantiate(template);
                        PortalCloning.AddClones(handler.original, handler.clone, portalAsArray, handler.renderers);
                        PortalCloning.AddClones(handler.original, handler.clone, portalAsArray, handler.filters);
                    }
                    else
                    {
                        handler.clone = new GameObject();
                        PortalCloning.CreateClones(handler.original, handler.clone, portalAsArray, handler.renderers);
                        PortalCloning.CreateClones(handler.original, handler.clone, portalAsArray, handler.filters);
                    }

                    PortalCloning.AddClones(handler.original, handler.clone, portalAsArray, handler.transforms);

                    handler.clone.transform.SetParent(original.transform.parent);

                    handler.clone.name = $"{original.name} (Render Clone)";
                }
                else UpdateHandlerPortal(transition, handler);

                if (handler.clone)
                    handler.clone.SetActive(original.activeSelf);
            }
        }

        private static void UpdateHandlerPortal(PortalTransition transition, CloneHandler handler)
        {
            if (transition && handler.portal != transition.portal)
            {
                handler.portal = transition.portal;
                Portal[] portalAsArray = new Portal[] { handler.portal };

                PortalCloning.ReplacePortals(handler.renderers, portalAsArray);
                PortalCloning.ReplacePortals(handler.filters, portalAsArray);
                PortalCloning.ReplacePortals(handler.transforms, portalAsArray);
            }
        }

        protected virtual void UpdateCloneHandler(PortalTransition transition, CloneHandler handler)
        {
            if (!handler.original || !handler.clone) return;

            if (transition)
            {
                UpdateHandlerPortal(transition, handler);

                foreach (PortalCloneInfo<Transform> info in handler.transforms)
                {
                    PortalCloning.UpdateActiveAndEnabled(info);
                    PortalCloning.UpdateLayer(info);
                    PortalCloning.UpdateTag(info);

                    if (handler.clone.transform == info.clone)
                        PortalCloning.UpdateTransformWorld(info);
                    else
                        PortalCloning.UpdateTransformLocal(info);
                }

                foreach (PortalCloneInfo<Renderer> info in handler.renderers)
                    PortalCloning.UpdateRenderer(info);

                foreach (PortalCloneInfo<MeshFilter> info in handler.filters)
                    PortalCloning.UpdateMeshFilter(info);
            }
            else handler.clone.SetActive(false);
        }

        private static WaitForFixedUpdate waitForFixed = new WaitForFixedUpdate();

        protected virtual void OnPreTeleport(Teleportation args) { }

        protected virtual void OnPostTeleport(Teleportation args)
        {
            int i = 0;

            while (i < sortedTransitions.Count)
            {
                PortalTransition transition = sortedTransitions[i];

                if (transition.portal == args.fromPortal)
                {
                    sortedTransitions[i++] = transition.connectedTransition;
                    ReplaceClone(transition, transition.connectedTransition);

                    continue;
                }

                RemoveClone(transition);
                sortedTransitions.RemoveAt(i);
            }

            teleportOverride = true;
            Apply();
            StartCoroutine(DisableOverrideAfterFixedUpdate());
        }

        protected virtual IEnumerator DisableOverrideAfterFixedUpdate()
        {
            yield return waitForFixed;

            teleportOverride = false;
        }
    }
}
