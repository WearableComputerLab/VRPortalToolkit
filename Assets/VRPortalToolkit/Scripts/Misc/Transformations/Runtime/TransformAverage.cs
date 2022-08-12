using Misc.Update;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Transformations
{
    public class TransformAverage : MonoBehaviour
    {
        [SerializeField] private UpdateMask _updateMask = new UpdateMask(UpdateFlags.Update);
        public UpdateMask updateMask => _updateMask;
        protected Updater updater = new Updater();

        [SerializeField] private TransformProperty _properties = TransformProperty.PositionAndRotation;
        public TransformProperty properties { get => _properties; set => _properties = value; }

        [SerializeField] private Transition _transition = new Transition();
        public Transition transition => _transition;
        protected TimeStep timeStep = new TimeStep();

        [Header("Source")]
        [SerializeField] protected List<Transform> sources = new List<Transform>();
        public HeapAllocationFreeReadOnlyList<Transform> readOnlySources => sources;
        [SerializeField] private SpaceMode _sourcesMode = SpaceMode.World;
        public virtual SpaceMode sourcesMode { get => _sourcesMode; set => _sourcesMode = value; }

        [Header("Target")]
        [SerializeField] private Transform _target;
        public virtual Transform target { get => _target; set => _target = value; }

        [SerializeField] private SpaceMode _targetMode = SpaceMode.World;
        public virtual SpaceMode targetMode { get => _targetMode; set => _targetMode = value; }

        [Header("Events")]
        public UnityEvent preUpdate;
        public UnityEvent postUpdate;

        public enum SpaceMode
        {
            Local = 0,
            World = 1,
        }

        protected Matrix4x4 previousMatrix;

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

        protected virtual void Reset()
        {
            target = transform;
        }

        public virtual void Apply()
        {
            if (isActiveAndEnabled && Application.isPlaying && !updater.isUpdating) ForceApply();
        }

        public virtual void ForceApply()
        {
            preUpdate?.Invoke();

            timeStep.unit = transition.timeUnit;
            float step = timeStep.UpdateStep();

            if (sources.Count > 0 && target)
            {
                int count = 0;

                foreach (Transform transform in sources)
                    if (transform) count++;

                if (count != 0)
                {
                    if (properties.HasFlag(TransformProperty.Position))
                        ApplyPosition(count, step);

                    if (properties.HasFlag(TransformProperty.Rotation))
                        ApplyRotation(count, step);

                    if (properties.HasFlag(TransformProperty.Scale))
                        ApplyScale(count, step);
                }
            }

            postUpdate?.Invoke();
        }

        protected virtual void ApplyPosition(int count, float timeStep) => SetTargetPosition(GetSourcePosition(count), timeStep);

        protected virtual Vector3 GetSourcePosition(int count)
        {
            Vector3 sourcePosition = Vector3.zero;

            switch (sourcesMode)
            {
                case SpaceMode.Local:
                    foreach (Transform transform in sources)
                        sourcePosition += transform.localPosition;
                    break;

                default:
                    foreach (Transform transform in sources)
                        sourcePosition += transform.position;
                    break;
            }

            sourcePosition = new Vector3(sourcePosition.x / count, sourcePosition.y / count, sourcePosition.z / count);
            return sourcePosition;
        }

        protected virtual Vector3 SetTargetPosition(Vector3 sourcePosition, float timeStep)
        {
            switch (targetMode)
            {
                case SpaceMode.Local:
                    StepPosition(target.localPosition, ref sourcePosition, timeStep);
                    target.localPosition = sourcePosition;
                    break;

                default:
                    StepPosition(target.position, ref sourcePosition, timeStep);
                    target.position = sourcePosition;
                    break;
            }

            return sourcePosition;
        }

        protected virtual void ApplyRotation(int count, float timeStep) => SetTargetScale(GetSourceRotation(), timeStep);

        protected virtual Quaternion GetSourceRotation()
        {
            Quaternion sourceRotation;
            float x = 0f, y = 0f, z = 0f, w = 0f, k;

            // This average only works well for close rotations, but what ya gonna do :/
            // https://gamedev.stackexchange.com/questions/119688/calculate-average-of-arbitrary-amount-of-quaternions-recursion
            switch (sourcesMode)
            {
                case SpaceMode.Local:
                    foreach (Transform transform in sources)
                    {
                        sourceRotation = transform.localRotation;
                        x += sourceRotation.x; y += sourceRotation.y; z += sourceRotation.z; w += sourceRotation.w;
                    }
                    break;

                default:
                    foreach (Transform transform in sources)
                    {
                        sourceRotation = transform.rotation;
                        x += sourceRotation.x; y += sourceRotation.y; z += sourceRotation.z; w += sourceRotation.w;
                    }
                    break;
            }

            k = 1.0f / Mathf.Sqrt(x * x + y * y + z * z + w * w);
            sourceRotation = new Quaternion(x * k, y * k, z * k, w * k);
            return sourceRotation;
        }

        protected virtual void SetTargetScale(Quaternion sourceRotation, float timeStep)
        {
            switch (targetMode)
            {
                case SpaceMode.Local:
                    StepRotation(target.localRotation, ref sourceRotation, timeStep);
                    target.localRotation = sourceRotation;
                    break;

                default:
                    StepRotation(target.rotation, ref sourceRotation, timeStep);
                    target.rotation = sourceRotation;
                    break;
            }
        }

        protected virtual void ApplyScale(int count, float timeStep) => SetTargetScale(GetSourceScale(count), timeStep);

        protected virtual Vector3 GetSourceScale(int count)
        {
            Vector3 sourceScale = Vector3.zero;

            switch (sourcesMode)
            {
                case SpaceMode.Local:
                    foreach (Transform source in sources)
                        sourceScale += source.localScale;
                    break;

                default:
                    foreach (Transform source in sources)
                        sourceScale += source.lossyScale;
                    break;
            }

            sourceScale = new Vector3(sourceScale.x / count, sourceScale.y / count, sourceScale.z / count);
            return sourceScale;
        }

        protected virtual void SetTargetScale(Vector3 sourceScale, float timeStep)
        {
            StepPosition(target.localScale, ref sourceScale, timeStep);
            target.localScale = sourceScale;
        }

        protected virtual void StepPosition(Vector3 from, ref Vector3 to, float timeStep)
            => transition.StepPosition(from, ref to, timeStep);

        protected virtual void StepRotation(Quaternion from, ref Quaternion to, float timeStep)
            => transition.StepRotation(from, ref to, timeStep);

        protected virtual void StepScale(Vector3 from, ref Vector3 to, float timeStep)
            => transition.StepScale(from, ref to, timeStep);

        public virtual void AddSource(Transform source)
        {
            if (source) sources.Add(source);
        }

        public virtual void RemoveSource(Transform source)
        {
            sources.Remove(source);
        }

        public virtual void ClearSources() => sources.Clear();
    }
}