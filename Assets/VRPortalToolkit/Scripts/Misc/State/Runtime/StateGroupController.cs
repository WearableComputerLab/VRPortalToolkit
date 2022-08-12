using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc
{
    public abstract class StateGroupController : MonoBehaviour
    {
        [SerializeField] protected List<StateRequester> requesters = new List<StateRequester>();
        public HeapAllocationFreeReadOnlyList<StateRequester> readOnlyRequesters => requesters;

        private ActionRemapper<StateRequester> _map = new ActionRemapper<StateRequester>();

        protected virtual void OnValidate()
        {
            Validate.FieldChanged(this, nameof(requesters), RemoveRequestersListeners, () =>
            {
                AddRequestersListeners();
                if (Application.isPlaying) UpdateActive();
            });
        }
        
        public virtual void Awake()
        {
            _map.onInvoke = RequesterModified;
            _map.addListener = (action, requester) => requester.modified.AddListener(action.Invoke);
            _map.removeListener = (action, requester) => requester.modified.RemoveListener(action.Invoke);
            AddRequestersListeners();
            _map.StartListening();

            UpdateActive();
        }

        protected virtual void RequesterModified(StateRequester requester) => UpdateActive();

        protected abstract void UpdateActive();

        protected virtual void AddRequestersListeners()
        {
            foreach (StateRequester stateRequester in requesters)
                AddRequesterListener(stateRequester);
        }
        protected virtual void RemoveRequestersListeners()
        {
            foreach (StateRequester stateRequester in requesters)
                RemoveRequesterListener(stateRequester);
        }

        protected virtual void AddRequesterListener(StateRequester stateRequester)
        {
            if (stateRequester) _map.AddSource(stateRequester);
        }

        protected virtual void RemoveRequesterListener(StateRequester stateRequester)
        {
            if (stateRequester) _map.RemoveSource(stateRequester);
        }

        public virtual void AddRequester(StateRequester item)
        {
            requesters.Add(item);
            UpdateActive();
        }

        public virtual void ClearRequesters()
        {
            requesters.Clear();
            UpdateActive();
        }

        public virtual bool ContainsRequester(StateRequester item) => requesters.Contains(item);

        public virtual bool RemoveRequester(StateRequester item)
        {
            if (requesters.Remove(item))
            {
                UpdateActive();
                return true;
            }

            return false;
        }
    }
}