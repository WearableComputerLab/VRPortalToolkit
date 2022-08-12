using Misc.EditorHelpers;
using Misc.Events;
using Misc.Update;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Transformations
{
    public class TransformUpdater : MonoBehaviour
    {

        [SerializeField] private UpdateMask _updateMask = new UpdateMask(UpdateFlags.FixedUpdate);
        public UpdateMask updateMask => _updateMask;
        protected Updater updater = new Updater();

        [SerializeField] private TransformProperty _properties = TransformProperty.PositionAndRotation;
        public virtual TransformProperty properties { get => _properties; set => _properties = value; }

        [SerializeField] private Transition _transition = new Transition();
        public Transition transition => _transition;
        protected TimeStep timeStep = new TimeStep();

        [Header("Target")]
        [SerializeField] private Transform _target;
        public Transform target { get => _target; set => _target = value; }

        [SerializeField] private SpaceMode _spaceMode = SpaceMode.World;
        public virtual SpaceMode spaceMode
        { get => _spaceMode; set => _spaceMode = value; }

#if UNITY_EDITOR
        private bool hasPosition => properties.HasFlag(TransformProperty.Position);

        [Space]
        [ShowIf(nameof(hasPosition))]
#endif
        [SerializeField] private Vector3 _position;
        public Vector3 position { get => _position; set => _position = value; }

#if UNITY_EDITOR
        private bool hasRotation => properties.HasFlag(TransformProperty.Rotation);

        [ShowIf(nameof(hasRotation))]
#endif
        [SerializeField] private Quaternion _rotation;
        public Quaternion rotation { get => _rotation; set => _rotation = value; }
        public Vector3 eulerAngles { get => _rotation.eulerAngles; set => _rotation = Quaternion.Euler(value); }

#if UNITY_EDITOR
        private bool hasScale => properties.HasFlag(TransformProperty.Scale);


        [ShowIf(nameof(hasScale))]
#endif
        [SerializeField] private Vector3 _scale = Vector3.one;
        public Vector3 scale { get => _scale; set => _scale = value; }

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

        public virtual void Apply()
        {
            if (isActiveAndEnabled && Application.isPlaying && !updater.isUpdating) ForceApply();
        }

        public virtual void ForceApply()
        {
            preUpdate?.Invoke();

            float step = timeStep.UpdateStep(transition.timeUnit);

            if (target)
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

        protected virtual void ApplyPosition(float timeStep)
        {
            if (spaceMode == SpaceMode.Local)
                target.localPosition = transition.StepPosition(target.localPosition, position, timeStep);
            else
                target.position = transition.StepPosition(target.position, position, timeStep);
        }

        protected virtual void ApplyRotation(float timeStep)
        {
            if (spaceMode == SpaceMode.Local)
                target.localRotation = transition.StepRotation(target.localRotation, rotation, timeStep);
            else
                target.rotation = transition.StepRotation(target.rotation, rotation, timeStep);
        }

        protected virtual void ApplyScale(float timeStep)
        {
            //if (sourceSpace == SpaceMode.Local)
            target.localScale = transition.StepScale(target.localScale, scale, timeStep);
            //else
            //    target.localScale = transition.StepScale(target.lossyScale, scale, timeStep);
        }
    }
}
