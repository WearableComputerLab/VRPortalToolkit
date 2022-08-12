using Misc.Events;
using Misc.Update;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Transformations
{
    public class RotateAround : MonoBehaviour
    {
        [SerializeField] private UpdateMask _updateMask = new UpdateMask(UpdateFlags.FixedUpdate);
        public UpdateMask updateMask => _updateMask;
        protected Updater updater = new Updater();

        [SerializeField] private TimeUnit _timeUnit = TimeUnit.TimeScaled;
        public virtual TimeUnit timeUnit { get => _timeUnit; set => _timeUnit = value; }
        protected TimeStep timeStep = new TimeStep();

        [SerializeField] private Transform _target;
        public virtual Transform target { get => _target; set => _target = value; }

        [SerializeField] private Transform _upright;
        public virtual Transform upright { get => _upright; set => _upright = value; }

        [SerializeField] private Transform _origin;
        public virtual Transform origin { get => _origin; set => _origin = value; }

        [SerializeField] private float _input;
        public virtual float input { get => _input; set => _input = value; }

        [SerializeField] private float _speed = 1f;
        public virtual float speed { get => _speed; set => _speed = value; }

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
            timeStep.unit = _timeUnit;
        }

        protected virtual void OnEnable()
        {
            timeStep.UpdateStep(_timeUnit);
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

            float step = timeStep.UpdateStep(_timeUnit);

            if (target && step != 0f)
                target.RotateAround(origin ? origin.position : target.position, upright ? upright.up : Vector3.up, Mathf.Clamp(input, -1f, 1f) * speed * step);

            postUpdate?.Invoke();
        }
    }
}
