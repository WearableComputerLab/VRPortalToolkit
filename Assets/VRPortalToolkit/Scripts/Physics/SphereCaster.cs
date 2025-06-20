using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Physics
{
    /// <summary>
    /// Provides sphere-based physics casting operations for portal physics.
    /// </summary>
    public struct SphereCaster : IPhysicsCaster
    {
        /// <summary>
        /// The radius of the sphere.
        /// </summary>
        public float radius;

        /// <summary>
        /// Initializes a new instance of the <see cref="SphereCaster"/> struct.
        /// </summary>
        /// <param name="radius">The radius of the sphere.</param>
        public SphereCaster(float radius)
        {
            this.radius = radius;
        }

        /// <inheritdoc/>
        public bool Check(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.CheckSphere(origin.GetColumn(3), GetScale(origin.lossyScale), layerMask, queryTriggerInteraction);

        /// <inheritdoc/>
        public Collider[] Overlap(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.OverlapSphere(origin.GetColumn(3), GetScale(origin.lossyScale), layerMask, queryTriggerInteraction);

        /// <inheritdoc/>
        public int OverlapNonAlloc(Matrix4x4 origin, Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.OverlapSphereNonAlloc(origin.GetColumn(3), GetScale(origin.lossyScale), results, layerMask, queryTriggerInteraction);

        /// <inheritdoc/>
        public bool Cast(Matrix4x4 origin, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.GetColumn(2);
            return UnityEngine.Physics.SphereCast(origin.GetColumn(3), GetScale(origin.lossyScale), dirVec, out hitInfo, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }

        /// <inheritdoc/>
        public RaycastHit[] CastAll(Matrix4x4 origin, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.GetColumn(2);
            return UnityEngine.Physics.SphereCastAll(origin.GetColumn(3), GetScale(origin.lossyScale), dirVec, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }

        /// <inheritdoc/>
        public int CastNonAlloc(Matrix4x4 origin, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.MultiplyVector(Vector3.forward);
            return UnityEngine.Physics.SphereCastNonAlloc(origin.GetColumn(3), radius * GetScale(origin.lossyScale), dirVec, results, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }

        /// <summary>
        /// Gets the largest scale component for the sphere.
        /// </summary>
        /// <param name="scale">The scale vector.</param>
        /// <returns>The largest component of the scale vector.</returns>
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