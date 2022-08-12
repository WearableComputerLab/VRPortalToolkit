using System.Collections;
using System.Collections.Generic;
using Misc.Events;
using Misc.Update;
using UnityEngine;

// TODO: Need a way to get time

namespace Misc.Transformations
{
    public class TransformModifier : MonoBehaviour
    {
        [SerializeField] private UpdateMask _updateMask = new UpdateMask(UpdateFlags.FixedUpdate);
        public UpdateMask updateMask => _updateMask;
        protected Updater updater = new Updater();

        [SerializeField] private TimeUnit _timeUnit = TimeUnit.TimeScaled;
        public virtual TimeUnit timeUnit { get => _timeUnit; set => _timeUnit = value; }
        protected TimeStep timeStep = new TimeStep();

        [SerializeField] private Transform _orientation;
        public virtual Transform orientation { get => _orientation; set => _orientation = value; }

        [SerializeField] private Transform _target;
        public virtual Transform target { get => _target; set => _target = value; }

        [Header("Offsets")]
        [SerializeField] private Vector3 _positionOffset;
        public virtual Vector3 positionOffset { get => _positionOffset; set => _positionOffset = value; }

        [SerializeField] private Quaternion _rotationOffset;
        public virtual Quaternion rotationOffset { get => _rotationOffset; set => _rotationOffset = value; }
        public Vector3 eulerAnglesOffset { get => _rotationOffset.eulerAngles; set => _rotationOffset = Quaternion.Euler(value); }

        [SerializeField] private Vector3 _localScaleOffset;
        public virtual Vector3 localScaleOffset { get => _localScaleOffset; set => _localScaleOffset = value; }

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

            if (target)
            {
                if (_positionOffset != Vector3.zero)
                {
                    if (!_orientation)
                        _target.position += _positionOffset * step;
                    else
                        _target.position += _orientation.TransformVector(_positionOffset * step);
                }

                if (_rotationOffset != Quaternion.identity)
                {
                    if (_orientation == _target)
                        _target.localRotation *= Quaternion.Euler(eulerAnglesOffset * step);
                    else if (!_orientation)
                        _target.rotation *= Quaternion.Euler(eulerAnglesOffset * step);
                    else
                        _target.rotation *= Quaternion.Euler(eulerAnglesOffset * step) * _orientation.localToWorldMatrix.rotation; // TODO: Definitely wrong
                }

                if (_localScaleOffset != Vector3.zero)
                {
                    // Just use local scale
                    _target.localScale += _localScaleOffset * step;
                }
            }

            postUpdate?.Invoke();
        }

        public virtual void SetPositionXOffset(float x)
            => positionOffset = new Vector3(x, _positionOffset.y, _positionOffset.z);

        public virtual void SetPositionYOffset(float y)
            => positionOffset = new Vector3(_positionOffset.x, y, _positionOffset.z);

        public virtual void SetPositionZOffset(float z)
            => positionOffset = new Vector3(_positionOffset.x, _positionOffset.y, z);

        public virtual void SetRotationEulerXOffset(float x)
            => positionOffset = new Vector3(x, _rotationOffset.y, _rotationOffset.z);

        public virtual void SetRotationEulerYOffset(float y)
            => positionOffset = new Vector3(_rotationOffset.x, y, _rotationOffset.z);

        public virtual void SetRotationEulerZOffset(float z)
            => positionOffset = new Vector3(_rotationOffset.x, _rotationOffset.y, z);
    }
}
