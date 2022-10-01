using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Portables
{
    public class ForcePortableCheck : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        public Transform target { get => _target; set => _target = value; }

        protected virtual void Reset()
        {
            _target = transform;
        }

        protected virtual void LateUpdate()
        {
            Apply();
        }

        protected virtual void FixedUpdate()
        {
            Apply();
        }

        public virtual void Apply()
        {
            PortalPhysics.ForcePortalCheck(target);
        }
    }
}
