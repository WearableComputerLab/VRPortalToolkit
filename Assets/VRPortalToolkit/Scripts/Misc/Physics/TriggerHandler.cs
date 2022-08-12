using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Physics
{
    // This is designed to handle components being destroyed, disabled, and hierachy changing
    public abstract class TriggerHandler : MonoBehaviour
    {
        private static WaitForFixedUpdate _fixedUpdateInstruction = new WaitForFixedUpdate();

        private IEnumerator _waitForFixedUpdateLoop;

        private Dictionary<Component, TriggerInfo> _activeTriggers = new Dictionary<Component, TriggerInfo>();
        private List<Component> _toBeRemoved = new List<Component>();

        private int _colliderCount = 0;
        protected int colliderCount => isActiveAndEnabled ? _colliderCount : 0;

        private int _rigidbodyCount = 0;
        protected int rigidbodyCount => isActiveAndEnabled ? _rigidbodyCount : 0;

        private int _containerCount = 0;
        protected int containerCount => isActiveAndEnabled ? _containerCount : 0;

        private class TriggerInfo
        {
            public double lastActivated = float.NegativeInfinity;

            public Component component;
        }

        protected virtual void OnEnable()
        {
            if (_activeTriggers.Count > 0)
            {
                Collider collider;
                Transform transform;
                Rigidbody rigidbody;

                double currentTime = Time.fixedTimeAsDouble;

                foreach (TriggerInfo info in _activeTriggers.Values)
                    if (info.lastActivated < currentTime) _toBeRemoved.Add(info.component);

                RemoveComponentsWithoutEvents(_toBeRemoved);

                foreach (Component component in _activeTriggers.Keys)
                {
                    if ((collider = component as Collider) != null)
                        OnTriggerEnterCollider(collider);
                    else if ((transform = component as Transform) != null)
                        OnTriggerEnterContainer(transform);
                    else if ((rigidbody = component as Rigidbody) != null)
                        OnTriggerEnterRigidbody(rigidbody);
                }
            }

            if (_waitForFixedUpdateLoop == null) _waitForFixedUpdateLoop = WaitForFixedUpdateEnumerator();
            StartCoroutine(_waitForFixedUpdateLoop);
        }

        protected virtual void OnDisable()
        {
            if (_activeTriggers.Count > 0)
            {
                Collider collider;
                Transform transform;
                Rigidbody rigidbody;

                foreach (Component component in _activeTriggers.Keys)
                {
                    if ((collider = component as Collider) != null)
                        OnTriggerExitCollider(collider);
                    else if ((transform = component as Transform) != null)
                        OnTriggerExitContainer(transform);
                    else if ((rigidbody = component as Rigidbody) != null)
                        OnTriggerExitRigidbody(rigidbody);
                }
            }

            StopCoroutine(_waitForFixedUpdateLoop);
        }

        protected virtual void OnTriggerEnter(Collider other) => OnTrigger(other);

        protected virtual void OnTriggerStay(Collider other) => OnTrigger(other);

        private void OnTrigger(Collider other)
        {
            if (other == null) return;

            if (AddComponent(other) && isActiveAndEnabled) OnTriggerEnterCollider(other);

            if (other.attachedRigidbody)
            {
                if (AddComponent(other.attachedRigidbody))
                {
                    _rigidbodyCount++;

                    if (isActiveAndEnabled) OnTriggerEnterRigidbody(other.attachedRigidbody);
                }

                if (AddComponent(other.attachedRigidbody.transform))
                {
                    _containerCount++;

                    if (isActiveAndEnabled) OnTriggerEnterContainer(other.attachedRigidbody.transform);
                }
            }
            else if (AddComponent(other.transform))
            {
                _containerCount++;

                if (isActiveAndEnabled) OnTriggerEnterContainer(other.transform);
            }
        }

        protected virtual void OnTriggerEnterCollider(Collider other) { }
        protected virtual void OnTriggerEnterRigidbody(Rigidbody other) { }
        protected virtual void OnTriggerEnterContainer(Transform other) { }

        protected virtual void OnTriggerExitCollider(Collider other) { }
        protected virtual void OnTriggerExitRigidbody(Rigidbody other) { }
        protected virtual void OnTriggerExitContainer(Transform other) { }

        private bool AddComponent(Component component)
        {
            if (_activeTriggers.TryGetValue(component, out TriggerInfo info))
            {
                info.lastActivated = Time.fixedTimeAsDouble;
                return false;
            }

            _activeTriggers.Add(component, new TriggerInfo() {
                component = component,
                lastActivated = Time.fixedTimeAsDouble
        });
            return true;
        }

        private IEnumerator WaitForFixedUpdateEnumerator()
        {
            while (true)
            {
                yield return _fixedUpdateInstruction;

                WaitForFixedUpdate();
            }
        }

        protected virtual void WaitForFixedUpdate()
        {
            _toBeRemoved.Clear();

            double currentTime = Time.fixedTimeAsDouble;

            foreach (TriggerInfo info in _activeTriggers.Values)
                if (info.lastActivated < currentTime) _toBeRemoved.Add(info.component);

            RemoveComponents(_toBeRemoved);
        }

        private void RemoveComponents(List<Component> toBeRemoved)
        {
            if (toBeRemoved.Count > 0)
            {
                Collider collider;
                Transform transform;
                Rigidbody rigidbody;

                foreach (Component component in toBeRemoved)
                {
                    if (_activeTriggers.Remove(component))
                    {
                        if ((collider = component as Collider) != null)
                        {
                            _colliderCount--;
                            OnTriggerExitCollider(collider);
                        }
                        else if ((transform = component as Transform) != null)
                        {
                            _containerCount--;
                            OnTriggerExitContainer(transform);
                        }
                        else if ((rigidbody = component as Rigidbody) != null)
                        {
                            _rigidbodyCount--;
                            OnTriggerExitRigidbody(rigidbody);
                        }
                    }
                }
            }
        }

        private void RemoveComponentsWithoutEvents(List<Component> toBeRemoved)
        {
            if (toBeRemoved.Count > 0)
            {
                Collider collider;
                Transform transform;
                Rigidbody rigidbody;

                foreach (Component component in toBeRemoved)
                {
                    if (_activeTriggers.Remove(component))
                    {
                        if ((collider = component as Collider) != null)
                            _colliderCount--;
                        else if ((transform = component as Transform) != null)
                            _containerCount--;
                        else if ((rigidbody = component as Rigidbody) != null)
                            _rigidbodyCount--;
                    }
                }
            }
        }

        protected bool HasCollider(Collider collider) => HasComponent(collider);
        protected bool HasRigidbody(Rigidbody rigidbody) => HasComponent(rigidbody);
        protected bool HasContainer(Transform transform) => HasComponent(transform);

        private bool HasComponent(Component component) => isActiveAndEnabled && _activeTriggers.ContainsKey(component);

        protected IEnumerable<Collider> GetColliders() => GetTriggered<Collider>();
        protected IEnumerable<Rigidbody> GetRigidbody() => GetTriggered<Rigidbody>();
        protected IEnumerable<Transform> GetContainer() => GetTriggered<Transform>();

        private IEnumerable<T> GetTriggered<T>() where T : Component
        {
            if (isActiveAndEnabled)
            {
                T asT;

                foreach (Component component in _activeTriggers.Keys)
                {
                    if ((asT = component as T) != null)
                        yield return asT;
                }
            }
        }
    }

    public enum CollisionSourceMode
    {
        Collider = 0,
        Rigidbody = 1,
        Container = 2,
    }

    public enum GetComponentsMode
    {
        None = 0,
        GetComponent = 1,
        GetComponentInChildren = 2,
        GetComponentInParent = 3,
        GetComponents = 4,
        GetComponentsInChildren = 5,
        GetComponentsInParent = 6
    }

    // TODO: This is getting annoyingly complicated.

    // The value of this is that on destroyed components, you can't use GetComponent<T> on transform
    public abstract class TriggerHandler<TComponent> : TriggerHandler where TComponent : Component
    {
        private CollisionSourceMode _getComponentsSource = CollisionSourceMode.Container;
        protected CollisionSourceMode getComponentsSource {
            get => _getComponentsSource;
            set
            {
                if (_getComponentsSource != value)
                {
                    _getComponentsSource = value;
                    ResetComponents();
                }
            }
        }

        protected ComponentHandler<Component, TComponent> handler = new ComponentHandler<Component, TComponent>();

        private class TriggerInfo
        {
            public bool isActiveAndEnabled;

            public TComponent component;

            public int sourcesCount = 1;
        }

        protected virtual void Awake()
        {
            handler.componentEntered = OnTriggerEnterComponent;
            handler.componentExited = OnTriggerExitComponent;
            handler.enabled = true;
        }

        protected override void OnTriggerEnterCollider(Collider other)
        {
            base.OnTriggerEnterCollider(other);

            if (getComponentsSource == CollisionSourceMode.Collider)
                handler.EnterSource(other);
        }

        protected override void OnTriggerEnterRigidbody(Rigidbody other)
        {
            base.OnTriggerEnterRigidbody(other);

            if (getComponentsSource == CollisionSourceMode.Rigidbody)
                handler.EnterSource(other);
        }

        protected override void OnTriggerEnterContainer(Transform other)
        {
            base.OnTriggerEnterContainer(other);

            if (getComponentsSource == CollisionSourceMode.Container)
                handler.EnterSource(other);
        }

        protected virtual void OnTriggerEnterComponent(TComponent other) { }

        protected override void OnTriggerExitCollider(Collider other)
        {
            base.OnTriggerEnterCollider(other);

            if (getComponentsSource == CollisionSourceMode.Collider)
                handler.ExitSource(other);
        }

        protected override void OnTriggerExitRigidbody(Rigidbody other)
        {
            base.OnTriggerEnterRigidbody(other);

            if (getComponentsSource == CollisionSourceMode.Rigidbody)
                handler.ExitSource(other);
        }

        protected override void OnTriggerExitContainer(Transform other)
        {
            base.OnTriggerEnterContainer(other);

            if (getComponentsSource == CollisionSourceMode.Container)
                handler.ExitSource(other);
        }

        protected virtual void OnTriggerExitComponent(TComponent other) { }

        protected override void WaitForFixedUpdate()
        {
            handler.ClearInvalid();

            base.WaitForFixedUpdate();
        }

        protected bool HasTriggeredComponent(TComponent component) => handler.HasComponent(component);

        protected IEnumerable<TComponent> GetTriggeredComponents() => handler.GetComponents();

        protected virtual void ResetComponents()
        {
            handler.Clear();

            switch (getComponentsSource)
            {
                case CollisionSourceMode.Collider:
                    foreach (Collider collider in GetColliders())
                        handler.EnterSource(collider);
                    break;

                case CollisionSourceMode.Rigidbody:
                    foreach (Rigidbody rigidbody in GetRigidbody())
                        handler.EnterSource(rigidbody);
                    break;

                case CollisionSourceMode.Container:
                    foreach (Transform container in GetContainer())
                        handler.EnterSource(container);
                    break;
            }
        }
    }
}
