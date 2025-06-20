using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Misc.EditorHelpers;
using VRPortalToolkit.Physics;
using VRPortalToolkit.Cloning;
using Misc;

namespace VRPortalToolkit
{
    /// <summary>
    /// Creates and manages render clones for objects that are visible through portals.
    /// These clones are created when the object triggers a PortalTransition.
    /// </summary>
    [DefaultExecutionOrder(1030)]
    public class PortalRenderClone : MonoBehaviour
    {
        private static readonly WaitForFixedUpdate _WaitForFixedUpdate = new WaitForFixedUpdate();

        /// <summary>
        /// The original GameObject to be cloned and rendered through portals.
        /// </summary>
        [Tooltip("The original GameObject to be cloned and rendered through portals")]
        [SerializeField] private GameObject _original;
        
        /// <summary>
        /// Gets or sets the original GameObject to be cloned.
        /// </summary>
        public GameObject original
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

        [Tooltip("The template GameObject used for cloning. If not set, a new GameObject will be created with only the necessary components")]
        [SerializeField] private GameObject _template;
        /// <summary>
        /// The template GameObject used for cloning. If not set, a new GameObject will be created with only the necessary components.
        /// </summary>
        public GameObject template { get => _template; set => _template = value; }

        [Tooltip("The maximum number of clones allowed. Set to -1 for unlimited")]
        [SerializeField] private int _maxCloneCount = -1;
        /// <summary>
        /// The maximum number of clones allowed. Set to -1 for unlimited.
        /// </summary>
        public int maxCloneCount
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
                            foreach (PortalTransition transition in triggerHandler.Values)
                                AddClone(transition);
                        }

                        GenerateClones();
                    }
                }
            }
        }

        /// <summary>
        /// Stores information about a clone and its associated components.
        /// </summary>
        protected class CloneHandler
        {
            public GameObject original;
            public GameObject clone;
            public Portal portal;

            public List<PortalCloneInfo<Transform>> transforms = new List<PortalCloneInfo<Transform>>();
            public List<PortalCloneInfo<Renderer>> renderers = new List<PortalCloneInfo<Renderer>>();
            public List<PortalCloneInfo<MeshFilter>> filters = new List<PortalCloneInfo<MeshFilter>>();
        }

        protected readonly List<PortalTransition> sortedTransitions = new List<PortalTransition>();
        protected readonly Dictionary<PortalTransition, CloneHandler> currentClones = new Dictionary<PortalTransition, CloneHandler>();
        protected readonly ObjectPool<CloneHandler> clonePool = new ObjectPool<CloneHandler>(() => new CloneHandler(), null,
                i => i.clone?.SetActive(false), i => { if (i.clone) Destroy(i.clone); }, 0);

        protected readonly TriggerHandler<PortalTransition> triggerHandler = new TriggerHandler<PortalTransition>();
        protected readonly HashSet<Collider> _stayedColliders = new HashSet<Collider>();
        private IEnumerator _waitFixedUpdateLoop;

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
            _waitFixedUpdateLoop = WaitFixedUpdateLoop();
        }

        protected virtual void OnEnable()
        {
            triggerHandler.valueAdded += OnTriggerEnterTransition;
            triggerHandler.valueRemoved += OnTriggerExitTransition;
            StartCoroutine(_waitFixedUpdateLoop);

            PortalPhysics.AddPreTeleportListener(transform, OnPreTeleport);
            PortalPhysics.AddPostTeleportListener(transform, OnPostTeleport);

            GenerateClones();
        }

        protected virtual void OnDisable()
        {
            triggerHandler.valueAdded -= OnTriggerEnterTransition;
            triggerHandler.valueRemoved -= OnTriggerExitTransition;
            StopCoroutine(_waitFixedUpdateLoop);

            PortalPhysics.RemovePreTeleportListener(transform, OnPreTeleport);
            PortalPhysics.RemovePostTeleportListener(transform, OnPostTeleport);

            ClearClones();
        }

        protected virtual void LateUpdate()
        {
            Apply();
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

        protected virtual void OnDestroy()
        {
            clonePool.Clear();
        }

        /// <summary>
        /// Updates all clones to match their original objects.
        /// </summary>
        public virtual void Apply()
        {
            GenerateClones();

            foreach (var pair in currentClones)
                UpdateCloneHandler(pair.Key, pair.Value);
        }

        /// <summary>
        /// Called when a portal transition is entered.
        /// </summary>
        /// <param name="transition">The portal transition that was entered.</param>
        protected virtual void OnTriggerEnterTransition(PortalTransition transition) { }

        /// <summary>
        /// Called when a portal transition is exited.
        /// </summary>
        /// <param name="transition">The portal transition that was exited.</param>
        protected virtual void OnTriggerExitTransition(PortalTransition transition) { }

        /// <summary>
        /// Generates clones based on active portal transitions.
        /// </summary>
        protected virtual void GenerateClones()
        {
            if (teleportOverride) return;

            // Remove all the transitions that are no longer used
            foreach (PortalTransition transition in triggerHandler.Values)
                sortedTransitions.Remove(transition);

            for (int i = 0; i < sortedTransitions.Count; i++)
                RemoveClone(sortedTransitions[i]);

            sortedTransitions.Clear();

            // Add back all the ones that are still in use
            foreach (PortalTransition transition in triggerHandler.Values)
                if (transition) sortedTransitions.Add(transition);

            sortedTransitions.Sort(SortTransitions);

            int maxClones = sortedTransitions.Count;
            if (maxCloneCount >= 0 && maxCloneCount < maxClones) maxClones = maxCloneCount;

            // Remove clones that are too high up
            for (int i = maxClones; i < sortedTransitions.Count; i++)
                RemoveClone(sortedTransitions[i]);

            // Will only add clone if required
            for (int i = 0; i < maxClones; i++)
                AddClone(sortedTransitions[i]);
        }

        private int SortTransitions(PortalTransition i, PortalTransition j)
            => GetScore(j).CompareTo(GetScore(i));

        /// <summary>
        /// Calculates a score for a portal transition based on distance to the original object.
        /// Used for sorting transitions by priority.
        /// </summary>
        /// <param name="transition">The transition to score.</param>
        /// <returns>The score value (lower scores have higher priority).</returns>
        protected virtual float GetScore(PortalTransition transition)
            => transition && original ? Vector3.Distance(transition.transform.position, original.transform.position) : float.MaxValue;

        /// <summary>
        /// Clears all active clones.
        /// </summary>
        protected virtual void ClearClones()
        {
            foreach (PortalTransition transition in triggerHandler.Values)
                RemoveClone(transition);
        }

        /// <summary>
        /// Replaces a clone associated with one transition with a clone for another transition.
        /// </summary>
        /// <param name="original">The original transition whose clone should be replaced.</param>
        /// <param name="replacement">The replacement transition to associate with the clone.</param>
        /// <returns>True if the replacement was successful, false otherwise.</returns>
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

        /// <summary>
        /// Adds a clone for a specific portal transition.
        /// </summary>
        /// <param name="transition">The portal transition to create a clone for.</param>
        /// <returns>True if the clone was added, false otherwise.</returns>
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

        /// <summary>
        /// Removes a clone associated with a specific portal transition.
        /// </summary>
        /// <param name="transition">The portal transition whose clone should be removed.</param>
        /// <returns>True if the clone was removed, false otherwise.</returns>
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

        /// <summary>
        /// Initializes a clone handler for a specific portal transition.
        /// </summary>
        /// <param name="transition">The portal transition to create a clone for.</param>
        /// <param name="handler">The clone handler to initialize.</param>
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

                        // Handle unique skinned renderer issue
                        foreach (PortalCloneInfo<Renderer> cloneInfo in handler.renderers)
                        {
                            if (cloneInfo.TryAs(out PortalCloneInfo<SkinnedMeshRenderer> skinnedCloneInfo))
                                skinnedCloneInfo.CloneBones();
                        }
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

        private static void FindCloneTransforms(Transform original, Transform clone, Dictionary<Transform, Transform> cloneByOriginal)
        {
            cloneByOriginal.Add(original, clone);

            int childCount = Mathf.Min(original.childCount, clone.childCount);

            for (int i = 0; i < childCount; i++)
                FindCloneTransforms(original.GetChild(i), clone.GetChild(i), cloneByOriginal);
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

        /// <summary>
        /// Updates a clone handler to match the current state of the original object.
        /// </summary>
        /// <param name="transition">The portal transition associated with the clone.</param>
        /// <param name="handler">The clone handler to update.</param>
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

        /// <summary>
        /// Called before the object teleports through a portal.
        /// </summary>
        /// <param name="args">The teleportation data.</param>
        protected virtual void OnPreTeleport(Teleportation args) { }

        /// <summary>
        /// Called after the object teleports through a portal.
        /// </summary>
        /// <param name="args">The teleportation data.</param>
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

        /// <summary>
        /// Disables the teleport override flag after the next fixed update.
        /// </summary>
        /// <returns>Coroutine enumerator.</returns>
        protected virtual IEnumerator DisableOverrideAfterFixedUpdate()
        {
            yield return _WaitForFixedUpdate;

            teleportOverride = false;
        }
    }
}
