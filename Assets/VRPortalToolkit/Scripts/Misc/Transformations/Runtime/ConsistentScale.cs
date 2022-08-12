using Misc.Update;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Transformations
{
    public class ConsistentScale : MonoBehaviour
    {
        [SerializeField] private UpdateMask _updateMask = new UpdateMask(UpdateFlags.FixedUpdate);
        public UpdateMask updateMask => _updateMask;
        protected Updater updater = new Updater();

        [SerializeField] private Transition _transition = new Transition();
        public Transition transition => _transition;
        protected TimeStep timeStep = new TimeStep();

        [Header("Target")]
        [SerializeField] private Transform _target;
        public virtual Transform target { get => _target; set => _target = value; }


        [SerializeField] private Vector3 _localScale;
        public virtual Vector3 localScale { get => _localScale; set => _localScale = value; }

        [SerializeField] private Vector3 _worldScale = Vector3.one;
        public virtual Vector3 worldScale { get => _worldScale; set => _worldScale = value; }

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

            float step = timeStep.UpdateStep(transition.timeUnit);

            if (target)
            {

                Vector3 newScale = worldScale;

                Transform parent = target.parent;

                // TODO: I don't know if there is a more efficient way, but this works
                while (parent != null)
                {
                    newScale = new Vector3(newScale.x / parent.localScale.x, newScale.y / parent.localScale.y, newScale.z / parent.localScale.z);
                    parent = parent.parent;
                }

                target.localScale = transition.StepScale(target.localScale, localScale + newScale, step);
            }

            postUpdate?.Invoke();
        }
    }
}
