using UnityEngine;
using VRPortalToolkit;
using VRPortalToolkit.Physics;

/// <summary>
/// Implements a sphere-based portal casting.
/// </summary>
public class PortalSphereCaster : PortalCaster
{
    [Tooltip("The radius of the sphere used for casting.")]
    [SerializeField] private float _radius = 0.5f;
    /// <summary>
    /// The radius of the sphere used for casting.
    /// </summary>
    public float radius
    {
        get => _radius;
        set => _radius = value;
    }

    /// <summary>
    /// Gets an array of portal rays using a sphere caster.
    /// </summary>
    /// <inheritdoc/>
    public override int GetPortalRays(Matrix4x4 origin, ref PortalRay[] portalRays, int maxRecursions, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        if (portalRays == null || portalRays.Length != maxRecursions) portalRays = new PortalRay[maxRecursions];

        return PortalPhysics.GetRays(new SphereCaster(_radius), origin, portalRays, maxDistance, layerMask, queryTriggerInteraction);
    }

    /// <summary>
    /// Casts a sphere through a series of portals.
    /// </summary>
    /// <inheritdoc/>
    public override bool Cast(PortalRay[] portalRays, int rayCount, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        return PortalPhysics.Cast(new SphereCaster(_radius), portalRays, rayCount, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);
    }
}