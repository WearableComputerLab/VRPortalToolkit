using Misc.Update;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Transformations
{
    public class StandUpright : MonoBehaviour
    {
        [SerializeField] private UpdateMask _updateMask = new UpdateMask(UpdateFlags.FixedUpdate);
        public UpdateMask updateMask => _updateMask;
        protected Updater updater = new Updater();

        [SerializeField] private Transition _transition = new Transition();
        public Transition transition => _transition;
        protected TimeStep timeStep = new TimeStep();

        [SerializeField] private Transform _target;
        public virtual Transform target { get => _target; set => _target = value; }

        [SerializeField] private Transform _head;
        public virtual Transform head { get => _head; set => _head = value; }

        [SerializeField] private Transform _upright;
        public virtual Transform upright { get => _upright; set => _upright = value; }

        [Header("Events")]
        public UnityEvent preUpdate;
        public UnityEvent postUpdate;

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

            timeStep.unit = transition.timeUnit;
            float step = timeStep.UpdateStep();

            if (target)
            {
                Vector3 upwards = _upright ? _upright.up : Vector3.up,
                    cachedHead = Vector3.ProjectOnPlane(head ? _head.position - _target.position : Vector3.zero, upwards);

                Quaternion targetRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(_target.forward, upwards), upwards);

                transition.StepRotation(_target.rotation, ref targetRotation, step);
                _target.rotation = targetRotation;

                // Now correct the head position
                Vector3 headPosition = head ? _head.position : Vector3.zero, planePosition = Vector3.ProjectOnPlane(headPosition - _target.position, upwards);
                _target.position += cachedHead - planePosition;
            }

            postUpdate?.Invoke();
        }
    }
}