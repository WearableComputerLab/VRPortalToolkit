using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Physics
{
    /// <summary>
    /// Provides box-based physics casting operations for portal physics.
    /// </summary>
    public struct BoxCaster : IPhysicsCaster
    {
        /// <summary>
        /// The half extents of the box.
        /// </summary>
        public Vector3 halfExtents;

        /// <summary>
        /// The orientation of the box.
        /// </summary>
        public Quaternion orientation;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoxCaster"/> struct.
        /// </summary>
        /// <param name="halfExtents">The half extents of the box.</param>
        /// <param name="orientation">The orientation of the box.</param>
        public BoxCaster(Vector3 halfExtents, Quaternion orientation)
        {
            this.halfExtents = halfExtents;
            this.orientation = orientation;
        }

        /// <inheritdoc/>
        public bool Check(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.CheckBox(origin.GetColumn(3), halfExtents * origin.lossyScale.z, orientation * origin.rotation, layerMask, queryTriggerInteraction);

        /// <inheritdoc/>
        public Collider[] Overlap(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.OverlapBox(origin.GetColumn(3), halfExtents * origin.lossyScale.z, orientation * origin.rotation, layerMask, queryTriggerInteraction);

        /// <inheritdoc/>
        public int OverlapNonAlloc(Matrix4x4 origin, Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => UnityEngine.Physics.OverlapBoxNonAlloc(origin.GetColumn(3), halfExtents * origin.lossyScale.z, results, orientation * origin.rotation, layerMask, queryTriggerInteraction);

        /// <inheritdoc/>
        public bool Cast(Matrix4x4 origin, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.GetColumn(2);
            return UnityEngine.Physics.BoxCast(origin.GetColumn(3), dirVec, halfExtents * origin.lossyScale.z, out hitInfo, orientation * origin.rotation, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }

        /// <inheritdoc/>
        public RaycastHit[] CastAll(Matrix4x4 origin, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.GetColumn(2);
            return UnityEngine.Physics.BoxCastAll(origin.GetColumn(3), dirVec, halfExtents * origin.lossyScale.z, orientation * origin.rotation, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }

        /// <inheritdoc/>
        public int CastNonAlloc(Matrix4x4 origin, RaycastHit[] results, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            Vector3 dirVec = origin.MultiplyVector(Vector3.forward);
            return UnityEngine.Physics.BoxCastNonAlloc(origin.GetColumn(0), dirVec, halfExtents * origin.lossyScale.z, results, orientation * origin.rotation, dirVec.magnitude * maxDistance, layerMask, queryTriggerInteraction);
        }
    }
}