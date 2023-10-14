using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Cloning;

namespace VRPortalToolkit.XRI
{
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
            //public readonly int fromCount;
            //public readonly int toCount;

            public TriggerInfo(Transform source, Collider collider, float distance)//, int fromCount, int toCount)
            {
                this.source = source;
                this.collider = collider;
                this.distance = distance;
                //this.fromCount = fromCount;
                //this.toCount = toCount;
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

        protected readonly TriggerHandler<TriggerKey, IXRInteractable> triggerHandler = new TriggerHandler<TriggerKey, IXRInteractable>();
        protected readonly HashSet<TriggerKey> _stayedColliders = new HashSet<TriggerKey>();
        private IEnumerator _waitFixedUpdateLoop;

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

        public virtual IEnumerable<Portal> GetPortalsToInteractable(IXRInteractable interactable)
        {
            if (interactableToTrigger.TryGetValue(interactable, out TriggerInfo info))
            {
                foreach (Portal portal in GetPortals(info.source, info.collider))//, out _, out _))
                    yield return portal;
            }

            yield break;
        }

        protected IEnumerable<Portal> GetPortals(Transform source, Collider collider)//, out int fromCount, out int toCount)
        {
            IEnumerable<Portal> from = null, to = null;
            //fromCount = 0;
            //toCount = 0;

            if (PortalCloning.TryGetCloneInfo(source, out var info))
            {
                from = info.GetOriginalToClonePortals();
                //fromCount = info.PortalCount;
            }

            if (PortalCloning.TryGetCloneInfo(collider.transform, out info))
            {
                to = info.GetCloneToOriginalPortals();
                //toCount = info.PortalCount;
            }
            
            return from.Difference(to);
        }

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
                // This also sets up "GetPortalsToInteractable" to work
                interactableToTrigger.Clear();

                foreach (var pair in triggerHandler)
                {
                    float distanceSqr = GetPortals(pair.Key.source, pair.Key.collider).DistanceSqr(transform.position, pair.Value.transform.position);

                    //float distanceSqr = GetPortals(pair.Key.source, pair.Key.collider, out int fromCount, out int toCount).DistanceSqr(transform.position, pair.Value.transform.position);

                    //int portalsCount = 0;
                    //Vector3 interactablePos = pair.Value.transform.position;

                    /*foreach (Portal portal in )
                    {
                        portalsCount++;
                        if (portal != null) portal.ModifyPoint(ref interactablePos);
                    }

                    float distanceSqr = (transform.position - interactablePos).sqrMagnitude;*/

                    if (!interactableToTrigger.TryGetValue(pair.Value, out TriggerInfo info))
                    {
                        targets.Add(pair.Value);
                        interactableToTrigger[pair.Value] = new TriggerInfo(pair.Key.source, pair.Key.collider, distanceSqr);//, fromCount, toCount);
                    }
                    else if (distanceSqr < info.distance)//IsPrefered(distanceSqr, fromCount, toCount, info))
                        interactableToTrigger[pair.Value] = new TriggerInfo(pair.Key.source, pair.Key.collider, distanceSqr);//, fromCount, toCount);
                }

                targets.Sort(new InteractableComparer(interactableToTrigger));
            }
        }

        /*private bool IsPrefered(float distance, int fromCount, int toCount, in TriggerInfo info)
        {

            //if (distance == info.distance)
            //{
                if (fromCount < info.fromCount) return true;

            if (fromCount == info.fromCount)
            {
                if (toCount < info.toCount) return true;
                //}
                if (distance < info.distance) return true;
            }

            return false;
        }*/

        protected virtual new void OnTriggerEnter(Collider other) =>
            OnCloneTriggerEnter(transform, other);

        protected virtual new void OnTriggerStay(Collider other) =>
            OnCloneTriggerStay(transform, other);

        protected virtual new void OnTriggerExit(Collider other) =>
            OnCloneTriggerExit(transform, other);

        public virtual void OnCloneTriggerEnter(Transform clone, Collider other)
        {
            AddInteractable(new TriggerKey(clone, other));
        }

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

        public virtual void OnCloneTriggerExit(Transform clone, Collider other)
        {
            triggerHandler.RemoveKey(new TriggerKey(clone, other));
        }

        protected virtual void OnTriggerEnterInteractable(IXRInteractable interactable)
        {

        }

        protected virtual void OnTriggerExitInteractable(IXRInteractable interactable)
        {

        }
    }
}
