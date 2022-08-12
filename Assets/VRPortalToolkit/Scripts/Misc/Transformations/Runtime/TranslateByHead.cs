using Misc.Events;
using Misc.Update;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.Transformations
{
    public class TranslateByHead : MonoBehaviour
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

        [SerializeField] private Transform _forward;
        public virtual Transform forward { get => _forward; set => _forward = value; }

        [SerializeField] private Vector2 _input;
        public virtual Vector2 input { get => _input; set => _input = value; }

        [SerializeField] private Vector2 _speed = Vector2.one;
        public virtual Vector2 speed { get => _speed; set => _speed = value; }

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
            {

                Vector3 up = upright ? upright.up : Vector3.up,
                    actualForward = Vector3.ProjectOnPlane(forward ? forward.forward : Vector3.forward, upright ? upright.up : Vector3.up);

                if (actualForward.magnitude > 0f)
                {
                    actualForward.Normalize();

                    Vector3 right = Vector3.Cross(actualForward, up);

                    Vector2 clampedInput = Vector2.ClampMagnitude(input, 1f);

                    target.position += right * (clampedInput.x * speed.x * step) + actualForward * clampedInput.y * speed.y * step;
                }
            }

            postUpdate?.Invoke();
        }
    }
}
