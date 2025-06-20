using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Physics
{
    /// <summary>
    /// Interface for physics casters used in portal physics operations.
    /// </summary>
    public interface IPhysicsCaster
    {
        /// <summary>
        /// Checks if the shape overlaps any colliders.
        /// </summary>
        bool Check(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        /// <summary>
        /// Gets all colliders overlapping the shape.
        /// </summary>
        Collider[] Overlap(Matrix4x4 origin, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        /// <summary>
        /// Gets all colliders overlapping the shape, using a preallocated array.
        /// </summary>
        int OverlapNonAlloc(Matrix4x4 origin, Collider[] results, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        /// <summary>
        /// Casts the shape and returns true if it hits something.
        /// </summary>
        bool Cast(Matrix4x4 origin, out RaycastHit hitInfo, float localDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        /// <summary>
        /// Casts the shape and returns all hits.
        /// </summary>
        RaycastHit[] CastAll(Matrix4x4 origin, float localDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction);

        /// <summary>
        /// Casts the shape and fills a preallocated array with hits.
        /// </summary>
        int CastNonAlloc(Matrix4x4 origin, RaycastHit[] all, float localDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction);
    }
}