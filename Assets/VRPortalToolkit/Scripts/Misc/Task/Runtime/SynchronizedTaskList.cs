using System.Collections.Generic;
using UnityEngine;

namespace Misc.Tasks
{
    public class SynchronizedTaskList : TaskList
    {
        public int completedCount => _completedSubTasks != null ? _completedSubTasks.Count : 0;

        [SerializeField] protected List<Task> _completedSubTasks;
        public HeapAllocationFreeReadOnlyList<Task> readOnlyCompletedSubTasks => _completedSubTasks;

        // TODO: I have a class to replace this
        //protected Dictionary<Task, UnityAction[]> _listeners = new Dictionary<Task, UnityAction[]>();

        protected ActionRemapper<Task> _completedMap = new ActionRemapper<Task>();
        protected ActionRemapper<Task> _cancelledMap = new ActionRemapper<Task>();

        [SerializeField] protected CancelMode _subTaskCancelledMode = CancelMode.Cancel;
        public CancelMode subTaskCancelledMode { get => _subTaskCancelledMode; set => _subTaskCancelledMode = value; }

        public enum CancelMode
        {
            Ignore = 0,
            Cancel = 1,
            RestartTask = 2,
            RestartRemainingTasks = 3,
            RestartAll = 4,
        }
        public virtual void Awake()
        {
            _completedMap.onInvoke = SubTaskCompleted;
            _completedMap.addListener = (action, subTask) => subTask.completed.AddListener(action.Invoke);
            _completedMap.removeListener = (action, subTask) => subTask.completed.RemoveListener(action.Invoke);
            _completedMap.StartListening();

            _cancelledMap.onInvoke = SubTaskCancelled;
            _cancelledMap.addListener = (action, subTask) => subTask.cancelled.AddListener(action.Invoke);
            _cancelledMap.removeListener = (action, subTask) => subTask.cancelled.RemoveListener(action.Invoke);
            _cancelledMap.StartListening();
        }

        public override void Begin()
        {
            ResetCompleted();

            base.Begin();
        }

        protected override void OnBegin()
        {
            base.OnBegin();

            SubscribeToSubTasks();
            StartSubTasks();
        }

        public virtual void ResetCompleted()
        {
            foreach (Task task in _completedSubTasks)
                AddSubTask(task);

            _completedSubTasks.Clear();
        }

        protected virtual void StartSubTasks()
        {
            // Done to prevent enumerator error if begin calls complete straight away
            Task task;
            for (int i = 0; i < _subTasks.Count; i++)
            {
                task = _subTasks[i];

                task.TryBegin();
                /*if (!task.isRunning)
                {
                    task.Begin();

                    // TODO: My begin and cancel could get called multiple times since task.Begin()...
                    if (!isRunning)
                        break;

                    // Assume completed if not running and move on
                    // TODO: Could cause problems
                    if (!task.isRunning) i--;
                }*/
            }
        }

        public virtual bool TryContinue()
        {
            if ((!stateMode.HasFlag(StateMode.CancelOnDisabled) || isActiveAndEnabled) && !isRunning)
            {
                Continue();
                return true;
            }

            return false;
            /*
            {
                isRunning = true;
                SubscribeToSubTasks();

                started?.Invoke();

                StartSubTasks();
            }*/
        }

        public virtual void Continue()
        {
            base.Begin();/*if (!isRunning)
            {
                isRunning = true;
                SubscribeToSubTasks();

                started?.Invoke();

                StartSubTasks();
            }*/
        }

        protected override void OnCancel()
        {
            UnsubscribeFromSubTasks();

            foreach (Task task in _subTasks)
                if (task.isRunning) task.Cancel();

            base.OnCancel();
        }

        protected override void OnComplete()
        {
            UnsubscribeFromSubTasks();

            base.Complete();
        }

        protected virtual void SubscribeToSubTasks()
        {
            if (_subTasks != null)
            {
                foreach (Task task in _subTasks)
                    SubscribeToSubTask(task);
            }
        }

        protected virtual void UnsubscribeFromSubTasks()
        {
            if (_subTasks != null)
            {
                foreach (Task task in _subTasks)
                    UnsubscribeFromSubTask(task);
            }
        }

        protected virtual void SubscribeToSubTask(Task task)
        {
            if (task)
            {
                _completedMap.AddSource(task);
                _cancelledMap.AddSource(task);
            }
        }

        protected virtual void UnsubscribeFromSubTask(Task task)
        {
            if (task)
            {
                _completedMap.RemoveSource(task);
                _cancelledMap.RemoveSource(task);
            }
        }

        protected virtual void SubTaskCancelled(Task task)
        {
            if (task && !task.isRunning)
            {
                switch (_subTaskCancelledMode)
                {
                    case CancelMode.Cancel:
                        Cancel();
                        break;

                    case CancelMode.RestartTask:
                        task.Begin();
                        break;

                    case CancelMode.RestartRemainingTasks:
                        foreach (Task other in _subTasks)
                            other.Begin();
                        break;

                    case CancelMode.RestartAll:
                        Begin();
                        break;

                    default:
                        break;
                }
            }
        }

        protected virtual void SubTaskCompleted(Task task)
        {
            if (_subTasks == null)
                Complete();
            else
            {
                _subTasks.Remove(task);
                _completedSubTasks.Add(task);

                if (_subTasks.Count == 0) Complete();
            }
        }

        public override void AddSubTask(Task task)
        {
            base.AddSubTask(task);

            if (isRunning) SubscribeToSubTask(task);
        }

        public override void RemoveSubTask(Task task)
        {
            base.RemoveSubTask(task);

            if (isRunning) UnsubscribeFromSubTask(task);
        }

        protected override void OnBeforeSubTasksChanged()
        {
            if (isRunning)
            {
                UnsubscribeFromSubTasks();

                // Could cancel all tasks, but dont think it should
                //foreach (Task task in SubTasks)
                //    if (task.IsRunning) task.Cancel();
            }
        }

        protected override void OnAfterSubTasksChanged()
        {
            if (isRunning)
            {
                SubscribeToSubTasks();

                foreach (Task task in _subTasks)
                    if (!task.isRunning) task.Begin();
            }
        }
    }
}