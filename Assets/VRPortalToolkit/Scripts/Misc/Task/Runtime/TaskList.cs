using Misc.EditorHelpers;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Tasks
{
    public abstract class TaskList : Task
    {
        [ContextMenuItem("Recalculate Sub Tasks", "RecalculateSubTasks")]
        [SerializeField] protected List<Task> _subTasks;

        public HeapAllocationFreeReadOnlyList<Task> ReadOnlySubTasks => _subTasks;

        protected virtual void Reset()
        {
            RecalculateSubTasks();
        }

        [ContextMenu("Recalculate Sub Tasks")]
        public virtual void RecalculateSubTasks()
        {
            ClearSubTasks();

            Task subTask;

            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).TryGetComponent(out subTask) && subTask.isActiveAndEnabled)
                    AddSubTask(subTask);
            }
        }

        protected virtual void OnValidate()
        {
            Validate.FieldChanged(this, nameof(_subTasks), OnBeforeSubTasksChanged, OnAfterSubTasksChanged);
        }

        public virtual void AddSubTask(Task task)
        {
            if (_subTasks == null) _subTasks = new List<Task>();

            _subTasks.Add(task);
        }

        public virtual void RemoveSubTask(Task task)
        {
            if (_subTasks == null) return;

            _subTasks.Remove(task);
        }

        public virtual void ClearSubTasks()
        {
            if (_subTasks == null) return;

            while (_subTasks.Count > 0)
                _subTasks.Remove(_subTasks[_subTasks.Count - 1]);
        }

        protected virtual void OnBeforeSubTasksChanged() { }
        protected virtual void OnAfterSubTasksChanged() { }
    }
}