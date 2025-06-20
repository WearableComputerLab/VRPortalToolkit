using System;
using UnityEngine;
using UnityEngine.Events;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.Pointers
{
    /// <summary>
    /// Main portal pointer class that handles raycasting through portals and manages hit detection.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class PortalPointer : MonoBehaviour
    {
        [Tooltip("The layer mask used for portal detection.")]
        [SerializeField] private LayerMask _portalMask = 1 << 3;
        /// <summary>
        /// The layer mask used for portal detection.
        /// </summary>
        public virtual LayerMask portalMask
        {
            get => _portalMask;
            set => _portalMask = value;
        }

        [Tooltip("Specifies whether to detect triggers when casting for portals.")]
        [SerializeField] private QueryTriggerInteraction _portalTriggerInteraction;
        /// <summary>
        /// Specifies whether to detect triggers when casting for portals.
        /// </summary>
        public virtual QueryTriggerInteraction portalTriggerInteraction
        {
            get => _portalTriggerInteraction;
            set => _portalTriggerInteraction = value;
        }

        [Tooltip("The maximum distance to cast.")]
        [SerializeField] private float _maxDistance = 10f;
        /// <summary>
        /// The maximum distance to cast.
        /// </summary>
        public virtual float maxDistance
        {
            get => _maxDistance;
            set => _maxDistance = value;
        }

        /// <summary>
        /// The current distance limited by the actual path through portals.
        /// </summary>
        public virtual float limitedDistance
        {
            get
            {
                if (portalRays == null || portalRaysCount != portalRays.Length)
                    return maxDistance;

                float distance = 0f;

                for (int i = 0; i < portalRaysCount; i++)
                    distance += portalRays[i].localDistance;

                return distance;
            }
        }

        [Tooltip("The maximum number of portal recursions allowed.")]
        [SerializeField] private int _maxRecursions = 32;
        /// <summary>
        /// The maximum number of portal recursions allowed.
        /// </summary>
        public virtual int maxRecursions
        {
            get => _maxRecursions;
            set => _maxRecursions = value;
        }

        [Tooltip("The layer mask used for raycast detection.")]
        [SerializeField] private LayerMask _raycastMask = ~0 & ~(1 << 2) & ~(1 << 3);
        /// <summary>
        /// The layer mask used for raycast detection.
        /// </summary>
        public virtual LayerMask raycastMask
        {
            get => _raycastMask;
            set => _raycastMask = value;
        }

        [Tooltip("Specifies whether to detect triggers when raycasting.")]
        [SerializeField] private QueryTriggerInteraction _raycastTriggerInteraction;
        /// <summary>
        /// Specifies whether to detect triggers when raycasting.
        /// </summary>
        public virtual QueryTriggerInteraction raycastTriggerInteraction
        {
            get => _raycastTriggerInteraction;
            set => _raycastTriggerInteraction = value;
        }

        /// <summary>
        /// Event invoked when the ray first hits something.
        /// </summary>
        public UnityAction<RaycastHit> raycastEntered;
        
        /// <summary>
        /// Event invoked when the ray stops hitting something.
        /// </summary>
        public UnityAction<RaycastHit> raycastExited;

        [Header("Optional"), Tooltip("The portal caster to use for the raycast.")]
        [SerializeField] private PortalCaster _portalCaster;
        /// <summary>
        /// The portal caster to use for the raycast.
        /// </summary>
        public PortalCaster portalCaster
        {
            get => _portalCaster;
            set => _portalCaster = value;
        }

        [Tooltip("The origin transform for the raycast.")]
        [SerializeField] private Transform _origin;
        /// <summary>
        /// The origin transform for the raycast.
        /// </summary>
        public virtual Transform origin {
            get => _origin;
            set => _origin = value;
        }

        [Tooltip("How the scale of the line is determined.")]
        [SerializeField] private ScaleSpace _scaleSpace = ScaleSpace.World;
        /// <summary>
        /// How the scale of the line is determined.
        /// </summary>
        public virtual ScaleSpace space {
            get => _scaleSpace;
            set => _scaleSpace = value;
        }

        /// <summary>
        /// Defines how the scale of the portal pointer is determined.
        /// </summary>
        public enum ScaleSpace
        {
            /// <summary>Use world space scaling.</summary>
            World = 0,
            /// <summary>Use local space scaling.</summary>
            Local = 1,
            /// <summary>Use origin transform scaling.</summary>
            Origin = 2
        }

        /// <summary>
        /// Gets the actual origin transform, falling back to this transform if not set.
        /// </summary>
        public Transform actualOrigin => origin ? origin : transform;

        private int _portalRaysCount = 0;
        /// <summary>
        /// The number of portal rays in the current raycast.
        /// </summary>
        public virtual int portalRaysCount => _portalRaysCount;
        
        /// <summary>
        /// Whether the pointer has a valid hit.
        /// </summary>
        public virtual bool isValid => hitInfo.collider;

        protected PortalRay[] castingPortalRays;
        protected PortalRay[] portalRays;
        protected RaycastHit hitInfo;
        protected int hitPortalRaysIndex = -1;

        protected PortalRay[] newPortalRays;

        protected virtual void Reset()
        {
            _portalMask = PortalPhysics.defaultPortalLayerMask;
            _raycastMask = ~0 & ~(1 << 2) & ~(_portalMask);
            portalCaster = GetComponentInChildren<PortalCaster>();
        }

        protected virtual void OnValidate()
        {
            maxDistance = _maxDistance;
        }

        protected virtual void OnEnable()
        {
            //
        }

        protected virtual void OnDisable()
        {
            if (hitPortalRaysIndex >= 0)
            {
                RaycastExited();
                hitInfo = default;
                hitPortalRaysIndex = -1;
                _portalRaysCount = 0;
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;

            if (TryGetHitInfo(out RaycastHit hitInfo, out int count))
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(GetPortalRay(count).origin, hitInfo.point);
            }
            else
                count = portalRaysCount;

            for (int i = 0; i < count; i++)
            {
                PortalRay ray = GetPortalRay(i);
                Gizmos.DrawRay(ray.origin, ray.direction);
            }
        }

        public virtual void Update()
        {
            Apply();
        }

        public virtual void FixedUpdate()
        {
            Apply();
        }

        /// <summary>
        /// Applies the portal pointer logic.
        /// </summary>
        public virtual void Apply()
        {
            if (_maxRecursions < 0) _maxRecursions = 0;

            bool raycastHit;
            int newPortalRaysCount, newHitInfoRayIndex;
            RaycastHit newHitInfo;

            Matrix4x4 originMatrix;
            Transform aOrigin = actualOrigin;

            switch (space)
            {
                case ScaleSpace.World:
                    if (aOrigin.parent)
                        originMatrix = Matrix4x4.TRS(aOrigin.position, aOrigin.rotation, Vector3.one);
                    else
                        originMatrix = aOrigin.localToWorldMatrix;
                    break;

                case ScaleSpace.Local:
                    if (transform == aOrigin)
                        originMatrix = aOrigin.localToWorldMatrix;
                    else
                        originMatrix = transform.localToWorldMatrix * Matrix4x4.LookAt(Vector3.zero, aOrigin.InverseTransformDirection(transform.position + aOrigin.forward), Vector3.up);
                    break;

                default: // ScaleSpace.Origin:
                    originMatrix = aOrigin.localToWorldMatrix;
                    break;
            }

            // Get portal rays
            if (_portalCaster)
                newPortalRaysCount = _portalCaster.GetPortalRays(originMatrix, ref newPortalRays, _maxRecursions, _maxDistance, _portalMask, _portalTriggerInteraction);
            else
            {
                if (newPortalRays == null || newPortalRays.Length != _maxRecursions) newPortalRays = new PortalRay[_maxRecursions];
                newPortalRaysCount = PortalPhysics.GetRays(new Raycaster(), originMatrix, newPortalRays, _maxDistance, _portalMask, _portalTriggerInteraction);
            }

            // Perform raycast
            if (_portalCaster)
                raycastHit = _portalCaster.Cast(newPortalRays, newPortalRaysCount, out newHitInfo, out newHitInfoRayIndex, _raycastMask, _raycastTriggerInteraction);
            else
                raycastHit = PortalPhysics.Raycast(newPortalRays, newPortalRaysCount, out newHitInfo, out newHitInfoRayIndex, _raycastMask, _raycastTriggerInteraction);

            // Now perform actual raycast
            if (raycastHit)
            {
                if (newHitInfo.collider != hitInfo.collider)
                {
                    // Exit the previous one
                    if (hitPortalRaysIndex >= 0)
                        RaycastExited();

                    SwapToNew(ref newHitInfo, newHitInfoRayIndex, newPortalRaysCount);

                    // Enter the raycast
                    RaycastEntered();
                }
                else
                    SwapToNew(ref newHitInfo, newHitInfoRayIndex, newPortalRaysCount);

                // Update the raycast
                //RaycastUpdated();
            }
            else
            {
                if (hitPortalRaysIndex >= 0)
                    RaycastExited();

                SwapToNew(ref newHitInfo, newHitInfoRayIndex, newPortalRaysCount);
            }
        }

        private void SwapToNew(ref RaycastHit newHitInfo, int newHitInfoRayIndex, int newPortalRaysCount)
        {
            PortalRay[] temp = newPortalRays;
            newPortalRays = portalRays;
            portalRays = temp;
            hitInfo = newHitInfo;
            hitPortalRaysIndex = newHitInfoRayIndex;
            _portalRaysCount = newPortalRaysCount;
        }

        /// <summary>
        /// Called when the raycast first hits something.
        /// </summary>
        protected virtual void RaycastEntered()
        {
            // Enter the raycast
            raycastEntered?.Invoke(hitInfo);
        }

        /// <summary>
        /// Called when the raycast stops hitting something.
        /// </summary>
        protected virtual void RaycastExited()
        {
            // Exit the previous one
            raycastExited?.Invoke(hitInfo);
        }

        /// <summary>
        /// Gets the portal rays and copies them to the provided array.
        /// </summary>
        /// <param name="portalRays">The array to copy portal rays to.</param>
        /// <returns>The number of portal rays copied.</returns>
        public virtual int GetPortalRays(PortalRay[] portalRays)
        {
            int count = _portalRaysCount > portalRays.Length ? portalRays.Length : _portalRaysCount;

            for (int i = 0; i < count; i++)
                portalRays[i] = this.portalRays[i];

            return count;
        }

        /// <summary>
        /// Gets a specific portal ray by index.
        /// </summary>
        /// <param name="index">The index of the portal ray to get.</param>
        /// <returns>The portal ray at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is out of range.</exception>
        public virtual PortalRay GetPortalRay(int index)
        {
            if (index < 0 || index >= _portalRaysCount) throw new IndexOutOfRangeException();

            return portalRays[index];
        }

        /// <summary>
        /// Tries to get the current hit information.
        /// </summary>
        /// <param name="hitInfo">Output parameter for the hit information.</param>
        /// <returns>True if there is a valid hit, false otherwise.</returns>
        public virtual bool TryGetHitInfo(out RaycastHit hitInfo) => TryGetHitInfo(out hitInfo, out int _);

        /// <summary>
        /// Tries to get the current hit information and the ray index that hit.
        /// </summary>
        /// <param name="hitInfo">Output parameter for the hit information.</param>
        /// <param name="portalRayIndex">Output parameter for the ray index that hit.</param>
        /// <returns>True if there is a valid hit, false otherwise.</returns>
        public virtual bool TryGetHitInfo(out RaycastHit hitInfo, out int portalRayIndex)
        {
            if (hitPortalRaysIndex >= 0)
            {
                hitInfo = this.hitInfo;
                portalRayIndex = hitPortalRaysIndex;

                return true;
            }

            portalRayIndex = -1;
            hitInfo = default(RaycastHit);
            return false;
        }

        /// <summary>
        /// Tries to get the current hit information, the ray index that hit, and the total hit distance.
        /// </summary>
        /// <param name="hitInfo">Output parameter for the hit information.</param>
        /// <param name="portalRayIndex">Output parameter for the ray index that hit.</param>
        /// <param name="hitDistance">Output parameter for the total distance to the hit point through portals.</param>
        /// <returns>True if there is a valid hit, false otherwise.</returns>
        public virtual bool TryGetHitInfo(out RaycastHit hitInfo, out int portalRayIndex, out float hitDistance)
        {
            if (TryGetHitInfo(out hitInfo, out portalRayIndex))
            {
                hitDistance = hitInfo.distance;

                for (int i = 0; i < portalRayIndex; i++)
                    hitDistance += portalRays[i].localDistance;

                return true;
            }

            hitDistance = 0f;
            return false;
        }
    }
}