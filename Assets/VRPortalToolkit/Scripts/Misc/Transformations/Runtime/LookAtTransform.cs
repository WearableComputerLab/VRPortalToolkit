using Misc.Update;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Transformations
{
    public class LookAtTransform : MonoBehaviour
    {
        [SerializeField] private UpdateMask _updateMask = new UpdateMask(UpdateFlags.FixedUpdate);
        public UpdateMask updateMask => _updateMask;
        protected Updater updater = new Updater();

        [SerializeField] private Transition _transition = new Transition();
        public Transition transition => _transition;
        protected TimeStep timeStep = new TimeStep();

        [SerializeField] private Transform _origin;
        public virtual Transform origin { get => _origin; set => _origin = value; }

        [SerializeField] private Transform _cursor;
        public virtual Transform cursor {
            get => _cursor ? _cursor : _cursor = transform;
            set => _cursor = value;
        }

        [SerializeField] private Transform _upright;
        public virtual Transform upright { get => _upright; set => _upright = value; }

        [Header("Events")]
        public UnityEvent preUpdate;
        public UnityEvent postUpdate;

        protected virtual void Reset()
        {
            origin = transform;
        }

        protected virtual void Awake()
        {
            updater.onInvoke = ForceApply;
            updater.updateMask = _updateMask;
        }

        protected virtual void OnValidate()
        {
            updater.updateMask = _updateMask;
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

            timeStep.unit = transition.timeUnit;
            float step = timeStep.UpdateStep();

            if (cursor && origin)
            {

                Vector3 upwards = upright ? upright.up : Vector3.up;

                //cursor.LookAt(origin, _upright ? _upright.up : Vector3.up);

                Vector3 direction = Vector3.ProjectOnPlane(origin.position - cursor.position, upwards);

                if (direction == Vector3.zero) direction = Vector3.Cross(upwards, Vector3.up);

                Quaternion targetRotation = Quaternion.LookRotation(direction, upwards);

                origin.rotation = transition.StepRotation(origin.rotation, targetRotation, step);

            }

            postUpdate?.Invoke();
        }
    }
}