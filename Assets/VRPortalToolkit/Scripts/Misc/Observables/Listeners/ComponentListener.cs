using Misc.EditorHelpers;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Observables
{
    public class ComponentListener : ObservableComponent
    {
        [Space]
        [SerializeField] private List<ObservableComponent> _sources = new List<ObservableComponent>();
        public HeapAllocationFreeReadOnlyList<ObservableComponent> readOnlySources => _sources;


        [SerializeField] private Mode _mode = Mode.FirstFound;
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
            FirstFound = 0,
            LastFound = 1,
            AllEqual = 2,
            LeastCommon = 3, // Includes null
            MostCommon = 4,
            LeastFound = 5, // Does not includes null
            MostFound = 6,
            LastReceived = 7
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            Validate.FieldWithProperty(this, nameof(_mode), nameof(mode));
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

        public virtual void Add(ObservableComponent source)
        {
            if (source)
            {
                if (isActiveAndEnabled && Application.isPlaying) AddListener(source);

                _sources.Add(source);

                Check(currentValue);
            }
        }

        public virtual void Remove(ObservableComponent source)
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

        public virtual void AddRange(IEnumerable<ObservableComponent> sources)
        {
            foreach (ObservableComponent source in sources)
            {
                if (source)
                {
                    if (isActiveAndEnabled && Application.isPlaying) AddListener(source);

                    _sources.Add(source);
                }
            }

            Check(currentValue);
        }

        public virtual void Set(ObservableComponent source)
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
            foreach (ObservableComponent observable in _sources)
                AddListener(observable);
        }

        protected virtual void AddListener(ObservableComponent source)
        {
            if (source) source.modified.AddListener(Check);
        }

        protected virtual void RemoveListeners()
        {
            foreach (ObservableComponent observable in _sources)
                RemoveListener(observable);
        }

        protected virtual void RemoveListener(ObservableComponent source)
        {
            if (source) source.modified.RemoveListener(Check);
        }

        protected virtual void Check(Component found) // TODO trying to remove the need for this to have bool
        {
            switch (mode)
            {
                case Mode.FirstFound:
                    {
                        for (int i = 0; i < _sources.Count; i++)
                        {
                            ObservableComponent current = _sources[i];

                            if (current && current.currentValue)
                            {
                                currentValue = current.currentValue;
                                return;
                            }
                        }

                        currentValue = defaultValue;
                        break;
                    }
                case Mode.LastFound:
                    {
                        for (int i = _sources.Count - 1; i >= 0; i--)
                        {
                            ObservableComponent current = _sources[i];

                            if (current && current.currentValue)
                            {
                                currentValue = current.currentValue;
                                return;
                            }
                        }

                        currentValue = defaultValue;
                        break;
                    }
                case Mode.AllEqual:
                    {
                        bool foundFirst = false;
                        Component first = defaultValue;

                        for (int i = 0; i < _sources.Count; i++)
                        {
                            ObservableComponent source = _sources[i];

                            if (source)
                            {
                                if (!foundFirst)
                                {
                                    first = source.currentValue;
                                    foundFirst = true;
                                }
                                else if (first != _sources[i].currentValue)
                                {
                                    currentValue = defaultValue;
                                    return;
                                }
                            }
                        }

                        currentValue = first;
                    }
                    break;

                case Mode.LeastCommon:
                    {
                        UpdateCount();

                        KeyValuePair<Component, int> best = new KeyValuePair<Component, int>(defaultValue, int.MaxValue);

                        foreach (KeyValuePair<Component, int> pair in count)
                            if (pair.Value <= best.Value) best = pair;

                        currentValue = best.Key;
                        break;
                    }

                case Mode.MostCommon:
                    {
                        UpdateCount();

                        KeyValuePair<Component, int> best = new KeyValuePair<Component, int>(defaultValue, 0);

                        foreach (KeyValuePair<Component, int> pair in count)
                            if (pair.Value > best.Value) best = pair;

                        currentValue = best.Key;
                    }
                    break;

                case Mode.LeastFound:
                    {
                        UpdateCount();

                        KeyValuePair<Component, int> best = new KeyValuePair<Component, int>(defaultValue, int.MaxValue);

                        foreach (KeyValuePair<Component, int> pair in count)
                            if (pair.Key && pair.Value <= best.Value) best = pair;

                        currentValue = best.Key;
                        break;
                    }

                case Mode.MostFound:
                    {
                        UpdateCount();

                        KeyValuePair<Component, int> best = new KeyValuePair<Component, int>(defaultValue, 0);

                        foreach (KeyValuePair<Component, int> pair in count)
                            if (pair.Key && pair.Value > best.Value) best = pair;

                        currentValue = best.Key;
                    }
                    break;

                default:
                    currentValue = found;
                    return;
            }
        }

        private List<KeyValuePair<Component, int>> count;

        private void UpdateCount()
        {
            if (count == null) count = new List<KeyValuePair<Component, int>>(_sources.Count);
            else count.Clear();

            foreach (ObservableComponent source in _sources)
            {
                if (!source) continue;

                bool found = false;

                for (int i = 0; i < count.Count; i++)
                {
                    KeyValuePair<Component, int> current = count[i];

                    if (count[i].Key == source.currentValue)
                    {
                        count[i] = new KeyValuePair<Component, int>(source.currentValue, current.Value + 1);
                        found = true;
                        break;
                    }
                }

                if (!found) count.Add(new KeyValuePair<Component, int>(source.currentValue, 1));
            }
        }
    }
}
