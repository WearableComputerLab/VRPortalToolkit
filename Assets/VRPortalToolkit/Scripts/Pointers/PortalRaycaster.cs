using UnityEngine;
using VRPortalToolkit;
using VRPortalToolkit.Physics;

public class PortalRaycaster : PortalCaster
{
    public override int GetPortalRays(Matrix4x4 origin, ref PortalRay[] portalRays, int maxRecursions, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        if (portalRays == null || portalRays.Length != maxRecursions) portalRays = new PortalRay[maxRecursions];

        return PortalPhysics.GetRays(new Raycaster(), origin, portalRays, maxDistance, layerMask, queryTriggerInteraction);
    }

    public override bool Cast(PortalRay[] portalRays, int rayCount, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        return PortalPhysics.Cast(new Raycaster(), portalRays, rayCount, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);
    }
}