using Misc.EditorHelpers;
using Misc.Events;
using Misc.Update;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Transformations
{
    public class TransformFollower : MonoBehaviour
    {
        [SerializeField] private UpdateMask _updateMask = new UpdateMask(UpdateFlags.FixedUpdate);
        public UpdateMask updateMask => _updateMask;
        protected Updater updater = new Updater();

        [SerializeField] private TransformProperty _properties = TransformProperty.PositionAndRotation;
        public virtual TransformProperty properties { get => _properties; set => _properties = value; }

        [SerializeField] private Transition _transition = new Transition();
        public Transition transition => _transition;
        protected TimeStep timeStep = new TimeStep();

        [Header("Source")]
        [SerializeField] private Transform _source;
        public virtual Transform source { get => _source; set => _source = value; }

        [SerializeField] private SpaceMode _sourceMode = SpaceMode.World;
        public virtual SpaceMode sourceSpace
        { get => _sourceMode; set => _sourceMode = value; }

#if UNITY_EDITOR
        private bool hasPosition => properties.HasFlag(TransformProperty.Position);

        [Space]
        [ShowIf(nameof(hasPosition))]
#endif
        [SerializeField] private Vector3 _sourcePosition;
        public Vector3 sourcePosition { get => _sourcePosition; set => _sourcePosition = value; }

#if UNITY_EDITOR
        private bool hasRotation => properties.HasFlag(TransformProperty.Rotation);

        [ShowIf(nameof(hasRotation))]
#endif
        [SerializeField] private Quaternion _sourceRotation;
        public Quaternion sourceRotation { get => _sourceRotation; set => _sourceRotation = value; }
        public Vector3 sourceEulerAngles { get => _sourceRotation.eulerAngles; set => _sourceRotation = Quaternion.Euler(value); }

#if UNITY_EDITOR
        private bool hasScale => properties.HasFlag(TransformProperty.Scale);


        [ShowIf(nameof(hasScale))]
#endif
        [SerializeField] private Vector3 _sourceScale = Vector3.one;
        public Vector3 sourceScale { get => _sourceScale; set => _sourceScale = value; }

        [Header("Target")]
        [SerializeField] private Transform _target;
        public Transform target { get => _target; set => _target = value; }

        [SerializeField] private SpaceMode _targetSpace = SpaceMode.World;
        public virtual SpaceMode targetSpace { get => _targetSpace; set => _targetSpace = value; }

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

        protected virtual void OnEnable()
        {
            timeStep.UpdateStep(transition.timeUnit);
            updater.enabled = true;
        }

        protected virtual void OnDisable()
        {
            updater.enabled = false;
        }

        [ContextMenu("Calculate Offset")]
        public void CalculateOffset()
        {
            // TODO : Need to backwards engineer so that the current space
            if (target && source)
            {
                Vector3 targetPosition, targetScale;
                Quaternion targetRotation;

                if (targetSpace == SpaceMode.Local)
                {
                    targetPosition = target.localPosition;
                    targetRotation = target.localRotation;
                    targetScale = target.localScale;
                }
                else
                {
                    targetPosition = target.position;
                    targetRotation = target.rotation;
                    targetScale = target.localScale; // Does not set
                }

                if (sourceSpace == SpaceMode.Local)
                {
                    sourcePosition = targetPosition - source.localPosition;
                    sourceRotation = Quaternion.Inverse(source.localRotation) * targetRotation;
                }
                else
                {
                    sourcePosition = source.InverseTransformPoint(targetPosition);
                    sourceRotation = Quaternion.Inverse(source.rotation) * targetRotation;
                }

                sourceScale = new Vector3(source.localScale.x != 0f ? targetScale.x / source.localScale.x : 0f,
                    source.localScale.y != 0f ? targetScale.y / source.localScale.y : 0f,
                    source.localScale.z != 0f ? targetScale.z / source.localScale.z : 0f);
            }
            else
            {
                sourcePosition = Vector3.zero;
                sourceRotation = Quaternion.identity;
                sourceScale = Vector3.one;
            }
        }

        public virtual void Apply()
        {
            if (isActiveAndEnabled && Application.isPlaying && !updater.isUpdating) ForceApply();
        }

        public virtual void ForceApply()
        {
            preUpdate?.Invoke();

            float step = timeStep.UpdateStep(transition.timeUnit);

            if (source && target)
            {
                if (properties.HasFlag(TransformProperty.Scale))
                    ApplyScale(step);

                if (properties.HasFlag(TransformProperty.Rotation))
                    ApplyRotation(step);

                if (properties.HasFlag(TransformProperty.Position))
                    ApplyPosition(step);
            }

            postUpdate?.Invoke();
        }

        protected virtual void ApplyPosition(float timeStep) => SetTargetPosition(GetSourcePosition(), timeStep);

        protected virtual Vector3 GetSourcePosition()
        {
            if (sourceSpace == SpaceMode.Local)
                return source.localPosition + sourcePosition;

            return source.TransformPoint(sourcePosition);
        }

        protected virtual void SetTargetPosition(Vector3 sourcePosition, float timeStep)
        {
            if (sourceSpace == SpaceMode.Local)
            {
                StepPosition(target.localPosition, ref sourcePosition, timeStep);
                target.localPosition = sourcePosition;
            }
            else
            {
                StepPosition(target.position, ref sourcePosition, timeStep);
                target.position = sourcePosition;
            }
        }

        protected virtual void StepPosition(Vector3 from, ref Vector3 to, float timeStep)
            => transition.StepPosition(from, ref to, timeStep);

        protected virtual void ApplyRotation(float timeStep) => SetTargetRotation(GetSourceRotation(), timeStep);

        protected virtual Quaternion GetSourceRotation()
        {
            if (sourceSpace == SpaceMode.Local)
                return source.localRotation * sourceRotation;

            return source.rotation * sourceRotation;
        }

        protected virtual void SetTargetRotation(Quaternion sourceRotation, float timeStep)
        {
            if (sourceSpace == SpaceMode.Local)
            {
                StepRotation(target.localRotation, ref sourceRotation, timeStep);
                target.localRotation = sourceRotation;
            }
            else
            {
                StepRotation(target.rotation, ref sourceRotation, timeStep);
                target.rotation = sourceRotation;
            }
        }

        protected virtual void StepRotation(Quaternion from, ref Quaternion to, float timeStep)
            => transition.StepRotation(from, ref to, timeStep);

        protected virtual void ApplyScale(float timeStep) => SetTargetScale(GetSourceScale(), timeStep);

        protected virtual Vector3 GetSourceScale()
        {
            //if (sourceSpace == SpaceMode.Local)
            return Vector3.Scale(source.localScale, _sourceScale);

            //return Vector3.Scale(source.lossyScale, _sourceScaleOffset);
        }

        protected virtual void SetTargetScale(Vector3 sourceScale, float timeStep)
        {
            StepScale(target.localScale, ref sourceScale, timeStep);
            target.localScale = sourceScale;
        }

        protected virtual void StepScale(Vector3 from, ref Vector3 to, float timeStep)
            => transition.StepScale(from, ref to, timeStep);

        public void UsedOnlyForAOTCodeGeneration()
        {
            CachedSetProperty<Quaternion> q = new CachedSetProperty<Quaternion>();
            CachedSetProperty<Vector3> v = new CachedSetProperty<Vector3>();

            throw new InvalidOperationException("This method is used for AOT code generation only. Do not call it at runtime.");
        }
    }
}
