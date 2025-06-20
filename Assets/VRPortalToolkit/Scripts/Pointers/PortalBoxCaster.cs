using UnityEngine;
using VRPortalToolkit;
using VRPortalToolkit.Physics;

/// <summary>
/// Implements a box-based portal casting.
/// </summary>
public class PortalBoxCaster : PortalCaster
{
    [Tooltip("The half extents of the box used for casting.")]
    [SerializeField] private Vector3 _halfExtents = Vector3.one;
    /// <summary>
    /// The half extents of the box used for casting.
    /// </summary>
    public Vector3 halfExtents
    {
        get => _halfExtents;
        set => _halfExtents = value;
    }

    private Quaternion _actualOrientation = Quaternion.identity;
    [Tooltip("The orientation of the box in Euler angles.")]
    [SerializeField] private Vector3 _orientation;
    /// <summary>
    /// The orientation of the box as a quaternion.
    /// </summary>
    public Quaternion orientation
    {
        get => _actualOrientation;
        set => _actualOrientation = value;
    }

    public virtual void Awake()
    {
        orientation = Quaternion.Euler(_orientation);
    }

    public virtual void OnValidate()
    {
        orientation = Quaternion.Euler(_orientation);
    }

    /// <summary>
    /// Gets an array of portal rays using a box caster.
    /// </summary>
    /// <inheritdoc/>
    public override int GetPortalRays(Matrix4x4 origin, ref PortalRay[] portalRays, int maxRecursions, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        if (portalRays == null || portalRays.Length != maxRecursions) portalRays = new PortalRay[maxRecursions];

        return PortalPhysics.GetRays(new BoxCaster(_halfExtents, _actualOrientation), origin, portalRays, maxDistance, layerMask, queryTriggerInteraction);
    }

    /// <summary>
    /// Casts a box through a series of portals.
    /// </summary>
    /// <inheritdoc/>
    public override bool Cast(PortalRay[] portalRays, int rayCount, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        return PortalPhysics.Cast(new BoxCaster(_halfExtents, _actualOrientation), portalRays, rayCount, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);
    }
}