using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Observables
{
    public class FloatListener : ObservableFloat
    {
        [Space]
        [SerializeField] private List<ObservableFloat> _sources = new List<ObservableFloat>();
        public HeapAllocationFreeReadOnlyList<ObservableFloat> readOnlySources => _sources;


        [SerializeField] private Mode _mode = Mode.Average;
        public Mode mode
        {
            get => _mode;
            set
            {
                if (value != mode)
                {
                    Validate.UpdateField(this, nameof(_mode), _mode = value);

                    if (isActiveAndEnabled && Application.isPlaying) Check(this.currentValue);
                }
            }
        }

        public enum Mode
        {
            Average = 0,
            Highest = 1,
            Lowest = 2,
            Sum = 3,
            LastReceived = 4
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            Validate.FieldWithProperty(this, nameof(_mode), nameof(mode));
            Validate.FieldChanged(this, nameof(_sources), RemoveListeners, AddListeners);
        }

        protected override void OnEnable()
        {
            base.OnDisable();

            AddListeners();
            Check(currentValue);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            RemoveListeners();
        }

        public virtual void Add(ObservableFloat source)
        {
            if (source)
            {
                if (isActiveAndEnabled && Application.isPlaying) AddListener(source);

                _sources.Add(source);

                Check(currentValue);
            }
        }

        public virtual void Remove(ObservableFloat source)
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

        public virtual void AddRange(IEnumerable<ObservableFloat> sources)
        {
            foreach (ObservableFloat source in sources)
            {
                if (source)
                {
                    if (isActiveAndEnabled && Application.isPlaying) AddListener(source);

                    _sources.Add(source);
                }
            }

            Check(currentValue);
        }

        public virtual void Set(ObservableFloat source)
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
            foreach (ObservableFloat source in _sources)
                AddListener(source);
        }

        protected virtual void AddListener(ObservableFloat source)
        {
            if (source) source.modified.AddListener(Check);
        }

        protected virtual void RemoveListeners()
        {
            foreach (ObservableFloat source in _sources)
                RemoveListener(source);
        }

        protected virtual void RemoveListener(ObservableFloat source)
        {
            if (source) source.modified.RemoveListener(Check);
        }

        protected virtual void Check(float found) // TODO trying to remove the need for this to have bool
        {
            switch (mode)
            {
                case Mode.Average:
                    {
                        int count = 0;
                        float sum = 0f;

                        foreach (ObservableFloat source in _sources)
                        {
                            if (source)
                            {
                                sum += source.currentValue;
                                count++;
                            }
                        }

                        if (count > 0)
                            currentValue = sum / count;
                        else
                            currentValue = defaultValue;

                        return;
                    }
                case Mode.Highest:
                    {
                        bool foundAny = false;
                        float highest = defaultValue;

                        foreach (ObservableFloat source in _sources)
                        {
                            if (source && (!foundAny || source.currentValue > highest))
                            {
                                highest = source.currentValue;
                                foundAny = true;
                            }
                        }

                        currentValue = highest;
                        return;
                    }
                case Mode.Lowest:
                    {
                        bool foundAny = false;
                        float lowest = defaultValue;

                        foreach (ObservableFloat source in _sources)
                        {
                            if (source && (!foundAny || source.currentValue < lowest))
                            {
                                lowest = source.currentValue;
                                foundAny = true;
                            }
                        }

                        currentValue = lowest;
                        return;
                    }
                case Mode.Sum:
                    {
                        float sum = 0;

                        foreach (ObservableFloat source in _sources)
                            if (source) sum += source.currentValue;

                        currentValue = sum;
                        return;
                    }
                default:
                    currentValue = found;
                    return;
            }
        }
    }
}