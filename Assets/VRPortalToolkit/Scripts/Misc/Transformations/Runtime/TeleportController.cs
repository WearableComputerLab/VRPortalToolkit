using Misc.Update;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Transformations
{
    public class TeleportController : MonoBehaviour
    {
        [SerializeField] private UpdateMask _updateMask = new UpdateMask(UpdateFlags.Never);
        public UpdateMask updateMask => _updateMask;
        protected Updater updater = new Updater();

        [SerializeField] private Transition _transition = new Transition();
        public Transition transition => _transition;
        protected TimeStep timeStep = new TimeStep();

        [SerializeField] private Transform _target;
        public virtual Transform target { get => _target; set => _target = value; }
        
        [SerializeField] private Transform _origin;
        public virtual Transform origin { get => _origin; set => _origin = value; }
        
        [SerializeField] private Transform _forward;
        public virtual Transform forward { get => _forward; set => _forward = value; }

        [SerializeField] private TransformProperty _transformProperties = TransformProperty.PositionAndRotation;
        public virtual TransformProperty transformProperties { get => _transformProperties; set => _transformProperties = value; }

        [SerializeField] private Transform _destination;
        public virtual Transform destination { get => _destination; set => _destination = value; }

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

            if (target && destination)
            {
                Vector3 targetPosition = target.position, targetScale = target.localScale;
                Quaternion targetRotation = target.rotation;

                // Apply Scale
                if (transformProperties.HasFlag(TransformProperty.Scale))
                    targetScale = transition.StepScale(target.localScale, destination.localScale, step);

                // Apply Rotation
                if (transformProperties.HasFlag(TransformProperty.Rotation))
                {
                    //Vector3 forward = origin ? target.InverseTransformDirection(origin.forward) : Vector3.forward;

                    targetRotation = destination.rotation;

                    // Correct to look forward
                    if (forward)
                    {
                        Vector3 flatForward = Vector3.ProjectOnPlane(destination.TransformDirection(target.InverseTransformDirection(forward.forward)), destination.up);

                        if (flatForward.magnitude != 0f)
                            targetRotation = Quaternion.LookRotation(Vector3.Reflect(flatForward, destination.right), destination.up);
                    }

                    transition.StepRotation(target.rotation, targetRotation, step);
                }

                // Apply Position
                if (transformProperties.HasFlag(TransformProperty.Position))
                {
                    Vector3 actualCentre = origin ? (new Plane(destination.up, targetPosition).ClosestPointOnPlane(origin.position)) : targetPosition;

                    // Apply transform need to get centre to distination
                    targetPosition = transition.StepPosition(target.position, target.position + destination.position - actualCentre, step);
                }

                // Do the actual transformations all at the end
                target.SetPositionAndRotation(targetPosition, targetRotation);
                target.localScale = targetScale;
            }

            postUpdate?.Invoke();
        }
    }
}
