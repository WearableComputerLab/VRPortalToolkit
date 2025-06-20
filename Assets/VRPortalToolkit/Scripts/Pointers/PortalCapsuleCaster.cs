using UnityEngine;
using VRPortalToolkit;
using VRPortalToolkit.Physics;

/// <summary>
/// Implements a capsule-based portal casting.
/// </summary>
public class PortalCapsuleCaster : PortalCaster
{
    [Tooltip("The offset from the origin to the capsule's end point.")]
    [SerializeField] private Vector3 _offset = Vector3.up;
    /// <summary>
    /// The offset from the origin to the capsule's end point.
    /// </summary>
    public Vector3 offset
    {
        get => _offset;
        set => _offset = value;
    }

    [Tooltip("The radius of the capsule used for casting.")]
    [SerializeField] private float _radius = 0.5f;
    /// <summary>
    /// The radius of the capsule used for casting.
    /// </summary>
    public float radius
    {
        get => _radius;
        set => _radius = value;
    }

    /// <summary>
    /// Gets an array of portal rays using a capsule caster.
    /// </summary>
    /// <inheritdoc/>
    public override int GetPortalRays(Matrix4x4 origin, ref PortalRay[] portalRays, int maxRecursions, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        if (portalRays == null || portalRays.Length != maxRecursions) portalRays = new PortalRay[maxRecursions];

        return PortalPhysics.GetRays(new CapsuleCaster(_offset, _radius), origin, portalRays, maxDistance, layerMask, queryTriggerInteraction);
    }

    /// <summary>
    /// Casts a capsule through a series of portals.
    /// </summary>
    /// <inheritdoc/>
    public override bool Cast(PortalRay[] portalRays, int rayCount, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        return PortalPhysics.Cast(new CapsuleCaster(_offset, _radius), portalRays, rayCount, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);
    }
}