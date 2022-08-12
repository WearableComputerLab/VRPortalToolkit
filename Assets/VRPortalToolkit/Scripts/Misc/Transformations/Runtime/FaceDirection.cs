using Misc.Events;
using Misc.Update;
using UnityEngine;

namespace Misc.Transformations
{
    public class FaceDirection : MonoBehaviour
    {
        [SerializeField] private UpdateMask _updateMask = new UpdateMask(UpdateFlags.FixedUpdate);
        public UpdateMask updateMask => _updateMask;
        protected Updater updater = new Updater();

        [SerializeField] private Transition _transition = new Transition();
        public Transition transition => _transition;
        protected TimeStep timeStep = new TimeStep();


        [SerializeField] private Transform _target;
        public Transform target {
            get => _target;
            set => _target = value;
        }

        [SerializeField] private Transform _upright;
        public Transform upright {
            get => _upright;
            set => _upright = value;
        }

        [SerializeField] private Transform _forward;
        public Transform forward {
            get => _forward;
            set => _forward = value;
        }

        [SerializeField] private Priority _priority = Priority.Forward;
        public Priority priority
        {
            get => _priority;
            set => _priority = value;
        }

        public enum Priority
        {
            Upright = 0,
            Forward = 1
        }

        [SerializeField] private Vector2 _input = Vector2.up;
        public Vector2 input {
            get => _input;
            set => _input = value;
        }

        [Header("Events")]
        public SerializableEvent preUpdate = new SerializableEvent();
        public SerializableEvent postUpdate = new SerializableEvent();

        protected virtual void Reset()
        {
            target = transform;
        }

        protected virtual void OnValidate()
        {
            updater.updateMask = _updateMask;
        }

        protected virtual void Awake()
        {
            updater.updateMask = _updateMask;
            updater.onInvoke = ForceApply;
        }

        protected virtual void OnEnable()
        {
            timeStep.UpdateStep();
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
                Vector3 up = upright ? upright.up : Vector3.up,
                    actualForward = forward ? forward.forward : Vector3.forward;

                Quaternion rotation = Quaternion.LookRotation(priority == Priority.Forward ? actualForward : Vector3.ProjectOnPlane(actualForward, up), up);
                
                float angle = Vector2.SignedAngle(input, Vector2.up);

                rotation *= Quaternion.AngleAxis(angle, up);

                target.rotation = transition.StepRotation(target.rotation, rotation, step);
            }

            postUpdate?.Invoke();
        }
    }
}
