using Misc.EditorHelpers;
using Misc.Events;
using Misc.Update;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Observables
{
    public abstract class Observable : MonoBehaviour { }

    public abstract class Observable<TValue> : Observable
    {
        [Foldout]
        [SerializeField] private TValue _currentValue;
        public virtual TValue currentValue
        {
            get => _currentValue;
            set
            {
                if (!isActiveAndEnabled && onDisabled.HasFlag(DisabledMode.IgnoreChanges))
                    return;

                if (!IsValueEqual(value))
                {
                    Validate.UpdateField(this, nameof(_currentValue), _currentValue = value);

                    InvokeModified();
                }
            }
        }

        [ExpandWith(nameof(_currentValue))]
        [SerializeField] private TValue _defaultValue;
        public virtual TValue defaultValue { get => _defaultValue; set => _defaultValue = value; }

        [ExpandWith(nameof(_currentValue))]
        [SerializeField] private DisabledMode _onDisabled = DisabledMode.None;
        public virtual DisabledMode onDisabled { get => _onDisabled; set => _onDisabled = value; }

        [System.Flags]
        public enum DisabledMode
        {
            None = 0,
            SetDefault = 1 << 0,
            IgnoreChanges = 1 << 1,
            DisableEvents = 1 << 2
        }

        public SerializableEvent<TValue> modified = new SerializableEvent<TValue>();

        public bool isDefaultValue => IsValueEqual(defaultValue);

        protected virtual void OnValidate()
        {
            // Should always be able to edit in editor
            if (isActiveAndEnabled && Application.isPlaying && onDisabled.HasFlag(DisabledMode.IgnoreChanges))
                Validate.UpdateField(this, nameof(_currentValue), _currentValue);
            else
                Validate.FieldWithProperty(this, nameof(_currentValue), nameof(currentValue));
        }

        protected virtual void OnEnable()
        {

        }

        protected virtual void OnDisable()
        {
            if (onDisabled.HasFlag(DisabledMode.SetDefault))
            {
                if (onDisabled.HasFlag(DisabledMode.IgnoreChanges))
                {
                    if (!IsValueEqual(defaultValue))
                    {
                        Validate.UpdateField(this, nameof(_currentValue), _currentValue = defaultValue);

                        InvokeModified();
                    }
                }
                else currentValue = defaultValue;
            }
        }

        /*protected virtual void DelayedModified()
        {
            if (Application.isPlaying)
            {
                if (!isActiveAndEnabled && onDisabled.HasFlag(DisabledMode.DisableEvents))
                    return;

                if (delay.UpdateFlags != UpdateFlags.Never)
                {
                    if (!delayedUpdater.enabled)
                    {
                        delayedUpdater.updateMask = delay;
                        delayedUpdater.onInvoke = InvokeModified;
                        delayedUpdater.enabled = true;
                    }
                }
                else InvokeModified();
            }
        }*/

        protected virtual void InvokeModified()
        {
            if (Application.isPlaying)
            {
                if (!isActiveAndEnabled && onDisabled.HasFlag(DisabledMode.DisableEvents))
                    return;

                modified?.Invoke(currentValue);
                //delayedUpdater.enabled = false;
            }
        }

        protected virtual bool IsValueEqual(TValue other)
            => EqualityComparer<TValue>.Default.Equals(currentValue, other);
    }
}
