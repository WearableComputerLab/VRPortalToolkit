using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Physics
{
    // TODO: Not sure about scale here
    public struct CapsuleCaster : IPhysicsCaster
    {
        public Vector3 endOffset;

        public float radius;

        public CapsuleCaster(Vector3 endOffset, float radius)
        {
            this.endOffset = endOffset;
            this.radius = radius;
        }

        public bool Check(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.CheckCapsule(origin.GetColumn(3), origin.MultiplyVector(endOffset), radius * origin.lossyScale.z, layerMask, queryTriggerInteraction);

        public Collider[] Overlap(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.OverlapCapsule(origin.GetColumn(3), origin.MultiplyVector(endOffset), radius * origin.lossyScale.z, layerMask, queryTriggerInteraction);

        public int OverlapNonAlloc(Matrix4x4 origin, Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.OverlapCapsuleNonAlloc(origin.GetColumn(3), origin.MultiplyVector(endOffset), radius * origin.lossyScale.z, results, layerMask, queryTriggerInteraction);

        public bool Cast(Matrix4x4 origin, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.GetColumn(2);
            return UnityEngine.Physics.CapsuleCast(origin.GetColumn(3), origin.MultiplyVector(endOffset), radius * origin.lossyScale.z, dirVec, out hitInfo, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }

        public RaycastHit[] CastAll(Matrix4x4 origin, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.GetColumn(2);
            return UnityEngine.Physics.CapsuleCastAll(origin.GetColumn(3), origin.MultiplyVector(endOffset), radius * origin.lossyScale.z, dirVec, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }

        public int CastNonAlloc(Matrix4x4 origin, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.MultiplyVector(Vector3.forward);
            return UnityEngine.Physics.CapsuleCastNonAlloc(origin.GetColumn(3), origin.MultiplyVector(endOffset), radius * origin.lossyScale.z, dirVec, results, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }
    }
}