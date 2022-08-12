using UnityEngine;
using VRPortalToolkit;
using VRPortalToolkit.Physics;

public class PortalBoxCaster : PortalCaster
{
    [SerializeField] private Vector3 _halfExtents = Vector3.one;
    public Vector3 halfExtents
    {
        get => _halfExtents;
        set => _halfExtents = value;
    }

    private Quaternion _actualOrientation = Quaternion.identity;
    [SerializeField] private Vector3 _orientation;
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

    public override int GetPortalRays(Matrix4x4 origin, ref PortalRay[] portalRays, int maxRecursions, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        if (portalRays == null || portalRays.Length != maxRecursions) portalRays = new PortalRay[maxRecursions];

        return PortalPhysics.GetRays(new BoxCaster(_halfExtents, _actualOrientation), origin, portalRays, maxDistance, layerMask, queryTriggerInteraction);
    }

    public override bool Cast(PortalRay[] portalRays, int rayCount, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
    {
        return PortalPhysics.Cast(new BoxCaster(_halfExtents, _actualOrientation), portalRays, rayCount, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);
    }
}