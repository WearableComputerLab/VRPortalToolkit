using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Observables
{
    public class Vector2Listener : ObservableVector2
    {
        [Space]
        [SerializeField] private List<ObservableVector2> _sources = new List<ObservableVector2>();
        public HeapAllocationFreeReadOnlyList<ObservableVector2> readOnlySources => _sources;

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

        // TODO: Could add scaled?
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
            base.OnDisable();

            RemoveListeners();
        }

        public virtual void Add(ObservableVector2 source)
        {
            if (source)
            {
                if (isActiveAndEnabled && Application.isPlaying) AddListener(source);

                _sources.Add(source);

                Check(currentValue);
            }
        }

        public virtual void Remove(ObservableVector2 source)
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

        public virtual void AddRange(IEnumerable<ObservableVector2> sources)
        {
            foreach (ObservableVector2 source in sources)
            {
                if (source)
                {
                    if (isActiveAndEnabled && Application.isPlaying) AddListener(source);

                    _sources.Add(source);
                }
            }

            Check(currentValue);
        }

        public virtual void Set(ObservableVector2 source)
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
            foreach (ObservableVector2 source in _sources)
                AddListener(source);
        }

        protected virtual void AddListener(ObservableVector2 source)
        {
            if (source) source.modified.AddListener(Check);
        }

        protected virtual void RemoveListeners()
        {
            foreach (ObservableVector2 source in _sources)
                RemoveListener(source);
        }

        protected virtual void RemoveListener(ObservableVector2 source)
        {
            if (source) source.modified.RemoveListener(Check);
        }

        protected virtual void Check(Vector2 found) // TODO trying to remove the need for this to have bool
        {
            switch (mode)
            {
                case Mode.Average:
                    {
                        int count = 0;
                        Vector2 sum = Vector2.zero;

                        foreach (ObservableVector2 source in _sources)
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
                            Receive(Vector2.zero);

                        return;
                    }
                case Mode.Longest:
                    {
                        bool foundAny = false;
                        float magnitude = 0f, newMagnitude;
                        Vector2 longest = defaultValue;

                        foreach (ObservableVector2 source in _sources)
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
                        Vector2 shortest = defaultValue;

                        foreach (ObservableVector2 source in _sources)
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
                        Vector2 sum = Vector2.zero;

                        foreach (ObservableVector2 source in _sources)
                            if (source) sum += source.currentValue;

                        Receive(sum);
                        return;
                    }
                case Mode.Scaled:
                    {
                        Vector2 scaled = Vector2.one;

                        foreach (ObservableVector2 source in _sources)
                            if (source) scaled = Vector2.Scale(scaled, source.currentValue);

                        Receive(scaled);
                        return;
                    }
                default:
                    Receive(found);
                    return;
            }
        }

        private void Receive(Vector2 newValue)
        {
            if (normalized)
            {
                if (newValue == Vector2.zero)
                    currentValue = defaultValue;
                else
                    currentValue = newValue.normalized;
            }
            else
                currentValue = newValue;
        }
    }
}