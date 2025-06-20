using UnityEngine;
using VRPortalToolkit.Physics;

/// <summary>
/// Abstract base class for portal casting operations.
/// </summary>
public abstract class PortalCaster : MonoBehaviour
{
    /// <summary>
    /// Gets an array of portal rays from an origin matrix.
    /// </summary>
    /// <param name="origin">The origin matrix.</param>
    /// <param name="portalRays">The array to store the portal rays.</param>
    /// <param name="maxRecursions">The maximum number of recursions (portal transitions).</param>
    /// <param name="maxDistance">The maximum distance to cast.</param>
    /// <param name="layerMask">The layer mask for portal detection.</param>
    /// <param name="queryTriggerInteraction">Specifies whether to query triggers.</param>
    /// <returns>The number of portal rays found.</returns>
    public abstract int GetPortalRays(Matrix4x4 origin, ref PortalRay[] portalRays, int maxRecursions, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction);

    /// <summary>
    /// Casts a ray through a series of portals and returns hit information.
    /// </summary>
    /// <param name="portalRays">The array of portal rays to cast through.</param>
    /// <param name="rayCount">The number of rays in the array.</param>
    /// <param name="hitInfo">The hit information, if any.</param>
    /// <param name="rayIndex">The index of the ray that hit.</param>
    /// <param name="layerMask">The layer mask for raycast detection.</param>
    /// <param name="queryTriggerInteraction">Specifies whether to query triggers.</param>
    /// <returns>True if the ray hit something, false otherwise.</returns>
    public abstract bool Cast(PortalRay[] portalRays, int rayCount, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction);
}