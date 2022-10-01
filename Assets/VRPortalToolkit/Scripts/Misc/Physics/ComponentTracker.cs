using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Physics
{
    public enum ExitMode
    {
        Never = 0,
        ComponentDisabled = 1,
        ComponentDestroyed = 2,
        SourceDisabled = 3,
        SourceDestroyed = 4,
        ComponentDisabledOrSourceDestroyed = 5,
        ComponentOrSourceDisabled = 6,
        ComponentDestroyedOrSourceDisabled = 7,
        ComponentOrSourceDestroyed = 8,
    }

    public abstract class ComponentTracker<TSource, TComponent> : MonoBehaviour where TSource : Component where TComponent : Component
    {
        private static readonly WaitForFixedUpdate WAIT_FOR_FIXED = new WaitForFixedUpdate();

        [Header("Component Events")]
        public UnityEvent<TComponent> componentEntered = new UnityEvent<TComponent>();
        public UnityEvent<TComponent> componentExited = new UnityEvent<TComponent>();

        [Header("Component Settings")]
        [SerializeField] private ExitMode _exitMode = ExitMode.ComponentOrSourceDisabled;
        protected ExitMode exitMode {
            get => _exitMode;
            set {
                if (_exitMode != value)
                {
                    Validate.UpdateField(this, nameof(_exitMode), _exitMode = value);
                    ApplyExitMode();
                }
            }
        }

        [SerializeField] private GetComponentsMode _getComponentsMode = GetComponentsMode.GetComponent;
        protected GetComponentsMode getComponentsMode {
            get => _getComponentsMode;
            set {
                if (_getComponentsMode != value)
                    Validate.UpdateField(this, nameof(_getComponentsMode), handler.getComponentsMode = _getComponentsMode = value);
            }
        }

        private IEnumerator _fixedCheckState;

        public int componentCount => handler.componentCount;

        protected ComponentHandler<TSource, TComponent> handler = new ComponentHandler<TSource, TComponent>();

        protected virtual void Awake()
        {
            handler.componentEntered = OnEnterComponent;
            handler.componentExited = OnExitComponent;
            handler.getComponentsMode = getComponentsMode;
            ApplyExitMode();
        }

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_exitMode), nameof(exitMode));
            Validate.FieldWithProperty(this, nameof(_getComponentsMode), nameof(getComponentsMode));
        }

        protected virtual void OnEnable()
        {
            handler.enabled = true;

            StartCoroutine(_fixedCheckState = FixedCheckState());
        }

        protected virtual void OnDisable()
        {
            handler.enabled = false;

            StopCoroutine(_fixedCheckState);
        }

        private IEnumerator FixedCheckState()
        {
            while (isActiveAndEnabled)
            {
                yield return WAIT_FOR_FIXED;

                CheckState();
            }
        }

        protected virtual void ApplyExitMode()
        {
            switch (exitMode)
            {
                case ExitMode.ComponentDisabled:
                    handler.exitOnSourceDisabled = handler.exitOnSourceDestroyed = false;
                    handler.exitOnComponentDisabled = handler.exitOnComponentDestroyed = true;
                    break;
                case ExitMode.ComponentDestroyed:
                    handler.exitOnComponentDisabled = handler.exitOnSourceDisabled = handler.exitOnSourceDestroyed = false;
                    handler.exitOnComponentDestroyed = true;
                    break;
                case ExitMode.SourceDisabled:
                    handler.exitOnComponentDisabled = handler.exitOnComponentDestroyed = false;
                    handler.exitOnSourceDisabled = handler.exitOnSourceDestroyed = true;
                    break;
                case ExitMode.SourceDestroyed:
                    handler.exitOnComponentDisabled = handler.exitOnSourceDisabled = handler.exitOnComponentDestroyed = false;
                    handler.exitOnSourceDestroyed = true;
                    break;
                case ExitMode.ComponentDisabledOrSourceDestroyed:
                    handler.exitOnSourceDisabled = false;
                    handler.exitOnComponentDisabled = handler.exitOnComponentDestroyed = handler.exitOnSourceDestroyed = true;
                    break;
                case ExitMode.ComponentOrSourceDisabled:
                    handler.exitOnSourceDisabled = handler.exitOnComponentDisabled = handler.exitOnComponentDestroyed = handler.exitOnSourceDestroyed = true;
                    break;
                case ExitMode.ComponentDestroyedOrSourceDisabled:
                    handler.exitOnSourceDisabled = false;
                    handler.exitOnComponentDisabled = handler.exitOnComponentDestroyed = handler.exitOnSourceDestroyed = true;
                    break;
                case ExitMode.ComponentOrSourceDestroyed:
                    handler.exitOnComponentDisabled = handler.exitOnSourceDisabled = false;
                    handler.exitOnComponentDestroyed = handler.exitOnSourceDestroyed = true;
                    break;
                default:
                    handler.exitOnComponentDisabled = handler.exitOnSourceDisabled = handler.exitOnComponentDestroyed = handler.exitOnSourceDestroyed = false;
                    break;
            }
        }

        public virtual void EnterSource(TSource source) => handler.EnterSource(source);

        public virtual void ExitSource(TSource source) => handler.ExitSource(source);

        public virtual void CheckState() => handler.ClearInvalid();

        public bool HasComponent(TComponent component) => handler.HasComponent(component);

        public IEnumerable<TComponent> GetComponents() => handler.GetComponents();

        protected virtual void OnEnterComponent(TComponent other) => componentEntered?.Invoke(other);

        protected virtual void OnExitComponent(TComponent other) => componentExited?.Invoke(other);
    }
}
