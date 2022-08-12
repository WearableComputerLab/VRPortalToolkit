using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Tasks
{

    [System.Flags]
    public enum TaskMask
    {
        Started = 1 << 0,
        Cancelled = 1 << 1,
        Completed = 1 << 2
    }

    public class TaskListener : MonoBehaviour
    {

        [SerializeField] private Task _source;
        public Task source {
            get => _source;
            set {
                if (_source != value)
                {
                    if (isActiveAndEnabled && Application.isPlaying)
                    {
                        UnsubscribeFromTask(source);
                        Validate.UpdateField(this, nameof(_source), _source = value);
                        SubscribeToTask(source);
                    }
                    else
                        Validate.UpdateField(this, nameof(_source), _source = value);
                }
            }
        }
        public void ClearSource() => source = null;

        [SerializeField] private Task _target;
        public Task target {
            get => _target;
            set => _target = value;
        }
        public void ClearTarget() => target = null;

        [Header("Listener Settings")]

        [SerializeField] private TargetMode _onSourceStarted = TargetMode.TryBegin;
        public TargetMode onSourceStarted {
            get => _onSourceStarted;
            set => _onSourceStarted = value;
        }

        [SerializeField] private TargetMode _onSourceCancelled = TargetMode.TryCancel;
        public TargetMode onSourceCancelled {
            get => _onSourceCancelled;
            set => _onSourceCancelled = value;
        }

        [SerializeField] private TargetMode _onSourceCompleted = TargetMode.TryComplete;
        public TargetMode onSourceCompleted {
            get => _onSourceCompleted;
            set => _onSourceCompleted = value;
        }

        public enum TargetMode
        {
            Ignore = 0,
            TryBegin = 1,
            Begin = 2,
            TryCancel = 3,
            Cancel = 4,
            TryComplete = 5,
            Complete = 6,
        }

        protected virtual void Reset()
        {
            Transform parent = transform.parent;
            while (parent != null)
            {
                source = parent.GetComponent<Task>();

                if (source) break;

                parent = parent.parent;
            }

            target = GetComponentInChildren<Task>();
        }

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_source), nameof(source));
        }

        protected virtual void OnEnable()
        {
            SubscribeToTask(source);
        }

        protected virtual void OnDisable()
        {
            UnsubscribeFromTask(source);
        }

        protected virtual void SubscribeToTask(Task source)
        {
            if (source)
            {
                source.started.AddListener(OnStarted);
                source.cancelled.AddListener(OnCancelled);
                source.completed.AddListener(OnCompleted);
            }
        }

        protected virtual void UnsubscribeFromTask(Task source)
        {
            if (source)
            {
                source.started.RemoveListener(OnStarted);
                source.cancelled.RemoveListener(OnCancelled);
                source.completed.RemoveListener(OnCompleted);
            }
        }

        protected virtual void OnStarted() => InvokeMode(onSourceStarted);

        protected virtual void OnCancelled() => InvokeMode(onSourceCancelled);

        protected virtual void OnCompleted() => InvokeMode(onSourceCompleted);

        protected virtual void InvokeMode(TargetMode mode)
        {
            if (target)
            {
                switch (mode)
                {
                    case TargetMode.TryBegin:
                        target.TryBegin();
                        break;
                    case TargetMode.Begin:
                        target.Begin();
                        break;
                    case TargetMode.TryCancel:
                        target.TryCancel();
                        break;
                    case TargetMode.Cancel:
                        target.Cancel();
                        break;
                    case TargetMode.TryComplete:
                        target.TryComplete();
                        break;
                    case TargetMode.Complete:
                        target.Complete();
                        break;
                }
            }
        }
    }
}