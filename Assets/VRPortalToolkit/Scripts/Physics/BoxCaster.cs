using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Physics
{
    // TODO: Not sure about scale here
    public struct BoxCaster : IPhysicsCaster
    {
        public Vector3 halfExtents;

        public Quaternion orientation;

        public BoxCaster(Vector3 halfExtents, Quaternion orientation)
        {
            this.halfExtents = halfExtents;
            this.orientation = orientation;
        }

        public bool Check(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.CheckBox(origin.GetColumn(3), halfExtents * origin.lossyScale.z, orientation * origin.rotation, layerMask, queryTriggerInteraction);

        public Collider[] Overlap(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.OverlapBox(origin.GetColumn(3), halfExtents * origin.lossyScale.z, orientation * origin.rotation, layerMask, queryTriggerInteraction);

        public int OverlapNonAlloc(Matrix4x4 origin, Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.OverlapBoxNonAlloc(origin.GetColumn(3), halfExtents * origin.lossyScale.z, results, orientation * origin.rotation, layerMask, queryTriggerInteraction);

        public bool Cast(Matrix4x4 origin, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.GetColumn(2);
            return UnityEngine.Physics.BoxCast(origin.GetColumn(3), dirVec, halfExtents * origin.lossyScale.z, out hitInfo, orientation * origin.rotation, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }

        public RaycastHit[] CastAll(Matrix4x4 origin, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.GetColumn(2);
            return UnityEngine.Physics.BoxCastAll(origin.GetColumn(3), dirVec, halfExtents * origin.lossyScale.z, orientation * origin.rotation, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }

        public int CastNonAlloc(Matrix4x4 origin, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.MultiplyVector(Vector3.forward);
            return UnityEngine.Physics.BoxCastNonAlloc(origin.GetColumn(0), dirVec, halfExtents * origin.lossyScale.z, results, orientation * origin.rotation, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }
    }
}