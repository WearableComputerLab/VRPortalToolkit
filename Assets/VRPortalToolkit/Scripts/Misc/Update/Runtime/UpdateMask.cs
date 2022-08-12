using Misc.EditorHelpers;
using System.Collections.Generic;
using UnityEngine;

// What is the best way to have update source work? Unity just clears references so its hard to work with

// It looks like because of the custom editor, CallOnChangeOf no longer works
namespace Misc.Update
{
    [System.Serializable]
    public class UpdateMask
    {
        [SerializeField] private UpdateFlags _updateFlags;
        public UpdateFlags UpdateFlags
        {
            get => _updateFlags;
            set
            {
                if (_updateFlags != value)
                {
                    UnsubscribeFromUpdateMask(_updateFlags & ~value);
                    SubscribeToUpdateMask(value & ~_updateFlags);
                    Validate.UpdateField(this, nameof(_updateFlags), _updateFlags = value);
                }
            }
        }

        [SerializeField] private List<EventSource> _sources = new List<EventSource>(0);

        [SerializeField] private List<float> _waitForSeconds = new List<float>(0);

        // Most things generally only need one listener
        protected List<IUpdateSourceListener> listeners = new List<IUpdateSourceListener>(1);

        public HeapAllocationFreeReadOnlyList<EventSource> Sources => _sources;

        public HeapAllocationFreeReadOnlyList<float> WaitForSeconds => _waitForSeconds;

        public UpdateMask() { }

        public UpdateMask(UpdateMask original) : base()
        {
            Copy(original);
        }

        public UpdateMask(UpdateFlags updateMask) : base()
        {
            UpdateFlags = updateMask;
            SubscribeToUpdateMask();
        }

        public UpdateMask(UpdateFlags updateMask, params float[] waitForSeconds) : base()
        {
            UpdateFlags = updateMask;
            _waitForSeconds = new List<float>(waitForSeconds);
            SubscribeToUpdateMask();
        }

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_updateFlags), nameof(UpdateFlags));
            Validate.FieldChanged(this, nameof(_sources), UnsubscribeFromSources, SubscribeToSources);
            Validate.FieldChanged(this, nameof(_waitForSeconds), UnsubscribeFromWaitForSeconds, SubscribeToWaitForSeconds);
        }

        public void Copy(UpdateMask original)
        {
            UnsubscribeFromUpdateMask();
            _sources.AddRange(original._sources);
            _waitForSeconds.AddRange(original._waitForSeconds);
            _updateFlags = original.UpdateFlags;
            SubscribeToUpdateMask();
        }

        public void AddListener(IUpdateSourceListener listener)
        {
            listeners.Add(listener);
            SubscribeToUpdateMask(listener);
        }

        public void RemoveListener(IUpdateSourceListener listener)
        {
            if (listeners.Remove(listener))
                UnsubscribeFromUpdateMask(listener);
        }

        public void SourceInvoke()
        {
            foreach (IUpdateSourceListener listener in listeners)
                listener.ForceInvoke();
        }

        public void AddWaitForSeconds(float seconds)
        {
            if (_waitForSeconds == null) _waitForSeconds = new List<float>(1);
            _waitForSeconds.Add(seconds);

            Validate.UpdateField(this, nameof(_waitForSeconds), _waitForSeconds);

            SubscribeToWaitForSeconds(seconds);
        }

        public void RemoveWaitForSeconds(float seconds)
        {
            if (_waitForSeconds != null && _waitForSeconds.Remove(seconds) && UpdateFlags.HasFlag(UpdateFlags.Sources))
                UnsubscribeFromWaitForSeconds(seconds);

            Validate.UpdateField(this, nameof(_waitForSeconds), _waitForSeconds);
        }

        public void ClearWaitForSeconds()
        {
            UnsubscribeFromWaitForSeconds();
            if (_waitForSeconds != null) _waitForSeconds.Clear();

            Validate.UpdateField(this, nameof(_waitForSeconds), _waitForSeconds);
        }

        public void AddSource(Object sourceObject, string eventName)
        {
            if (sourceObject && !string.IsNullOrEmpty(eventName))
            {
                EventSource source = new EventSource(sourceObject, eventName);
                if (_sources == null) _sources = new List<EventSource>(1);
                _sources.Add(source);

                Validate.UpdateField(this, nameof(_sources), _sources);

                if (UpdateFlags.HasFlag(UpdateFlags.Sources)) SubscribeToSource(source);
            }
        }

        public void RemoveSource(Object sourceObject, string eventName)
        {
            EventSource source = new EventSource(sourceObject, eventName);

            if (_sources != null && _sources.Remove(source) && UpdateFlags.HasFlag(UpdateFlags.Sources))
                UnsubscribeFromSource(source);

            Validate.UpdateField(this, nameof(_sources), _sources);
        }

        public void ClearSources()
        {
            UnsubscribeFromSources();
            _sources.Clear();

            Validate.UpdateField(this, nameof(_sources), _sources);
        }

        private void SubscribeToUpdateMask()
            => SubscribeToUpdateMask(_updateFlags);

        private void SubscribeToUpdateMask(UpdateFlags updateMask)
        {
            foreach (IUpdateSourceListener listener in listeners)
                SubscribeToUpdateMask(listener, updateMask);
        }

        private void SubscribeToUpdateMask(IUpdateSourceListener listener)
            => SubscribeToUpdateMask(_updateFlags);

        private void SubscribeToUpdateMask(IUpdateSourceListener listener, UpdateFlags updateMask)
        {
            if (updateMask.HasFlag(UpdateFlags.FixedUpdate)) listener.SubscribeToFixedUpdate();

            if (updateMask.HasFlag(UpdateFlags.WaitForFixedUpdate)) listener.SubscribeToWaitForFixedUpdate();

            if (updateMask.HasFlag(UpdateFlags.Update)) listener.SubscribeToUpdate();

            if (updateMask.HasFlag(UpdateFlags.NullUpdate)) listener.SubscribeToNullUpdate();

            if (updateMask.HasFlag(UpdateFlags.LateUpdate)) listener.SubscribeToLateUpdate();

            if (updateMask.HasFlag(UpdateFlags.WaitForEndOfFrame)) listener.SubscribeToWaitForEndOfFrame();

            if (updateMask.HasFlag(UpdateFlags.OnPreCull)) listener.SubscribeToPreCull();

            if (updateMask.HasFlag(UpdateFlags.OnPreRender)) listener.SubscribeToPreRender();

            if (updateMask.HasFlag(UpdateFlags.OnPostRender)) listener.SubscribeToPostRender();

            if (updateMask.HasFlag(UpdateFlags.BeginCameraRendering)) listener.SubscribeToBeginCameraRendering();

            if (updateMask.HasFlag(UpdateFlags.BeginContextRendering)) listener.SubscribeToBeginContextRendering();

            if (updateMask.HasFlag(UpdateFlags.BeginFrameRendering)) listener.SubscribeToBeginFrameRendering();

            if (updateMask.HasFlag(UpdateFlags.EndCameraRendering)) listener.SubscribeToEndCameraRendering();

            if (updateMask.HasFlag(UpdateFlags.EndContextRendering)) listener.SubscribeToEndContextRendering();

            if (updateMask.HasFlag(UpdateFlags.EndFrameRendering)) listener.SubscribeToEndFrameRendering();

            if (updateMask.HasFlag(UpdateFlags.EndContextRendering)) listener.SubscribeToEndContextRendering();

            if (updateMask.HasFlag(UpdateFlags.EndFrameRendering)) listener.SubscribeToEndFrameRendering();

            //if (updateMask.HasFlag(UpdateFlags.OnEnabled)) listener.SubscribeToOnEnabled();

            //if (updateMask.HasFlag(UpdateFlags.OnDisabled)) listener.SubscribeToOnDisabled();

            if (updateMask.HasFlag(UpdateFlags.Sources) && _sources != null)
                foreach (EventSource source in _sources)
                    listener.SubscribeToSource(source);

            if (updateMask.HasFlag(UpdateFlags.WaitForSeconds) && _waitForSeconds != null)
                foreach (float seconds in _waitForSeconds)
                    if (seconds > float.Epsilon) listener.SubscribeToWaitForSeconds(seconds);
        }

        private void UnsubscribeFromUpdateMask()
            => UnsubscribeFromUpdateMask(_updateFlags);

        private void UnsubscribeFromUpdateMask(UpdateFlags updateMask)
        {
            foreach (IUpdateSourceListener listener in listeners)
                UnsubscribeFromUpdateMask(listener, updateMask);
        }

        private void UnsubscribeFromUpdateMask(IUpdateSourceListener listener)
            => UnsubscribeFromUpdateMask(_updateFlags);

        private void UnsubscribeFromUpdateMask(IUpdateSourceListener listener, UpdateFlags updateMask)
        {
            if (updateMask.HasFlag(UpdateFlags.FixedUpdate)) listener.UnsubscribeFromFixedUpdate();

            if (updateMask.HasFlag(UpdateFlags.WaitForFixedUpdate)) listener.UnsubscribeFromWaitForFixedUpdate();

            if (updateMask.HasFlag(UpdateFlags.Update)) listener.UnsubscribeFromUpdate();

            if (updateMask.HasFlag(UpdateFlags.NullUpdate)) listener.UnsubscribeFromNullUpdate();

            if (updateMask.HasFlag(UpdateFlags.LateUpdate)) listener.UnsubscribeFromLateUpdate();

            if (updateMask.HasFlag(UpdateFlags.WaitForEndOfFrame)) listener.UnsubscribeFromWaitForEndOfFrame();

            if (updateMask.HasFlag(UpdateFlags.OnPreCull)) listener.UnsubscribeFromPreCull();

            if (updateMask.HasFlag(UpdateFlags.OnPreRender)) listener.UnsubscribeFromPreRender();

            if (updateMask.HasFlag(UpdateFlags.OnPostRender)) listener.UnsubscribeFromPostRender();

            if (updateMask.HasFlag(UpdateFlags.BeginCameraRendering)) listener.UnsubscribeFromBeginCameraRendering();

            if (updateMask.HasFlag(UpdateFlags.BeginContextRendering)) listener.UnsubscribeFromBeginContextRendering();

            if (updateMask.HasFlag(UpdateFlags.BeginFrameRendering)) listener.UnsubscribeFromBeginFrameRendering();

            if (updateMask.HasFlag(UpdateFlags.EndCameraRendering)) listener.UnsubscribeFromEndCameraRendering();

            if (updateMask.HasFlag(UpdateFlags.EndContextRendering)) listener.UnsubscribeFromEndContextRendering();

            if (updateMask.HasFlag(UpdateFlags.EndFrameRendering)) listener.UnsubscribeFromEndFrameRendering();

            //if (updateMask.HasFlag(UpdateFlags.OnEnabled)) listener.UnsubscribeFromOnEnabled();

            //if (updateMask.HasFlag(UpdateFlags.OnDisabled)) listener.UnsubscribeFromOnDisabled();

            if (updateMask.HasFlag(UpdateFlags.Sources) && _sources != null)
                foreach (EventSource source in _sources)
                    listener.UnsubscribeFromSource(source);

            if (updateMask.HasFlag(UpdateFlags.WaitForSeconds) && _waitForSeconds != null)
                foreach (float seconds in _waitForSeconds)
                    if (seconds > float.Epsilon) listener.UnsubscribeFromWaitForSeconds(seconds);
        }

        private void SubscribeToSource(EventSource source)
        {
            foreach (IUpdateSourceListener listener in listeners)
                listener.SubscribeToSource(source);
        }

        private void UnsubscribeFromSource(EventSource source)
        {
            foreach (IUpdateSourceListener listener in listeners)
                listener.UnsubscribeFromSource(source);
        }

        private void SubscribeToSources()
        {
            if (_sources == null) return;

            foreach (EventSource source in _sources)
                SubscribeToSource(source);
        }

        private void UnsubscribeFromSources()
        {
            if (_sources == null) return;

            foreach (EventSource source in _sources)
                UnsubscribeFromSource(source);
        }

        private void SubscribeToWaitForSeconds(float seconds)
        {
            if (_waitForSeconds == null || seconds <= float.Epsilon) return;

            foreach (IUpdateSourceListener listener in listeners)
                listener.SubscribeToWaitForSeconds(seconds);
        }

        private void UnsubscribeFromWaitForSeconds(float seconds)
        {
            if (_waitForSeconds == null || seconds <= float.Epsilon) return;

            foreach (IUpdateSourceListener listener in listeners)
                listener.UnsubscribeFromWaitForSeconds(seconds);
        }

        private void SubscribeToWaitForSeconds()
        {
            if (_waitForSeconds == null) return;

            foreach (float seconds in _waitForSeconds)
            {
                if (seconds <= float.Epsilon) continue;

                foreach (IUpdateSourceListener listener in listeners)
                    listener.SubscribeToWaitForSeconds(seconds);
            }
        }

        private void UnsubscribeFromWaitForSeconds()
        {
            if (_waitForSeconds == null) return;

            foreach (float seconds in _waitForSeconds)
            {
                if (seconds <= float.Epsilon) continue;

                foreach (IUpdateSourceListener listener in listeners)
                    listener.UnsubscribeFromWaitForSeconds(seconds);
            }
        }
    }
}
