using UnityEngine;
using VRPortalToolkit;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.Pointers
{
    public class PortalProjectileCaster : PortalCaster
    {
        [SerializeField] private Transform _upright;
        public virtual Transform upright
        {
            get => _upright;
            set => _upright = value;
        }

        [SerializeField] private float _velocity = 16f;
        public virtual float velocity
        {
            get => _velocity;
            set => _velocity = value;
        }

        [SerializeField] private float _acceleration = 9.8f;
        public virtual float acceleration
        {
            get => _acceleration;
            set => _acceleration = value;
        }

        [SerializeField] private float _additionalFlightTime = 0.5f;
        public virtual float additionalFlightTime
        {
            get => _additionalFlightTime;
            set => _additionalFlightTime = value;
        }

        [SerializeField] private int _sampleFrequency = 20;
        public virtual int sampleFrequency
        {
            get => _sampleFrequency;
            set => _sampleFrequency = value;
        }

        [Header("Optional"), SerializeField] private PortalCaster _portalCaster;
        public virtual PortalCaster portalCaster
        {
            get => _portalCaster;
            set => _portalCaster = value;
        }

        protected PortalRay[] castingRays;

        // TODO: Increasing sample size decreases length for some reason
        public override int GetPortalRays(Matrix4x4 origin, ref PortalRay[] portalRays, int maxRecursions, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            if (portalRays == null || portalRays.Length < maxRecursions + _sampleFrequency)
                portalRays = new PortalRay[maxRecursions + _sampleFrequency];

            // Up should be relative to space
            Matrix4x4 space = origin;

            Vector3 up = space.inverse.MultiplyVector(upright ? upright.up : Vector3.up),
                projectedForward = Vector3.ProjectOnPlane(Vector3.forward, up),
                velocityVector = Vector3.forward * velocity,
                accelerationVector = up * -1f * acceleration,
                previousPoint = Vector3.zero, nextPoint;

            float angle = Mathf.Approximately(Vector3.Angle(Vector3.forward, projectedForward), 0f)
                ? 0f : Vector3.SignedAngle(Vector3.forward, projectedForward, Vector3.Cross(Vector3.forward, projectedForward)),
                flightTime = 2f * velocity * Mathf.Sin(Mathf.Abs(angle) * Mathf.Deg2Rad) / _acceleration + _additionalFlightTime,
                nextTime, distance;

            PortalRay portalRay;
            int portalRaysCount = 0, portalCount;

            if (!_portalCaster && (castingRays == null || castingRays.Length != maxRecursions))
                castingRays = new PortalRay[maxRecursions];

            for (int i = 1; i < sampleFrequency; ++i)
            {
                if (maxDistance <= 0 || maxRecursions <= 0) return portalRaysCount;

                nextTime = i / (float)(sampleFrequency - 1) * flightTime;

                nextPoint = CalculateProjectilePoint(nextTime, velocityVector, accelerationVector);
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

                    if (portalRaysCount >= portalRays.Length) return portalRaysCount;
                }

                previousPoint = nextPoint;
            }

            return portalRaysCount;
        }

        protected static Vector3 CalculateProjectilePoint(float t, Vector3 velocity, Vector3 acceleration)
        {
            return velocity * t + 0.5f * acceleration * t * t;
        }

        public override bool Cast(PortalRay[] portalRays, int rayCount, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            if (_portalCaster)
                return _portalCaster.Cast(portalRays, rayCount, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);

            return PortalPhysics.Cast(new Raycaster(), portalRays, rayCount, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);
        }
    }
}