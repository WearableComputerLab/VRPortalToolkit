using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Physics
{
    /// <summary>
    /// Provides capsule-based physics casting operations for portal physics.
    /// </summary>
    public struct CapsuleCaster : IPhysicsCaster
    {
        /// <summary>
        /// The offset from the start to the end of the capsule.
        /// </summary>
        public Vector3 endOffset;

        /// <summary>
        /// The radius of the capsule.
        /// </summary>
        public float radius;

        /// <summary>
        /// Initializes a new instance of the <see cref="CapsuleCaster"/> struct.
        /// </summary>
        /// <param name="endOffset">The offset from the start to the end of the capsule.</param>
        /// <param name="radius">The radius of the capsule.</param>
        public CapsuleCaster(Vector3 endOffset, float radius)
        {
            this.endOffset = endOffset;
            this.radius = radius;
        }

        /// <inheritdoc/>
        public bool Check(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.CheckCapsule(origin.GetColumn(3), origin.MultiplyVector(endOffset), radius * origin.lossyScale.z, layerMask, queryTriggerInteraction);

        /// <inheritdoc/>
        public Collider[] Overlap(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.OverlapCapsule(origin.GetColumn(3), origin.MultiplyVector(endOffset), radius * origin.lossyScale.z, layerMask, queryTriggerInteraction);

        /// <inheritdoc/>
        public int OverlapNonAlloc(Matrix4x4 origin, Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.OverlapCapsuleNonAlloc(origin.GetColumn(3), origin.MultiplyVector(endOffset), radius * origin.lossyScale.z, results, layerMask, queryTriggerInteraction);

        /// <inheritdoc/>
        public bool Cast(Matrix4x4 origin, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.GetColumn(2);
            return UnityEngine.Physics.CapsuleCast(origin.GetColumn(3), origin.MultiplyVector(endOffset), radius * origin.lossyScale.z, dirVec, out hitInfo, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }

        /// <inheritdoc/>
        public RaycastHit[] CastAll(Matrix4x4 origin, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.GetColumn(2);
            return UnityEngine.Physics.CapsuleCastAll(origin.GetColumn(3), origin.MultiplyVector(endOffset), radius * origin.lossyScale.z, dirVec, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }

        /// <inheritdoc/>
        public int CastNonAlloc(Matrix4x4 origin, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.MultiplyVector(Vector3.forward);
            return UnityEngine.Physics.CapsuleCastNonAlloc(origin.GetColumn(3), origin.MultiplyVector(endOffset), radius * origin.lossyScale.z, dirVec, results, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }
    }
}