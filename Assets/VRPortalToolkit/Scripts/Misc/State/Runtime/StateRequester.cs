using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Misc
{
    public abstract class StateRequester<TSource> : MonoBehaviour
    {
        private int _activeRequestsCount = -1;
        public int activeRequestsCount {
            get
            {
                if (_activeRequestsCount < 0)
                {
                    _activeRequestsCount = 0;

                    foreach (StateRequest<TSource> request in requests)
                        if (request.state) _activeRequestsCount++;
                }

                return _activeRequestsCount;
            }
            protected set => _activeRequestsCount = value;
        }

        private int _deactiveRequestsCount = -1;
        public int deactiveRequestsCount {
            get {
                if (_deactiveRequestsCount < 0)
                {
                    _deactiveRequestsCount = 0;

                    foreach (StateRequest<TSource> request in requests)
                        if (!request.state) _deactiveRequestsCount++;
                }

                return _deactiveRequestsCount;
            }
            protected set => _deactiveRequestsCount = value;
        }

        [SerializeField] protected List<StateRequest<TSource>> requests = new List<StateRequest<TSource>>();
        public HeapAllocationFreeReadOnlyList<StateRequest<TSource>> readOnlyRequests => requests;
        public int requestsCount => requests.Count;

        public UnityEvent modified { get; } = new UnityEvent();

        protected virtual void OnValidate()
        {
            Validate.FieldChanged(this, nameof(requests), null, OnAfterChangeOfRequests);
        }

        public virtual void ClearRequests()
        {
            if (requests.Count > 0)
            {
                requests.Clear();
                activeRequestsCount = 0;
                deactiveRequestsCount = 0;
                modified?.Invoke();
            }
        }

        public virtual void RequestActive(TSource source) => RequestSetActive(source, true);

        public virtual void RequestDeactivate(TSource source) => RequestSetActive(source, false);

        public virtual void RequestSetActive(TSource source, bool active)
        {
            if (IsSourceValid(source))
            {
                if (TryGetRequestIndex(source, out int index))
                {
                    if (requests[index].state) activeRequestsCount--;
                    else deactiveRequestsCount--;

                    requests[index] = new StateRequest<TSource>() { source = source, state = active};
                }
                else
                    requests.Add(new StateRequest<TSource>() { source = source, state = active});

                if (active) activeRequestsCount++;
                else deactiveRequestsCount++;

                modified?.Invoke();
            }
        }
        
        public virtual void DoRemoveRequest(TSource source) => RemoveRequest(source);

        public virtual bool RemoveRequest(TSource source)
        {
            if (TryGetRequestIndex(source, out int index))
            {
                if (requests[index].state) activeRequestsCount--;
                else deactiveRequestsCount--;

                requests.RemoveAt(index);
                modified?.Invoke();

                return true;
            }

            return false;
        }

        protected virtual bool TryGetRequestIndex(TSource source, out int index)
        {
            for (index = 0; index < requests.Count; index++)
                if (IsSourceEqual(requests[index].source, source)) return true;

            return false;
        }

        protected virtual bool IsSourceValid(TSource source)
        {
            return requests != null;
        }

        protected virtual bool IsSourceEqual(TSource sourceA, TSource sourceB)
        {
            return EqualityComparer<TSource>.Default.Equals(sourceA, sourceB);
        }

        public virtual bool TryGetRequest(TSource source, out bool active)
        {
            if (TryGetRequestIndex(source, out int index))
            {
                active = requests[index].state;
                return true;
            }

            return active = false;
        }

        protected virtual void OnAfterChangeOfRequests()
        {
            activeRequestsCount = 0;
            deactiveRequestsCount = 0;

            foreach (StateRequest<TSource> request in requests)
            {
                if (request.state) activeRequestsCount++;
                else deactiveRequestsCount++;
            }

            modified?.Invoke();
        }
    }

    public class StateRequester : StateRequester<Component>
    {
        protected override bool IsSourceValid(Component source) => source;

        protected override bool IsSourceEqual(Component sourceA, Component sourceB) => sourceA == sourceB;
    }
}
