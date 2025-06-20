using UnityEngine;
using VRPortalToolkit;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.Pointers
{
    /// <summary>
    /// Implements a projectile-based portal casting that simulates the path of a projectile under gravity.
    /// </summary>
    public class PortalProjectileCaster : PortalCaster
    {
        [Tooltip("The transform used to determine the up direction for gravity.")]
        [SerializeField] private Transform _upright;
        /// <summary>
        /// The transform used to determine the up direction for gravity.
        /// </summary>
        public virtual Transform upright
        {
            get => _upright;
            set => _upright = value;
        }

        [Tooltip("The initial velocity of the projectile.")]
        [SerializeField] private float _velocity = 16f;
        /// <summary>
        /// The initial velocity of the projectile.
        /// </summary>
        public virtual float velocity
        {
            get => _velocity;
            set => _velocity = value;
        }

        [Tooltip("The acceleration due to gravity applied to the projectile.")]
        [SerializeField] private float _acceleration = 9.8f;
        /// <summary>
        /// The acceleration due to gravity applied to the projectile.
        /// </summary>
        public virtual float acceleration
        {
            get => _acceleration;
            set => _acceleration = value;
        }

        [Tooltip("Additional flight time to add to the calculated trajectory.")]
        [SerializeField] private float _additionalFlightTime = 0.5f;
        /// <summary>
        /// Additional flight time to add to the calculated trajectory.
        /// </summary>
        public virtual float additionalFlightTime
        {
            get => _additionalFlightTime;
            set => _additionalFlightTime = value;
        }

        [Tooltip("The number of sample points along the projectile path.")]
        [SerializeField] private int _sampleFrequency = 20;
        /// <summary>
        /// The number of sample points along the projectile path.
        /// </summary>
        public virtual int sampleFrequency
        {
            get => _sampleFrequency;
            set => _sampleFrequency = value;
        }

        [Header("Optional"), Tooltip("Optional portal caster to use for actual casting.")]
        [SerializeField] private PortalCaster _portalCaster;
        /// <summary>
        /// Optional portal caster to use for actual casting.
        /// </summary>
        public virtual PortalCaster portalCaster
        {
            get => _portalCaster;
            set => _portalCaster = value;
        }

        protected PortalRay[] castingRays;

        /// <inheritdoc/>
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

        /// <summary>
        /// Calculates a point on a projectile trajectory.
        /// </summary>
        /// <param name="t">Time parameter.</param>
        /// <param name="velocity">The initial velocity vector.</param>
        /// <param name="acceleration">The acceleration vector (typically gravity).</param>
        /// <returns>The calculated point on the projectile trajectory.</returns>
        protected static Vector3 CalculateProjectilePoint(float t, Vector3 velocity, Vector3 acceleration)
        {
            return velocity * t + 0.5f * acceleration * t * t;
        }

        /// <inheritdoc/>
        public override bool Cast(PortalRay[] portalRays, int rayCount, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            if (_portalCaster)
                return _portalCaster.Cast(portalRays, rayCount, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);

            return PortalPhysics.Cast(new Raycaster(), portalRays, rayCount, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);
        }
    }
}