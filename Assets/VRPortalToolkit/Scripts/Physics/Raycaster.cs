using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Physics
{
    public struct Raycaster : IPhysicsCaster
    {
        public bool Check(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.CheckSphere(origin.GetColumn(3), float.MinValue, layerMask, queryTriggerInteraction);

        public Collider[] Overlap(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.OverlapSphere(origin.GetColumn(3), float.MinValue, layerMask, queryTriggerInteraction);

        public int OverlapNonAlloc(Matrix4x4 origin, Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.OverlapSphereNonAlloc(origin.GetColumn(3), float.MinValue, results, layerMask, queryTriggerInteraction);

        public bool Cast(Matrix4x4 origin, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.GetColumn(2);
            return UnityEngine.Physics.Raycast(origin.GetColumn(3), dirVec, out hitInfo, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }

        public RaycastHit[] CastAll(Matrix4x4 origin, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.GetColumn(2);
            return UnityEngine.Physics.RaycastAll(origin.GetColumn(3), dirVec, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }

        public int CastNonAlloc(Matrix4x4 origin, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.MultiplyVector(Vector3.forward);
            return UnityEngine.Physics.RaycastNonAlloc(origin.GetColumn(3), dirVec, results, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }
    }
}