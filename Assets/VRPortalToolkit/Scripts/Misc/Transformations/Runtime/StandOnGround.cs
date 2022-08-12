using Misc.Update;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Transformations
{
    public class StandOnGround : MonoBehaviour
    {
        [SerializeField] private UpdateMask _updateMask = new UpdateMask(UpdateFlags.FixedUpdate);
        public UpdateMask updateMask => _updateMask;
        protected Updater updater = new Updater();

        [SerializeField] private Transition _transition = new Transition();
        public Transition transition => _transition;
        protected TimeStep timeStep = new TimeStep();

        [SerializeField] private Transform _target;
        public virtual Transform target { get => _target; set => _target = value; }

        [SerializeField] private Transform _groundPlane;
        public virtual Transform groundPlane { get => _groundPlane; set => _groundPlane = value; }

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

            float step = timeStep.UpdateStep(transition.timeUnit);

            if (target)
            {
                Plane plane = groundPlane ? new Plane(_groundPlane.up, _groundPlane.position) : new Plane(Vector3.up, Vector3.zero);

                Vector3 groundedPoint = plane.ClosestPointOnPlane(_target.position);

                transition.StepPosition(_target.position, ref groundedPoint, step);
                _target.position = groundedPoint;
            }

            postUpdate?.Invoke();
        }
    }
}