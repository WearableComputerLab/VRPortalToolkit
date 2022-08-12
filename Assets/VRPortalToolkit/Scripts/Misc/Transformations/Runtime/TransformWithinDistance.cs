using Misc.EditorHelpers;
using Misc.Update;
using UnityEngine;
using UnityEngine.Events;

namespace Misc.Transformations
{
    public class TransformWithinDistance : MonoBehaviour
    {
        [SerializeField] private UpdateMask _updateMask = new UpdateMask(UpdateFlags.FixedUpdate);
        public UpdateMask updateMask => _updateMask;
        protected Updater updater = new Updater();

        [SerializeField] private Transform _source;
        public virtual Transform source { get => _source; set => _source = value; }

        [SerializeField] private Transform _target;
        public virtual Transform target { get => _target; set => _target = value; }

        [SerializeField] private float _minDistance = 0f;
        public virtual float minDistance { get => _minDistance; set => _minDistance = value; }

        [SerializeField] private float _maxDistance = 1f;
        public virtual float maxDistance { get => _maxDistance; set => _maxDistance = value; }

        [SerializeField] private bool _isWithinRange;
        public virtual bool isWithinRange
        {
            get => _isWithinRange;
            set
            {
                if (_isWithinRange != value)
                {
                    Validate.UpdateField(this, nameof(_isWithinRange), _isWithinRange = value);

                    if (isActiveAndEnabled && Application.isPlaying)
                    {
                        if (_isWithinRange)
                            enteredRange?.Invoke();
                        else
                            exitedRange?.Invoke();
                    }
                }
            }
        }
        
        [Header("Events")]
        public UnityEvent enteredRange;
        public UnityEvent exitedRange;

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
            Validate.FieldWithProperty(this, nameof(_isWithinRange), nameof(isWithinRange));
        }

        protected virtual void OnEnable()
        {
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
            if (source && target)
            {
                bool newIsWithinRange = true;

                if (Vector3.Distance(target.position, source.position) < minDistance)
                    newIsWithinRange = false;
                else if (Vector3.Distance(target.position, source.position) > maxDistance)
                    newIsWithinRange = false;

                isWithinRange = newIsWithinRange;
            }
        }
    }
}
