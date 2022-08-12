using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Physics
{
    public interface IPhysicsCaster
    {
        bool Check(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        Collider[] Overlap(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        int OverlapNonAlloc(Matrix4x4 origin, Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        bool Cast(Matrix4x4 origin, out RaycastHit hitInfo, float localDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        RaycastHit[] CastAll(Matrix4x4 origin, float localDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        int CastNonAlloc(Matrix4x4 origin, RaycastHit[] all, float localDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
    }
}