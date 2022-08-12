using Misc.Update;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Portables
{
    public class ForcePortableCheck : MonoBehaviour
    {
        [SerializeField] private UpdateMask _updateMask = new UpdateMask(UpdateFlags.Update);
        public UpdateMask UpdateMask => _updateMask;
        protected Updater updater = new Updater();

        [SerializeField] private Transform _target;
        public Transform target { get => _target; set => _target = value; }

        protected virtual void Awake()
        {
            updater.updateMask = _updateMask;
            updater.onInvoke = ForceApply;
        }

        protected virtual void OnEnable()
        {
            updater.enabled = true;
        }

        protected virtual void OnDisable()
        {
            updater.enabled = false;
        }

        protected virtual void Reset()
        {
            _target = transform;
        }

        public virtual void Apply()
        {
            if (isActiveAndEnabled && Application.isPlaying && !updater.isUpdating) ForceApply();
        }

        public virtual void ForceApply()
        {
            PortalPhysics.ForcePortalCheck(target);
        }
    }
}
