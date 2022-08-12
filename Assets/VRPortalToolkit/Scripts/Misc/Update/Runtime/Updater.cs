using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using Misc.Reflection;
using Misc.Events;

// TODO: Moved back to an older version so no longer have CameraContextRendering

namespace Misc.Update
{
    [Serializable]
    public class Updater : IUpdateSourceListener
    {
        protected Dictionary<EventSource, SourceDelegate> delegateBySource;
        protected struct SourceDelegate
        {
            public Delegate Delegate;
            public int Count;
        }

        public Action onInvoke;

        private bool _enabled = false;
        public bool enabled
        {
            get => _enabled;
            set
            {
                _requestedDisabled = false;

                if (value != _enabled)
                {
                    _enabled = value;

                    if (!isUpdating)
                    {
                        if (_enabled)
                        {
                            if (_state == State.Disabled)
                            {
                                _state = State.PreInitialUpdate;
                                SubscribeToUpdateMask();
                            }
                            else _state = State.OutsideUpdate;
                        }
                        else OnPostUpdate();
                    }
                }
            }
        }

        private UpdateMask _updateMask;
        public UpdateMask updateMask
        {
            get => _updateMask;
            set
            {
                if (_updateMask != value)
                {
                    if (_updateMask != null) _updateMask.RemoveListener(this);

                    _updateMask = value;

                    if (_updateMask != null) _updateMask.AddListener(this);
                }
            }
        }

        public enum State : byte
        {
            Disabled = 0,
            PreInitialUpdate = 1,
            InitialUpdate = 2,
            OutsideUpdate = 3,
            InsideUpdate = 4,
            PreFinalUpdate = 4,
            FinalUpdate = 5
        }

        private State _state = State.Disabled;
        public State state => _state;

        public bool postIntialUpdate => (byte)_state > 2;

        public bool isUpdating => _state == State.InitialUpdate || _state == State.InsideUpdate || _state == State.FinalUpdate;

        private bool _requestedDisabled = false;

        // Will run one last update
        public void RequestDisable()
        {
            _requestedDisabled = true;
            
            if (_state != State.Disabled && !isUpdating)
                _state = State.PreFinalUpdate;
        }

        protected virtual void UpdateLoop()
        {
            if (_state == State.PreInitialUpdate)
            {
                _state = State.InitialUpdate;
                onInvoke?.Invoke();
                OnPostUpdate();
            }
            else if (_state == State.OutsideUpdate)
            {
                _state = State.InsideUpdate;
                onInvoke?.Invoke();
                OnPostUpdate();
            }
            else if (_state == State.PreFinalUpdate)
            {
                _state = State.FinalUpdate;
                onInvoke?.Invoke();

                if (_requestedDisabled || !_enabled)
                {
                    _state = State.Disabled;
                    _enabled = false;
                    UnsubscribeFromUpdateMask();
                }
                else _state = State.PreInitialUpdate;
            }
        }

        private void OnPostUpdate()
        {
            if (!_enabled)
            {
                _state = State.Disabled;
                UnsubscribeFromUpdateMask();
            }
            else if (_requestedDisabled) _state = State.PreFinalUpdate;
            else _state = State.OutsideUpdate;
        }

        private void UpdateLoop1<T0>(T0 t) => UpdateLoop();

        private void UpdateLoop2<T0, T1>(T0 t0, T1 t1) => UpdateLoop();

        private void UpdateLoop3<T0, T1, T2>(T0 t0, T1 t1, T2 t2) => UpdateLoop();

        private void UpdateLoop4<T0, T1, T2, T3>(T0 t0, T1 t1, T2 t2, T3 t3) => UpdateLoop();

        protected virtual void SubscribeToUpdateMask()
        {
            if (_updateMask != null)
            {
                UpdateFlags updateMask = _updateMask.UpdateFlags;

                //if (updateMask.HasFlag(UpdateFlags.OnEnabled)) UpdateLoop();

                if (updateMask.HasFlag(UpdateFlags.FixedUpdate)) UpdatePool.FixedUpdateAdd(UpdateLoop);

                if (updateMask.HasFlag(UpdateFlags.WaitForFixedUpdate)) UpdatePool.WaitForFixedUpdateAdd(UpdateLoop);

                if (updateMask.HasFlag(UpdateFlags.Update)) UpdatePool.UpdateAdd(UpdateLoop);

                if (updateMask.HasFlag(UpdateFlags.NullUpdate)) UpdatePool.NullUpdateAdd(UpdateLoop);

                if (updateMask.HasFlag(UpdateFlags.LateUpdate)) UpdatePool.LateUpdateAdd(UpdateLoop);

                if (updateMask.HasFlag(UpdateFlags.WaitForEndOfFrame)) UpdatePool.WaitForEndOfFrameAdd(UpdateLoop);

                if (updateMask.HasFlag(UpdateFlags.OnPreCull)) Camera.onPreCull += UpdateLoop1;

                if (updateMask.HasFlag(UpdateFlags.OnPreRender)) Camera.onPreRender += UpdateLoop1;

                if (updateMask.HasFlag(UpdateFlags.OnPostRender)) Camera.onPostRender += UpdateLoop1;

                if (updateMask.HasFlag(UpdateFlags.BeginCameraRendering)) RenderPipelineManager.beginCameraRendering += UpdateLoop2;

                //if (updateMask.HasFlag(UpdateFlags.BeginContextRendering)) RenderPipelineManager.beginContextRendering += UpdateLoop2;

                if (updateMask.HasFlag(UpdateFlags.BeginFrameRendering)) RenderPipelineManager.beginFrameRendering += UpdateLoop2;

                if (updateMask.HasFlag(UpdateFlags.EndCameraRendering)) RenderPipelineManager.endCameraRendering += UpdateLoop2;

                //if (updateMask.HasFlag(UpdateFlags.EndContextRendering)) RenderPipelineManager.endContextRendering += UpdateLoop2;

                if (updateMask.HasFlag(UpdateFlags.EndFrameRendering)) RenderPipelineManager.endFrameRendering += UpdateLoop2;

                foreach (EventSource source in _updateMask.Sources)
                    ActualSubscribeToSource(source);

                foreach (float seconds in _updateMask.WaitForSeconds)
                    UpdatePool.WaitForSecondsAdd(UpdateLoop, seconds);
            }
        }

        protected virtual void UnsubscribeFromUpdateMask()
        {
            if (_updateMask != null)
            {
                UpdateFlags updateMask = _updateMask.UpdateFlags;
                if (updateMask.HasFlag(UpdateFlags.FixedUpdate)) UpdatePool.FixedUpdateRemove(UpdateLoop);

                if (updateMask.HasFlag(UpdateFlags.WaitForFixedUpdate)) UpdatePool.WaitForFixedUpdateRemove(UpdateLoop);

                if (updateMask.HasFlag(UpdateFlags.Update)) UpdatePool.UpdateRemove(UpdateLoop);

                if (updateMask.HasFlag(UpdateFlags.NullUpdate)) UpdatePool.NullUpdateRemove(UpdateLoop);

                if (updateMask.HasFlag(UpdateFlags.LateUpdate)) UpdatePool.LateUpdateRemove(UpdateLoop);

                if (updateMask.HasFlag(UpdateFlags.WaitForEndOfFrame)) UpdatePool.WaitForEndOfFrameRemove(UpdateLoop);

                if (updateMask.HasFlag(UpdateFlags.OnPreCull)) Camera.onPreCull -= UpdateLoop1;

                if (updateMask.HasFlag(UpdateFlags.OnPreRender)) Camera.onPreRender -= UpdateLoop1;

                if (updateMask.HasFlag(UpdateFlags.OnPostRender)) Camera.onPostRender -= UpdateLoop1;

                if (updateMask.HasFlag(UpdateFlags.BeginCameraRendering)) RenderPipelineManager.beginCameraRendering -= UpdateLoop2;

                //if (updateMask.HasFlag(UpdateFlags.BeginContextRendering)) RenderPipelineManager.beginContextRendering -= UpdateLoop2;

                if (updateMask.HasFlag(UpdateFlags.BeginFrameRendering)) RenderPipelineManager.beginFrameRendering -= UpdateLoop2;

                if (updateMask.HasFlag(UpdateFlags.EndCameraRendering)) RenderPipelineManager.endCameraRendering -= UpdateLoop2;

                //if (updateMask.HasFlag(UpdateFlags.EndContextRendering)) RenderPipelineManager.endContextRendering -= UpdateLoop2;

                if (updateMask.HasFlag(UpdateFlags.EndFrameRendering)) RenderPipelineManager.endFrameRendering -= UpdateLoop2;

                foreach (EventSource source in _updateMask.Sources)
                    ActualUnsubscribeFromSource(source);

                foreach (float seconds in _updateMask.WaitForSeconds)
                    UpdatePool.WaitForSecondsRemove(UpdateLoop, seconds);
                
                //if (updateMask.HasFlag(UpdateFlags.OnDisabled)) UpdateLoop();
            }
        }

        public virtual void SubscribeToFixedUpdate()
        {
            if (_state != State.Disabled) UpdatePool.FixedUpdateAdd(UpdateLoop);
        }

        public virtual void UnsubscribeFromFixedUpdate()
        {
            if (_state != State.Disabled) UpdatePool.FixedUpdateRemove(UpdateLoop);
        }

        public virtual void SubscribeToWaitForFixedUpdate()
        {
            if (_state != State.Disabled) UpdatePool.WaitForFixedUpdateAdd(UpdateLoop);
        }

        public virtual void UnsubscribeFromWaitForFixedUpdate()
        {
            if (_state != State.Disabled) UpdatePool.WaitForFixedUpdateRemove(UpdateLoop);
        }

        public virtual void SubscribeToUpdate()
        {
            if (_state != State.Disabled) UpdatePool.UpdateAdd(UpdateLoop);
        }

        public virtual void UnsubscribeFromUpdate()
        {
            if (_state != State.Disabled) UpdatePool.UpdateRemove(UpdateLoop);
        }

        public virtual void SubscribeToNullUpdate()
        {
            if (_state != State.Disabled) UpdatePool.NullUpdateAdd(UpdateLoop);
        }

        public virtual void UnsubscribeFromNullUpdate()
        {
            if (_state != State.Disabled) UpdatePool.NullUpdateRemove(UpdateLoop);
        }

        public virtual void SubscribeToLateUpdate()
        {
            if (_state != State.Disabled) UpdatePool.LateUpdateAdd(UpdateLoop);
        }

        public virtual void UnsubscribeFromLateUpdate()
        {
            if (_state != State.Disabled) UpdatePool.LateUpdateRemove(UpdateLoop);
        }

        public virtual void SubscribeToWaitForEndOfFrame()
        {
            if (_state != State.Disabled) UpdatePool.WaitForEndOfFrameAdd(UpdateLoop);
        }

        public virtual void UnsubscribeFromWaitForEndOfFrame()
        {
            if (_state != State.Disabled) UpdatePool.WaitForEndOfFrameRemove(UpdateLoop);
        }

        public virtual void SubscribeToPreCull()
        {
            if (_state != State.Disabled) Camera.onPreCull += UpdateLoop1;
        }

        public virtual void UnsubscribeFromPreCull()
        {
            if (_state != State.Disabled) Camera.onPreCull -= UpdateLoop1;
        }

        public virtual void SubscribeToPreRender()
        {
            if (_state != State.Disabled) Camera.onPreRender += UpdateLoop1;
        }

        public virtual void UnsubscribeFromPreRender()
        {
            if (_state != State.Disabled) Camera.onPreRender -= UpdateLoop1;
        }

        public virtual void SubscribeToPostRender()
        {
            if (_state != State.Disabled) Camera.onPostRender += UpdateLoop1;
        }

        public virtual void UnsubscribeFromPostRender()
        {
            if (_state != State.Disabled) Camera.onPostRender -= UpdateLoop1;
        }

        public virtual void SubscribeToBeginCameraRendering()
        {
            if (_state != State.Disabled) RenderPipelineManager.beginCameraRendering += UpdateLoop2;
        }

        public virtual void UnsubscribeFromBeginCameraRendering()
        {
            if (_state != State.Disabled) RenderPipelineManager.beginCameraRendering -= UpdateLoop2;
        }

        public virtual void SubscribeToBeginContextRendering()
        {
            //if (_state != State.Disabled) RenderPipelineManager.beginContextRendering += UpdateLoop2;
        }

        public virtual void UnsubscribeFromBeginContextRendering()
        {
            //if (_state != State.Disabled) RenderPipelineManager.beginContextRendering -= UpdateLoop2;
        }

        public virtual void SubscribeToBeginFrameRendering()
        {
            if (_state != State.Disabled) RenderPipelineManager.beginFrameRendering += UpdateLoop2;
        }

        public virtual void UnsubscribeFromBeginFrameRendering()
        {
            if (_state != State.Disabled) RenderPipelineManager.beginFrameRendering -= UpdateLoop2;
        }

        public virtual void SubscribeToEndCameraRendering()
        {
            if (_state != State.Disabled) RenderPipelineManager.endCameraRendering += UpdateLoop2;
        }

        public virtual void UnsubscribeFromEndCameraRendering()
        {
            if (_state != State.Disabled) RenderPipelineManager.endCameraRendering -= UpdateLoop2;
        }

        public virtual void SubscribeToEndContextRendering()
        {
            //if (_state != State.Disabled) RenderPipelineManager.endContextRendering += UpdateLoop2;
        }

        public virtual void UnsubscribeFromEndContextRendering()
        {
            //if (_state != State.Disabled) RenderPipelineManager.endContextRendering -= UpdateLoop2;
        }

        public virtual void SubscribeToEndFrameRendering()
        {
            if (_state != State.Disabled) RenderPipelineManager.endFrameRendering += UpdateLoop2;
        }

        public virtual void UnsubscribeFromEndFrameRendering()
        {
            if (_state != State.Disabled) RenderPipelineManager.endFrameRendering -= UpdateLoop2;
        }

        public virtual void SubscribeToOnEnabled() {}

        public virtual void UnsubscribeFromOnEnabled() {}

        public virtual void SubscribeToOnDisabled() {}

        public virtual void UnsubscribeFromOnDisabled() {}

        public virtual void ForceInvoke()
        {
            if (_state != State.Disabled) UpdateLoop();
        }

        public virtual void SubscribeToSource(EventSource source)
        {
            if (_state != State.Disabled) ActualSubscribeToSource(source);
        }

        public virtual void UnsubscribeFromSource(EventSource source)
        {
            ActualUnsubscribeFromSource(source);
        }

        public virtual void ActualSubscribeToSource(EventSource source)
        {
            object asObject = ReflectionUtilities.GetValue(source.SourceObject, source.EventName);

            if (asObject != null)
            {
                if (delegateBySource != null && delegateBySource.TryGetValue(source, out SourceDelegate value))
                {
                    asObject.GetType().InvokeMember("AddListener", BindingFlags.InvokeMethod, null, asObject, new[] { value.Delegate });
                    delegateBySource[source] = new SourceDelegate() { Delegate = value.Delegate, Count = value.Count + 1 };
                }
                else if (asObject is UnityEventBase)
                {
                    if (asObject is UnityEvent)
                        AddListener(nameof(UpdateLoop), source, asObject, typeof(UnityEvent), typeof(UnityAction));
                    else
                    {
                        Type type = asObject.GetType(), genericType;

                        while (type != typeof(UnityEventBase))
                        {
                            if (type.IsGenericType)
                            {
                                genericType = type.GetGenericTypeDefinition();

                                if (genericType == typeof(UnityEvent<>))
                                {
                                    AddListener(nameof(UpdateLoop1), source, asObject, type, genericType, typeof(UnityAction<>));
                                    break;
                                }
                                else if (genericType == typeof(UnityEvent<,>))
                                {
                                    AddListener(nameof(UpdateLoop2), source, asObject, type, genericType, typeof(UnityAction<,>));
                                    break;
                                }
                                else if (genericType == typeof(UnityEvent<,,>))
                                {
                                    AddListener(nameof(UpdateLoop3), source, asObject, type, genericType, typeof(UnityAction<,,>));
                                    break;
                                }
                                else if (genericType == typeof(UnityEvent<,,,>))
                                {
                                    AddListener(nameof(UpdateLoop4), source, asObject, type, genericType, typeof(UnityAction<,,,>));
                                    break;
                                }
                            }

                            type = type.BaseType;
                        }
                    }
                }
                else if (asObject is SerializableEventBase)
                {
                    if (asObject is SerializableEvent)
                        AddListener(nameof(UpdateLoop), source, asObject, typeof(SerializableEvent), typeof(UnityAction));
                    else
                    {
                        Type type = asObject.GetType(), genericType;

                        while (type != typeof(SerializableEventBase))
                        {
                            if (type.IsGenericType)
                            {
                                genericType = type.GetGenericTypeDefinition();

                                if (genericType == typeof(SerializableEvent<>))
                                {
                                    AddListener(nameof(UpdateLoop1), source, asObject, type, genericType, typeof(UnityAction<>));
                                    break;
                                }
                                else if (genericType == typeof(SerializableEvent<,>))
                                {
                                    AddListener(nameof(UpdateLoop2), source, asObject, type, genericType, typeof(UnityAction<,>));
                                    break;
                                }
                                else if (genericType == typeof(SerializableEvent<,,>))
                                {
                                    AddListener(nameof(UpdateLoop3), source, asObject, type, genericType, typeof(UnityAction<,,>));
                                    break;
                                }
                                else if (genericType == typeof(SerializableEvent<,,,>))
                                {
                                    AddListener(nameof(UpdateLoop4), source, asObject, type, genericType, typeof(UnityAction<,,,>));
                                    break;
                                }
                            }

                            type = type.BaseType;
                        }
                    }
                }
                else
                {
                    Debug.LogError("<" + asObject.GetType() + "> is not currently supported by Updater");
                }
            }
        }

        protected virtual void AddListener(string uniqueMethod, EventSource source, object asObject, Type eventType, Type genericType, Type actionType)
            => AddListener(uniqueMethod, source, asObject, eventType, actionType.MakeGenericType(eventType.GenericTypeArguments)); // I dont know how to get the generic type for the thing I actually want
        
        protected virtual void AddListener(string uniqueMethod, EventSource source, object asObject, Type eventType, Type delegateType)
        {
            MethodInfo methodInfo = typeof(Updater).GetMethod(uniqueMethod, BindingFlags.NonPublic | BindingFlags.Instance);

            if (delegateType.IsGenericType)
                methodInfo = methodInfo.MakeGenericMethod(delegateType.GenericTypeArguments);

            Delegate del = Delegate.CreateDelegate(delegateType, this, methodInfo);
            eventType.InvokeMember("AddListener", BindingFlags.InvokeMethod, null, asObject, new[] { del });

            if (delegateBySource == null) delegateBySource = new Dictionary<EventSource, SourceDelegate>();
            delegateBySource[source] = new SourceDelegate() { Delegate = del, Count = 1 };
        }

        public virtual void ActualUnsubscribeFromSource(EventSource source)
        {
            if (!source.SourceObject || string.IsNullOrEmpty(source.EventName)) return;

            object asObject = ReflectionUtilities.GetValue(source.SourceObject, source.EventName);

            if (asObject != null)
            {
                if (delegateBySource != null && delegateBySource.TryGetValue(source, out SourceDelegate value))
                {
                    asObject.GetType().InvokeMember("RemoveListener", BindingFlags.InvokeMethod, null, asObject, new[] { value.Delegate });

                    if (value.Count == 1) delegateBySource.Remove(source);
                    else delegateBySource[source] = new SourceDelegate() { Delegate = value.Delegate, Count = value.Count - 1 };
                }
            }
        }

        public virtual void SubscribeToWaitForSeconds(float seconds)
        {
            if (_state != State.Disabled) UpdatePool.WaitForSecondsAdd(UpdateLoop, seconds);
        }

        public virtual void UnsubscribeFromWaitForSeconds(float seconds)
        {
            if (_state != State.Disabled) UpdatePool.WaitForSecondsRemove(UpdateLoop, seconds);
        }
    }
}
