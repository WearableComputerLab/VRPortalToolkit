using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Physics
{
    public struct SphereCaster : IPhysicsCaster
    {
        public float radius;

        public SphereCaster(float radius)
        {
            this.radius = radius;
        }

        public bool Check(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.CheckSphere(origin.GetColumn(3), GetScale(origin.lossyScale), layerMask, queryTriggerInteraction);

        public Collider[] Overlap(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.OverlapSphere(origin.GetColumn(3), GetScale(origin.lossyScale), layerMask, queryTriggerInteraction);

        public int OverlapNonAlloc(Matrix4x4 origin, Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.OverlapSphereNonAlloc(origin.GetColumn(3), GetScale(origin.lossyScale), results, layerMask, queryTriggerInteraction);

        public bool Cast(Matrix4x4 origin, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.GetColumn(2);
            return UnityEngine.Physics.SphereCast(origin.GetColumn(3), GetScale(origin.lossyScale), dirVec, out hitInfo, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }

        public RaycastHit[] CastAll(Matrix4x4 origin, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.GetColumn(2);
            return UnityEngine.Physics.SphereCastAll(origin.GetColumn(3), GetScale(origin.lossyScale), dirVec, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }

        public int CastNonAlloc(Matrix4x4 origin, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.MultiplyVector(Vector3.forward);
            return UnityEngine.Physics.SphereCastNonAlloc(origin.GetColumn(3), radius * GetScale(origin.lossyScale), dirVec, results, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }

        private float GetScale(Vector3 scale)
        {
            if (scale.x > scale.y)
            {
                if (scale.x > scale.z) return scale.x;
                return scale.y;
            }

            if (scale.y > scale.z) return scale.y;
            return scale.y;
        }
    }
}