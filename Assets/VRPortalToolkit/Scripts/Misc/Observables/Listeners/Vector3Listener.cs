using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Observables
{
    public class Vector3Listener : ObservableVector3
    {
        [Space]
        [SerializeField] private List<ObservableVector3> _sources = new List<ObservableVector3>();
        public HeapAllocationFreeReadOnlyList<ObservableVector3> readOnlySources => _sources;

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

        [SerializeField] private bool _normalized = false;
        public bool normalized
        {
            get => _normalized;
            set
            {
                if (value != _normalized)
                {
                    Validate.UpdateField(this, nameof(_normalized), _normalized = value);

                    if (isActiveAndEnabled && Application.isPlaying) Check(this.currentValue);
                }
            }
        }

        public enum Mode
        {
            Average = 0,
            Longest = 1,
            Shortest = 2,
            Sum = 3,
            Scaled = 4,
            LastReceived = 5
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            Validate.FieldWithProperty(this, nameof(_mode), nameof(mode));
            Validate.FieldWithProperty(this, nameof(_normalized), nameof(normalized));
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
            base.OnEnable();

            RemoveListeners();
        }

        public virtual void Add(ObservableVector3 source)
        {
            if (source)
            {
                if (isActiveAndEnabled && Application.isPlaying) AddListener(source);

                _sources.Add(source);

                Check(currentValue);
            }
        }

        public virtual void Remove(ObservableVector3 source)
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

        public virtual void AddRange(IEnumerable<ObservableVector3> sources)
        {
            foreach (ObservableVector3 source in sources)
            {
                if (source)
                {
                    if (isActiveAndEnabled && Application.isPlaying) AddListener(source);

                    _sources.Add(source);
                }
            }

            Check(currentValue);
        }

        public virtual void Set(ObservableVector3 source)
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
            foreach (ObservableVector3 source in _sources)
                AddListener(source);
        }

        protected virtual void AddListener(ObservableVector3 source)
        {
            if (source) source.modified.AddListener(Check);
        }

        protected virtual void RemoveListeners()
        {
            foreach (ObservableVector3 source in _sources)
                RemoveListener(source);
        }

        protected virtual void RemoveListener(ObservableVector3 source)
        {
            if (source) source.modified.RemoveListener(Check);
        }

        protected virtual void Check(Vector3 found) // TODO trying to remove the need for this to have bool
        {
            switch (mode)
            {
                case Mode.Average:
                    {
                        int count = 0;
                        Vector3 sum = Vector3.zero;

                        foreach (ObservableVector3 source in _sources)
                        {
                            if (source)
                            {
                                sum += source.currentValue;
                                count++;
                            }
                        }

                        if (count > 0)
                            Receive(sum / count);
                        else
                            Receive(defaultValue);

                        return;
                    }
                case Mode.Longest:
                    {
                        bool foundAny = false;
                        float magnitude = 0f, newMagnitude;
                        Vector3 longest = defaultValue;

                        foreach (ObservableVector3 source in _sources)
                        {
                            if (source)
                            {
                                newMagnitude = source.currentValue.magnitude;

                                if (!foundAny || newMagnitude > magnitude)
                                {
                                    longest = source.currentValue;
                                    foundAny = true;
                                    magnitude = newMagnitude;
                                }
                            }
                        }

                        Receive(longest);
                        return;
                    }
                case Mode.Shortest:
                    {
                        bool foundAny = false;
                        float magnitude = 0f, newMagnitude;
                        Vector3 shortest = defaultValue;

                        foreach (ObservableVector3 source in _sources)
                        {
                            if (source)
                            {
                                newMagnitude = source.currentValue.magnitude;

                                if (!foundAny || newMagnitude < magnitude)
                                {
                                    shortest = source.currentValue;
                                    foundAny = true;
                                    magnitude = newMagnitude;
                                }
                            }
                        }

                        Receive(shortest);
                        return;
                    }
                case Mode.Sum:
                    {
                        Vector3 sum = Vector3.zero;

                        foreach (ObservableVector3 source in _sources)
                            if (source) sum += source.currentValue;

                        Receive(sum);
                        return;
                    }
                case Mode.Scaled:
                    {
                        Vector2 scaled = Vector3.one;

                        foreach (ObservableVector3 source in _sources)
                            if (source) scaled = Vector3.Scale(scaled, source.currentValue);

                        Receive(scaled);
                        return;
                    }
                default:
                    Receive(found);
                    return;
            }
        }

        private void Receive(Vector3 newValue)
        {
            if (normalized)
            {
                if (newValue == Vector3.zero)
                    currentValue = defaultValue;
                else
                    currentValue = newValue.normalized;
            }
            else
                currentValue = newValue;
        }
    }
}