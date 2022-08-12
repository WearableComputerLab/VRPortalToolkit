using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Observables
{
    public class QuaternionListener : ObservableQuaternion
    {
        [Space]
        [SerializeField] private List<ObservableQuaternion> _sources = new List<ObservableQuaternion>();
        public HeapAllocationFreeReadOnlyList<ObservableQuaternion> readOnlySources => _sources;


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
            Combined = 1,
            LastReceived = 2
        }

        protected virtual void OnReset()
        {
            currentValue = Quaternion.identity;
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

        public virtual void Add(ObservableQuaternion source)
        {
            if (source)
            {
                if (isActiveAndEnabled && Application.isPlaying) AddListener(source);

                _sources.Add(source);

                Check(currentValue);
            }
        }

        public virtual void Remove(ObservableQuaternion source)
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

        public virtual void AddRange(IEnumerable<ObservableQuaternion> sources)
        {
            foreach (ObservableQuaternion source in sources)
            {
                if (source)
                {
                    if (isActiveAndEnabled && Application.isPlaying) AddListener(source);

                    _sources.Add(source);
                }
            }

            Check(currentValue);
        }

        public virtual void Set(ObservableQuaternion source)
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
            foreach (ObservableQuaternion source in _sources)
                AddListener(source);
        }

        protected virtual void AddListener(ObservableQuaternion source)
        {
            if (source) source.modified.AddListener(Check);
        }

        protected virtual void RemoveListeners()
        {
            foreach (ObservableQuaternion source in _sources)
                RemoveListener(source);
        }

        protected virtual void RemoveListener(ObservableQuaternion source)
        {
            if (source) source.modified.RemoveListener(Check);
        }

        protected virtual void Check(Quaternion found) // TODO trying to remove the need for this to have bool
        {
            switch (mode)
            {
                case Mode.Average:
                    {
                        float x = 0f, y = 0f, z = 0f, w = 0f, k;

                        // This average only works well for close rotations, but what ya gonna do :/
                        // https://gamedev.stackexchange.com/questions/119688/calculate-average-of-arbitrary-amount-of-quaternions-recursion

                        foreach (ObservableQuaternion source in _sources)
                        {
                            if (source)
                            {
                                Quaternion sourceRotation = source.currentValue;
                                x += sourceRotation.x;
                                y += sourceRotation.y;
                                z += sourceRotation.z;
                                w += sourceRotation.w;
                            }
                        }

                        k = 1.0f / Mathf.Sqrt(x * x + y * y + z * z + w * w);
                        Receive(new Quaternion(x * k, y * k, z * k, w * k));

                        return;
                    }
                case Mode.Combined:
                    {
                        Quaternion combined = Quaternion.identity;

                        foreach (ObservableQuaternion source in _sources)
                            if (source) combined *= source.currentValue;

                        currentValue = combined;
                        return;
                    }
                default:
                    currentValue = found;
                    return;
            }
        }

        private void Receive(Quaternion newValue)
        {
            if (normalized)
                currentValue = newValue.normalized;
            else
                currentValue = newValue;
        }
    }
}