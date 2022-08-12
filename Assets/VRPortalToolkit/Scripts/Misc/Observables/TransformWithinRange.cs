using Misc.Data;
using Misc.EditorHelpers;
using Misc.Events;
using Misc.Transformations;
using Misc.Update;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Observables
{
    public class TransformWithinRange : ObservableBoolean
    {
        [Header("Within Range")]

        [SerializeField] private UpdateMask _updateMask = new UpdateMask(UpdateFlags.FixedUpdate);
        public UpdateMask updateMask => _updateMask;
        protected Updater updater = new Updater();


        [Space]
        [SerializeField] private bool _valueIfWithin = true;
        public bool valueIfTrue { get; set; }

        [SerializeField] private TransformProperty _checkProperties = TransformProperty.PositionAndRotation;
        public virtual TransformProperty checkProperties { get => _checkProperties; set => _checkProperties = value; }

#if UNITY_EDITOR
        private bool hasPosition => checkProperties.HasFlag(TransformProperty.Position);

        [Space]
        [ShowIf(nameof(hasPosition))]
#endif
        [SerializeField] private FloatRange _positionRange;
        public FloatRange positionRange { get => _positionRange; set => _positionRange = value; }

#if UNITY_EDITOR
        private bool hasRotation => checkProperties.HasFlag(TransformProperty.Rotation);

        [ShowIf(nameof(hasRotation))]
#endif
        [SerializeField] private FloatRange _rotationRange;
        public FloatRange rotationRange { get => _rotationRange; set => _rotationRange = value; }

#if UNITY_EDITOR
        private bool hasScale => checkProperties.HasFlag(TransformProperty.Scale);


        [ShowIf(nameof(hasScale))]
#endif
        [SerializeField] private FloatRange _scaleRange;
        public FloatRange scaleRange { get => _scaleRange; set => _scaleRange = value; }

        [Header("Source")]
        [SerializeField] private Transform _source;
        public Transform source { get => _source; set => _source = value; }

        [SerializeField] private SpaceMode _sourceMode = SpaceMode.World;
        public virtual SpaceMode souceMode { get => _sourceMode; set => _sourceMode = value; }

        [Header("Target")]
        [SerializeField] private Transform _target;
        public Transform target { get => _target; set => _target = value; }

        [SerializeField] private SpaceMode _targetMode = SpaceMode.World;
        public virtual SpaceMode targetMode { get => _targetMode; set => _targetMode = value; }

        [Header("Events")]
        public SerializableEvent preUpdate = new SerializableEvent();
        public SerializableEvent postUpdate = new SerializableEvent();

        protected virtual void Reset()
        {
            target = transform;
        }

        protected virtual void Awake()
        {
            updater.updateMask = _updateMask;
            updater.onInvoke = ForceApply;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            updater.enabled = true;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            updater.enabled = false;
        }

        public virtual void Apply()
        {
            if (isActiveAndEnabled && Application.isPlaying && !updater.isUpdating) ForceApply();
        }

        public virtual void ForceApply()
        {
            preUpdate?.Invoke();

            if (source && target)
            {
                bool isWithinRange = true;

                if (checkProperties.HasFlag(TransformProperty.Position) && !positionRange.Contains(Vector3.Distance(GetTargetPosition(), GetSourcePosition())))
                    isWithinRange = false;
                else if (checkProperties.HasFlag(TransformProperty.Rotation) && !rotationRange.Contains(Quaternion.Angle(GetTargetRotation(), GetSourceRotation())))
                    isWithinRange = false;
                else if (checkProperties.HasFlag(TransformProperty.Scale) && !scaleRange.Contains(Vector3.Distance(GetTargetScale(), GetSourceScale())))
                    isWithinRange = false;

                currentValue = isWithinRange ^ !_valueIfWithin;
            }

            postUpdate?.Invoke();
        }

        protected virtual Vector3 GetSourcePosition() => souceMode == SpaceMode.Local ? source.localPosition : source.position;

        protected virtual Vector3 GetTargetPosition() => targetMode == SpaceMode.Local ? target.localPosition : target.position;

        protected virtual Quaternion GetSourceRotation() => souceMode == SpaceMode.Local ? source.localRotation : source.rotation;

        protected virtual Quaternion GetTargetRotation() => targetMode == SpaceMode.Local ? target.localRotation : target.rotation;

        protected virtual Vector3 GetSourceScale() => souceMode == SpaceMode.Local ? source.localScale : source.localScale;

        protected virtual Vector3 GetTargetScale() => targetMode == SpaceMode.Local ? target.localScale : target.localScale;
    }
}
