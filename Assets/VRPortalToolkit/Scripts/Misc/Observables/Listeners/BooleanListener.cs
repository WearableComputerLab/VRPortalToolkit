using Misc.EditorHelpers;
using Misc.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Observables
{
    public class BooleanListener : ObservableBoolean
    {
        [Space]
        [SerializeField] private List<ObservableBoolean> _sources = new List<ObservableBoolean>();
        public HeapAllocationFreeReadOnlyList<ObservableBoolean> readOnlySources => _sources;


        [SerializeField] private Mode _trueIf = Mode.AnyTrue;
        public Mode trueIf
        {
            get => _trueIf;
            set
            {
                if (value != trueIf)
                {
                    Validate.UpdateField(this, nameof(_trueIf), _trueIf = value);

                    if (isActiveAndEnabled && Application.isPlaying) Check(this.currentValue);
                }
            }
        }

        [SerializeField] private bool _inverted = false;
        public bool inverted
        {
            get => _inverted;
            set
            {
                if (value != _inverted)
                {
                    Validate.UpdateField(this, nameof(_inverted), _inverted = value);

                    if (isActiveAndEnabled && Application.isPlaying) Check(this.currentValue);
                }
            }
        }

        public enum Mode
        {
            AnyTrue = 0,
            AllTrue = 1,
            AllEqual = 2,
            MoreThanAverage = 3,
            AverageOrMore = 4,
            LastReceived = 5
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            Validate.FieldWithProperty(this, nameof(_trueIf), nameof(trueIf));
            Validate.FieldWithProperty(this, nameof(_inverted), nameof(inverted));
            Validate.FieldChanged(this, nameof(_sources), RemoveListeners, AddListeners);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            AddListeners();
            Check(currentValue);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            RemoveListeners();
        }

        public virtual void Add(ObservableBoolean source)
        {
            if (source)
            {
                if (isActiveAndEnabled && Application.isPlaying) AddListener(source);

                _sources.Add(source);

                Check(currentValue);
            }
        }

        public virtual void Remove(ObservableBoolean source)
        {
            if (isActiveAndEnabled && Application.isPlaying) RemoveListener(source);

            if (_sources.Remove(source))
                Check(currentValue);
        }

        public virtual void Clear()
        {
            if (_sources.Count > 0)
            {
                RemoveListeners();
                _sources.Clear();

                Check(currentValue);
            }
        }

        public virtual void AddRange(IEnumerable<ObservableBoolean> sources)
        {
            foreach (ObservableBoolean source in sources)
            {
                if (source)
                {
                    if (isActiveAndEnabled && Application.isPlaying) AddListener(source);

                    _sources.Add(source);
                }
            }

            Check(currentValue);
        }

        public virtual void Set(ObservableBoolean source)
        {
            RemoveListeners();
            _sources.Clear();

            if (source)
            {
                if (isActiveAndEnabled && Application.isPlaying) AddListener(source);

                _sources.Add(source);
            }

            Check(currentValue);
        }

        protected virtual void AddListeners()
        {
            foreach (ObservableBoolean observable in _sources)
                AddListener(observable);
        }

        protected virtual void AddListener(ObservableBoolean source)
        {
            if (source) source.modified.AddListener(Check);
        }

        protected virtual void RemoveListeners()
        {
            foreach (ObservableBoolean observable in _sources)
                RemoveListener(observable);
        }

        protected virtual void RemoveListener(ObservableBoolean source)
        {
            if (source) source.modified.RemoveListener(Check);
        }

        protected virtual void Check(bool found) // TODO trying to remove the need for this to have bool
        {
            switch (trueIf)
            {
                case Mode.AnyTrue:
                    currentValue = HasValue(true) ^ inverted;
                    return;
                case Mode.AllTrue:
                    currentValue = !HasValue(false) ^ inverted;
                    return;
                case Mode.AllEqual:
                    currentValue = (HasValue(false) != HasValue(true)) ^ inverted;
                    return;
                case Mode.MoreThanAverage:
                    currentValue = (TrueCount() > 0) ^ inverted;
                    return;
                case Mode.AverageOrMore:
                    currentValue = TrueCount() >= 0 ^ inverted;
                    return;
                default:
                    currentValue = found ^ inverted;
                    return;
            }
        }

        private bool HasValue(bool value)
        {
            foreach (ObservableBoolean source in _sources)
                if (source && source.currentValue == value)
                    return true;

            return false;
        }

        private int TrueCount()
        {
            int count = 0;

            foreach (ObservableBoolean source in _sources)
                if (source) count++;

            return count;
        }
    }
}
