using UnityEngine;
using VRPortalToolkit;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.Pointers
{
    public class PortalBezierCaster : PortalCaster
    {
        [SerializeField] protected Transform _upright;
        public Transform upright
        {
            get => _upright;
            set => _upright = value;
        }

        [SerializeField] protected float _endPointDistance = 30f;
        public float endPointDistance
        {
            get => _endPointDistance;
            set => _endPointDistance = value;
        }

        [SerializeField] protected float _endPointHeight = -10f;
        public float endPointHeight
        {
            get => _endPointHeight;
            set => _endPointHeight = value;
        }

        [SerializeField] protected float _controlPointDistance = 10f;
        public float controlPointDistance
        {
            get => _controlPointDistance;
            set => _controlPointDistance = value;
        }

        [SerializeField] protected float _controlPointHeight = 5f;
        public float controlPointHeight
        {
            get => _controlPointHeight;
            set => _controlPointHeight = value;
        }

        [SerializeField] protected int _sampleFrequency = 20;
        public int sampleFrequency
        {
            get => _sampleFrequency;
            set => _sampleFrequency = value;
        }

        [Header("Optional"), SerializeField] private PortalCaster _portalCaster;
        public PortalCaster portalCaster
        {
            get => _portalCaster;
            set => _portalCaster = value;
        }

        protected PortalRay[] castingRays;

        // TODO: Increasing sample size decreases length for some reason
        public override int GetPortalRays(Matrix4x4 origin, ref PortalRay[] portalRays, int maxRecursions, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            if (portalRays == null || portalRays.Length != maxRecursions + _sampleFrequency)
                portalRays = new PortalRay[maxRecursions + _sampleFrequency];

            Matrix4x4 space = origin;

            Vector3 up = space.inverse.MultiplyVector(upright ? upright.up : Vector3.up),
                control = Vector3.forward * controlPointDistance + up * controlPointHeight,
                end = Vector3.forward * endPointDistance + up * endPointHeight,
                previousPoint = Vector3.zero, nextPoint;

            float nextTime, distance;

            PortalRay portalRay;
            int portalRaysCount = 0, portalCount;

            if (!_portalCaster && (castingRays == null || castingRays.Length != maxRecursions))
                castingRays = new PortalRay[maxRecursions];

            for (int i = 1; i < sampleFrequency; ++i)
            {
                if (maxDistance <= 0 || maxRecursions <= 0) return portalRaysCount;

                nextTime = i / (float)(sampleFrequency - 1);

                nextPoint = CalculateBezierPoint(nextTime, control, end);
                origin = space * Matrix4x4.LookAt(previousPoint, nextPoint, Vector3.up);

                // Need the distance in origin space
                distance = Mathf.Min(Vector3.Distance(previousPoint, nextPoint), maxDistance);
                maxDistance -= distance;

                if (_portalCaster)
                    portalCount = _portalCaster.GetPortalRays(origin, ref castingRays, maxRecursions, distance, layerMask, queryTriggerInteraction);
                else
                {
                    if (castingRays == null || castingRays.Length < maxRecursions) castingRays = new PortalRay[maxRecursions];
                    portalCount = PortalPhysics.GetRays(new Raycaster(), origin, castingRays, distance, layerMask, queryTriggerInteraction);
                }

                for (int j = 0; j < portalCount; j++)
                {
                    portalRay = castingRays[j];

                    if (portalRay.fromPortal)
                    {
                        if (maxRecursions <= 0) return portalRaysCount;

                        space = portalRay.fromPortal.ModifyMatrix(space);
                        maxRecursions -= 1;
                    }

                    portalRays[portalRaysCount++] = portalRay;
                    maxRecursions -= 1;

                    if (portalRaysCount >= portalRays.Length) return portalRaysCount;
                }

                previousPoint = nextPoint;
            }

            return portalRaysCount;
        }

        protected static Vector3 CalculateBezierPoint(float t, Vector3 control, Vector3 end)
        {
            return 2f * (1f - t) * t * control + Mathf.Pow(t, 2f) * end;
        }

        public override bool Cast(PortalRay[] portalRays, int rayCount, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            if (_portalCaster)
                return _portalCaster.Cast(portalRays, rayCount, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);

            return PortalPhysics.Cast(new Raycaster(), portalRays, rayCount, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);
        }
    }
}