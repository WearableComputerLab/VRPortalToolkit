using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Cloning;

namespace VRPortalToolkit.XRI
{
    /// <summary>
    /// XR direct interactor that supports portal-aware interactions and trigger handling.
    /// </summary>
    public class XRPortableDirectInteractor : XRDirectInteractor, IXRPortableInteractor, ICloneTriggerEnterHandler, ICloneTriggerStayHandler, ICloneTriggerExitHandler
    {
        private static readonly WaitForFixedUpdate _WaitForFixedUpdate = new WaitForFixedUpdate();

        protected readonly struct TriggerKey
        {
            public readonly Transform source;
            public readonly Collider collider;

            public TriggerKey(Transform source, Collider collider)
            {
                this.source = source;
                this.collider = collider;
            }
        }

        protected readonly struct TriggerInfo
        {
            public readonly Transform source;
            public readonly Collider collider;
            public readonly float distance;


            public TriggerInfo(Transform source, Collider collider, float distance)
            {
                this.source = source;
                this.collider = collider;
                this.distance = distance;
            }
        }

        protected readonly struct InteractableComparer : IComparer<IXRInteractable>
        {
            public readonly Dictionary<IXRInteractable, TriggerInfo> interactableToTrigger;

            public InteractableComparer(Dictionary<IXRInteractable, TriggerInfo> interactableToTrigger)
            {
                this.interactableToTrigger = interactableToTrigger;
            }

            public int Compare(IXRInteractable x, IXRInteractable y) =>
                interactableToTrigger[x].distance.CompareTo(interactableToTrigger[y].distance);
        }

        /// <summary>
        /// Handler for trigger events, mapping trigger keys to interactables.
        /// </summary>
        protected readonly TriggerHandler<TriggerKey, IXRInteractable> triggerHandler = new TriggerHandler<TriggerKey, IXRInteractable>();
        /// <summary>
        /// Set of colliders that stayed in the trigger during the last update.
        /// </summary>
        protected readonly HashSet<TriggerKey> _stayedColliders = new HashSet<TriggerKey>();
        private IEnumerator _waitFixedUpdateLoop;

        /// <summary>
        /// Mapping of interactables to their associated trigger information.
        /// </summary>
        protected readonly Dictionary<IXRInteractable, TriggerInfo> interactableToTrigger = new Dictionary<IXRInteractable, TriggerInfo>();

        protected override void Awake()
        {
            base.Awake();
            _waitFixedUpdateLoop = WaitFixedUpdateLoop();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            triggerHandler.valueAdded += OnTriggerEnterInteractable;
            triggerHandler.valueRemoved += OnTriggerExitInteractable;
            StartCoroutine(_waitFixedUpdateLoop);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            triggerHandler.valueAdded -= OnTriggerEnterInteractable;
            triggerHandler.valueRemoved -= OnTriggerExitInteractable;
            StopCoroutine(_waitFixedUpdateLoop);
        }

        private IEnumerator WaitFixedUpdateLoop()
        {
            while (true)
            {
                yield return _WaitForFixedUpdate;

                triggerHandler.UpdateKeys(_stayedColliders);
                _stayedColliders.Clear();
            }
        }

        /// <summary>
        /// Gets the portals needed to travel to the specified interactable.
        /// </summary>
        /// <param name="interactable">The XR interactable.</param>
        /// <returns>An enumerable of portals.</returns>
        public virtual IEnumerable<Portal> GetPortalsToInteractable(IXRInteractable interactable)
        {
            if (interactableToTrigger.TryGetValue(interactable, out TriggerInfo info))
            {
                foreach (Portal portal in GetPortals(info.source, info.collider))
                    yield return portal;
            }

            yield break;
        }

        /// <summary>
        /// Gets the portals needed to travel from the source to the collider.
        /// <summary/>
        protected IEnumerable<Portal> GetPortals(Transform source, Collider collider)
        {
            IEnumerable<Portal> from = null, to = null;

            if (PortalCloning.TryGetCloneInfo(source, out var info))
            {
                from = info.GetOriginalToClonePortals();
            }

            if (PortalCloning.TryGetCloneInfo(collider.transform, out info))
            {
                to = info.GetCloneToOriginalPortals();
            }
            
            return from.Difference(to);
        }

        /// <inheritdoc/>
        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            targets.Clear();

            if (!isActiveAndEnabled)
                return;

            var filter = targetFilter;
            if (filter != null && filter.canProcess)
                filter.Process(this, unsortedValidTargets, targets);
            else
            {
                interactableToTrigger.Clear();

                foreach (var pair in triggerHandler)
                {
                    float distanceSqr = GetPortals(pair.Key.source, pair.Key.collider).DistanceSqr(transform.position, pair.Value.transform.position);

                    if (!interactableToTrigger.TryGetValue(pair.Value, out TriggerInfo info))
                    {
                        targets.Add(pair.Value);
                        interactableToTrigger[pair.Value] = new TriggerInfo(pair.Key.source, pair.Key.collider, distanceSqr);
                    }
                    else if (distanceSqr < info.distance)
                        interactableToTrigger[pair.Value] = new TriggerInfo(pair.Key.source, pair.Key.collider, distanceSqr);
                }

                targets.Sort(new InteractableComparer(interactableToTrigger));
            }
        }

        protected virtual new void OnTriggerEnter(Collider other) =>
            OnCloneTriggerEnter(transform, other);

        protected virtual new void OnTriggerStay(Collider other) =>
            OnCloneTriggerStay(transform, other);

        protected virtual new void OnTriggerExit(Collider other) =>
            OnCloneTriggerExit(transform, other);

        /// <summary>
        /// Handles the event when a clone enters a trigger.
        /// </summary>
        /// <param name="clone">The clone transform.</param>
        /// <param name="other">The collider that entered the trigger.</param>
        public virtual void OnCloneTriggerEnter(Transform clone, Collider other)
        {
            AddInteractable(new TriggerKey(clone, other));
        }

        /// <summary>
        /// Handles the event when a clone stays in a trigger.
        /// </summary>
        /// <param name="clone">The clone transform.</param>
        /// <param name="other">The collider that stayed in the trigger.</param>
        public virtual void OnCloneTriggerStay(Transform clone, Collider other)
        {
            TriggerKey key = new TriggerKey(clone, other);

            if (!triggerHandler.HasKey(key))
                AddInteractable(key);

            _stayedColliders.Add(key);
        }

        private void AddInteractable(TriggerKey key)
        {
            if (interactionManager.TryGetInteractableForCollider(PortalCloning.GetOriginal(key.collider), out IXRInteractable interactable))
                triggerHandler.Add(key, interactable);
        }

        /// <summary>
        /// Handles the event when a clone exits a trigger.
        /// </summary>
        /// <param name="clone">The clone transform.</param>
        /// <param name="other">The collider that exited the trigger.</param>
        public virtual void OnCloneTriggerExit(Transform clone, Collider other)
        {
            triggerHandler.RemoveKey(new TriggerKey(clone, other));
        }

        /// <summary>
        /// Called when an interactable enters a trigger.
        /// </summary>
        /// <param name="interactable">The interactable that entered the trigger.</param>
        protected virtual void OnTriggerEnterInteractable(IXRInteractable interactable)
        {

        }

        /// <summary>
        /// Called when an interactable exits a trigger.
        /// </summary>
        /// <param name="interactable">The interactable that exited the trigger.</param>
        protected virtual void OnTriggerExitInteractable(IXRInteractable interactable)
        {

        }
    }
}
