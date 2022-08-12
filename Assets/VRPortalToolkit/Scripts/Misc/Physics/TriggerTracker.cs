using Misc.EditorHelpers;
using Misc.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Physics
{
    public class TriggerTracker : TriggerHandler
    {
        [Header("Collider Events")]
        public SerializableEvent<Collider> colliderEntered = new SerializableEvent<Collider>();
        public SerializableEvent<Collider> colliderExited = new SerializableEvent<Collider>();

        [Header("Rigidbody Events")]
        public SerializableEvent<Rigidbody> rigidbodyEntered = new SerializableEvent<Rigidbody>();
        public SerializableEvent<Rigidbody> rigidbodyExited = new SerializableEvent<Rigidbody>();

        [Header("Container Events")]
        public SerializableEvent<Transform> containerEntered = new SerializableEvent<Transform>();
        public SerializableEvent<Transform> containerExited = new SerializableEvent<Transform>();

        public new int colliderCount => base.colliderCount;
        public new int rigidbodyCount => base.rigidbodyCount;
        public new int containerCount => base.containerCount;

        protected override void OnTriggerEnterCollider(Collider other)
            => colliderEntered?.Invoke(other);

        protected override void OnTriggerEnterRigidbody(Rigidbody other)
            => rigidbodyEntered?.Invoke(other);

        protected override void OnTriggerEnterContainer(Transform other)
            => containerEntered?.Invoke(other);

        protected override void OnTriggerExitCollider(Collider other)
            => colliderExited?.Invoke(other);

        protected override void OnTriggerExitRigidbody(Rigidbody other)
            => rigidbodyExited?.Invoke(other);

        protected override void OnTriggerExitContainer(Transform other)
            => containerExited?.Invoke(other);

        public new bool HasCollider(Collider collider) => base.HasCollider(collider);
        public new bool HasRigidbody(Rigidbody rigidbody) => base.HasRigidbody(rigidbody);
        public new bool HasContainer(Transform transform) => base.HasContainer(transform);

        public new IEnumerable<Collider> GetColliders() => base.GetColliders();
        public new IEnumerable<Rigidbody> GetRigidbody() => base.GetRigidbody();
        public new IEnumerable<Transform> GetContainer() => base.GetContainer();
    }

    public abstract class TriggerTracker<TComponent> : TriggerHandler<TComponent> where TComponent : Component
    {
        [Header("Trigger Events")]
        public UnityEvent<TComponent> triggerEntered = new UnityEvent<TComponent>();
        public UnityEvent<TComponent> triggerExited = new UnityEvent<TComponent>();

        [Header("Trigger Settings")]
        [SerializeField] private CollisionSourceMode _getComponentsSource = CollisionSourceMode.Container;
        public new CollisionSourceMode getComponentsSource {
            get => _getComponentsSource;
            set
            {
                if (_getComponentsSource != value)
                    Validate.UpdateField(this, nameof(_getComponentsSource), base.getComponentsSource = _getComponentsSource = value);
            }
        }

        [SerializeField] private GetComponentsMode _getComponentsMode = GetComponentsMode.GetComponent;
        public GetComponentsMode getComponentsMode {
            get => _getComponentsMode;
            set {
                if (_getComponentsMode != value)
                    Validate.UpdateField(this, nameof(_getComponentsMode), handler.getComponentsMode = _getComponentsMode = value);
            }
        }

        public int triggeredComponentCount => handler.componentCount;

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_getComponentsSource), nameof(getComponentsSource));
            Validate.FieldWithProperty(this, nameof(_getComponentsMode), nameof(getComponentsMode));
        }

        protected override void Awake()
        {
            base.Awake();
            base.getComponentsSource = getComponentsSource;
            handler.getComponentsMode = getComponentsMode;
        }

        protected override void OnTriggerEnterComponent(TComponent other)
            => triggerEntered?.Invoke(other);

        protected override void OnTriggerExitComponent(TComponent other)
            => triggerExited?.Invoke(other);

        public new bool HasTriggeredComponent(TComponent component) => base.HasTriggeredComponent(component);

        public new IEnumerable<TComponent> GetTriggeredComponents() => base.GetTriggeredComponents();
    }
}
