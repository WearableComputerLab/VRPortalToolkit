using Misc.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Tasks
{
    public class Task : MonoBehaviour
    {
        /*[TaskIsRunning] // Draw the UI after this
        [SerializeField] private StateMode _stateMode = 0;
        public StateMode stateMode { get => _stateMode; set => _stateMode = value; }

        [System.Flags]
        public enum StateMode
        {
            IgnoreEnabled = 0,
            BeginOnEnabled = 1 << 1,
            CancelOnDisabled = 1 << 2
        }

        public UnityEvent started = new UnityEvent();
        public UnityEvent cancelled = new UnityEvent();
        public UnityEvent completed = new UnityEvent();

        public bool isRunning { get; protected set; }

        public virtual void DoTryBegin() => TryBegin();

        protected virtual void OnEnable()
        {
            if (stateMode.HasFlag(StateMode.BeginOnEnabled))
                TryBegin();
        }

        protected virtual void OnDisable()
        {
            if (stateMode.HasFlag(StateMode.CancelOnDisabled))
                TryCancel();
        }

        public virtual bool TryBegin()
        {
            if ((!stateMode.HasFlag(StateMode.CancelOnDisabled) || isActiveAndEnabled) && !isRunning)
            {
                Begin();
                return true;
            }

            return false;
        }

        public virtual void Begin()
        {
            if (isRunning) Cancel();

            isRunning = true;
            started?.Invoke();
        }

        public virtual void DoTryCancel() => TryCancel();

        public virtual bool TryCancel()
        {
            if (isRunning)
            {
                Cancel();
                return true;
            }

            return false;
        }

        public virtual void Cancel()
        {
            isRunning = false;
            cancelled?.Invoke();
        }

        public virtual void DoTryComplete() => TryComplete();

        public virtual bool TryComplete()
        {
            if (isRunning)
            {
                Complete();
                return true;
            }

            return false;
        }

        public virtual void Complete()
        {
            isRunning = false;
            completed?.Invoke();
        }*/

        [TaskIsRunning] // Draw the UI after this
        [SerializeField] private StateMode _stateMode = StateMode.IgnoreEnabled;
        public StateMode stateMode { get => _stateMode; set => _stateMode = value; }

        [System.Flags]
        public enum StateMode
        {
            IgnoreEnabled = 0,
            BeginOnEnabled = 1 << 1,
            CancelOnDisabled = 1 << 2
        }

        private InvokeState _invokeState = InvokeState.NotInvoking;

        private enum InvokeState : byte
        {
            NotInvoking = 0,
            Invoking = 1,
            Begin = 2,
            Cancel = 3,
            Complete = 4
        }

        public bool isInvoking => _invokeState != InvokeState.NotInvoking;
        public SerializableEvent started = new SerializableEvent();
        public SerializableEvent cancelled = new SerializableEvent();
        public SerializableEvent completed = new SerializableEvent();

        private bool _isRunning = false;
        public bool isRunning { get => _isRunning; }//protected set => _isRunning = value; }

        public virtual void DoTryBegin() => TryBegin();

        protected virtual void OnEnable()
        {
            if (stateMode.HasFlag(StateMode.BeginOnEnabled))
                TryBegin();
        }

        protected virtual void OnDisable()
        {
            if (stateMode.HasFlag(StateMode.CancelOnDisabled))
                TryCancel();
        }

        public virtual bool TryBegin()
        {
            if ((!stateMode.HasFlag(StateMode.CancelOnDisabled) || isActiveAndEnabled) && !isRunning)
            {
                Begin();
                return true;
            }

            return false;
        }

        public virtual void Begin()
        {
            /*if (isRunning) Cancel();

            _isRunning = true;
            started?.Invoke();*/
            SetState(InvokeState.Begin);
        }
        protected virtual void OnBegin() => started?.Invoke();

        public virtual void DoTryCancel() => TryCancel();

        public virtual bool TryCancel()
        {
            if (isRunning)
            {
                Cancel();
                return true;
            }

            return false;
        }

        public virtual void Cancel()
        {
            //_isRunning = false;
            //cancelled?.Invoke();
            SetState(InvokeState.Cancel);
        }

        public virtual void DoTryComplete() => TryComplete();

        protected virtual void OnCancel() => cancelled?.Invoke();

        public virtual bool TryComplete()
        {
            if (isRunning)
            {
                Complete();
                return true;
            }

            return false;
        }

        public virtual void Complete()
        {
            //_isRunning = false;
            //completed?.Invoke();
            SetState(InvokeState.Complete);
        }

        protected virtual void OnComplete() => completed?.Invoke();

        private void SetState(InvokeState state)
        {
            if (state == InvokeState.NotInvoking || state == InvokeState.Invoking)
                return;

            if (_invokeState != InvokeState.NotInvoking)
            {
                _invokeState = state;
                return;
            }

            _invokeState = state;

            do
            {
                switch (_invokeState)
                {
                    case InvokeState.Begin:
                        if (isRunning)
                        {
                            _isRunning = false;
                            OnCancel();
                        }
                        else
                        {
                            _isRunning = true;
                            _invokeState = InvokeState.Invoking;
                            OnBegin();
                        }
                        break;

                    case InvokeState.Cancel:
                        if (isRunning)
                        {
                            _isRunning = false;
                            _invokeState = InvokeState.Invoking;
                            OnCancel();
                        }
                        else
                        {
                            _isRunning = true;
                            OnBegin();
                        }
                        break;

                    case InvokeState.Complete:
                        if (isRunning)
                        {
                            _isRunning = false;
                            _invokeState = InvokeState.Invoking;
                            OnComplete();
                        }
                        else
                        {
                            _isRunning = true;
                            OnBegin();
                        }
                        break;
                }

                // End invoking
                if (_invokeState == InvokeState.Invoking)
                    _invokeState = InvokeState.NotInvoking;

            } while (_invokeState != InvokeState.NotInvoking);
        }
    }
}