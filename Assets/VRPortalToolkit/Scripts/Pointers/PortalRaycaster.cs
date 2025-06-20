using UnityEngine;
using VRPortalToolkit;
using VRPortalToolkit.Physics;

/// <summary>
/// Implements a ray-based portal casting.
/// </summary>
public class PortalRaycaster : PortalCaster
{
    /// <summary>
    /// Gets an array of portal rays using a simple raycaster.
    /// </summary>
    /// <inheritdoc/>
    public override int GetPortalRays(Matrix4x4 origin, ref PortalRay[] portalRays, int maxRecursions, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        if (portalRays == null || portalRays.Length != maxRecursions) portalRays = new PortalRay[maxRecursions];

        return PortalPhysics.GetRays(new Raycaster(), origin, portalRays, maxDistance, layerMask, queryTriggerInteraction);
    }

    /// <summary>
    /// Casts a ray through a series of portals using a simple raycaster.
    /// </summary>
    /// <inheritdoc/>
    public override bool Cast(PortalRay[] portalRays, int rayCount, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        return PortalPhysics.Raycast(portalRays, rayCount, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);
    }
}