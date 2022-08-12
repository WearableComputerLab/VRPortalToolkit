using Misc.EditorHelpers;
using UnityEngine;

namespace Misc.Tasks
{
    public class ConsecutiveTaskList : TaskList
    {
        [SerializeField] public int _currentTask;
        public int currentTask
        {
            get => _currentTask;
            set
            {
                if (_currentTask != value)
                {
                    if (isRunning)
                    {
                        CancelCurrent();
                        Validate.UpdateField(this, nameof(_currentTask), _currentTask = value);
                        ActualContinue();
                    }
                    else
                        Validate.UpdateField(this, nameof(_currentTask), _currentTask = value);
                }
            }
        }

        [SerializeField] public CancelMode _currentTaskCancelledMode = CancelMode.Cancel;
        public CancelMode currentTaskCancelledMode { get => _currentTaskCancelledMode; set => _currentTaskCancelledMode = value; }

        public enum CancelMode
        {
            Ignore = 0,
            Cancel = 1,
            Restart = 2,
            Continue = 3
        }

        protected Task current;

        protected override void OnValidate()
        {
            base.OnValidate();
            Validate.FieldWithProperty(this, nameof(_currentTask), nameof(currentTask));
        }

        public virtual void Begin(int index)
        {
            if (index < 0)
                _currentTask = 0;
            else
                _currentTask = index;

            base.Begin();
        }

        protected override void OnBegin()
        {
            base.OnBegin();

            ActualContinue();
        }

        public virtual bool TryContinue()
        {
            if ((!stateMode.HasFlag(StateMode.CancelOnDisabled) || isActiveAndEnabled) && !isRunning)
            {
                Continue();
                return true;
            }

            return false;
        }

        public virtual void Continue()
        {
            base.Begin();
        }

        // Assumes current is not subscribed to
        protected virtual void ActualContinue()
        {
            if (_currentTask < 0)
                _currentTask = 0;
            
            if (_subTasks == null)
                Complete();
            else
            {
                while (_currentTask < _subTasks.Count)
                {
                    current = _subTasks[_currentTask];

                    if (!current)
                        _currentTask++;
                    else
                    {
                        // Just in case
                        UnsubscribeFromCurrent();

                        if (current.isRunning)
                            current.TryCancel();

                        SubscribeToCurrent();
                        current.Begin();

                        return;
                    }
                }

                Complete();
            }
        }

        public override void Begin() => Begin(0);

        protected override void OnCancel()
        {
            CancelCurrent();
            base.OnCancel();
        }

        protected override void OnComplete()
        {
            // Just in case
            CancelCurrent();

            currentTask = 0;
            base.OnComplete();
        }

        public virtual void Previous(int count) => currentTask -= count;
        public virtual void Previous() => Previous(1);

        public virtual void Next(int count) => currentTask += count;

        public virtual void Next() => Next(1);

        protected virtual void SubscribeToCurrent()
        {
            if (current)
            {
                current.cancelled.AddListener(CurrentCancelled);
                current.completed.AddListener(CurrentCompleted);
            }
        }

        protected virtual void UnsubscribeFromCurrent()
        {
            if (current)
            {
                current.cancelled.RemoveListener(CurrentCancelled);
                current.completed.RemoveListener(CurrentCompleted);
            }
        }

        protected virtual void CancelCurrent()
        {
            if (current)
            {
                UnsubscribeFromCurrent();
                current.TryCancel();
                current = null;
            }
        }

        public virtual void CurrentCancelled()
        {
            if (current && !current.isRunning)
            {
                switch (_currentTaskCancelledMode)
                {
                    case CancelMode.Continue:
                        UnsubscribeFromCurrent();
                        ActualContinue();
                        break;

                    case CancelMode.Cancel:
                        UnsubscribeFromCurrent();
                        current = null;
                        Cancel();
                        break;

                    case CancelMode.Restart:
                        UnsubscribeFromCurrent();
                        current = null;
                        Begin();
                        break;

                    default:
                        break;
                }
            }
        }

        public virtual void CurrentCompleted()
        {
            if (current)
            {
                UnsubscribeFromCurrent();
                current = null;
            }

            currentTask++;
        }

        public override void AddSubTask(Task task)
        {
            base.AddSubTask(task);

            if (isRunning)
            {
                CancelCurrent();
                ActualContinue();
            }
        }

        public override void RemoveSubTask(Task task)
        {
            base.RemoveSubTask(task);

            if (isRunning)
            {
                CancelCurrent();
                ActualContinue();
            }
        }

        protected override void OnBeforeSubTasksChanged()
        {
            if (isRunning) CancelCurrent();
        }

        protected override void OnAfterSubTasksChanged()
        {
            if (isRunning) ActualContinue();
        }
    }
}