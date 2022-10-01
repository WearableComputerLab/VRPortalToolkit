using System;
using UnityEngine;
using UnityEngine.Events;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit.Pointers
{
    [DefaultExecutionOrder(100)]
    public class PortalPointer : MonoBehaviour
    {
        [SerializeField] private LayerMask _portalMask = 1 << 3; // TODO: change this on reset to default to a user defined value
        public virtual LayerMask portalMask
        {
            get => _portalMask;
            set => _portalMask = value;
        }

        [SerializeField] private QueryTriggerInteraction _portalTriggerInteraction;
        public virtual QueryTriggerInteraction portalTriggerInteraction
        {
            get => _portalTriggerInteraction;
            set => _portalTriggerInteraction = value;
        }

        [SerializeField] private float _maxDistance = 10f;
        public virtual float maxDistance
        {
            get => _maxDistance;
            set => _maxDistance = value;
        }

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

        [SerializeField] private int _maxRecursions = 32;
        public virtual int maxRecursions
        {
            get => _maxRecursions;
            set => _maxRecursions = value;
        }

        [SerializeField] private LayerMask _raycastMask = ~0 & ~(1 << 2) & ~(1 << 3);
        public virtual LayerMask raycastMask
        {
            get => _raycastMask;
            set => _raycastMask = value;
        }

        [SerializeField] private QueryTriggerInteraction _raycastTriggerInteraction;
        public virtual QueryTriggerInteraction raycastTriggerInteraction
        {
            get => _raycastTriggerInteraction;
            set => _raycastTriggerInteraction = value;
        }

        [Header("Raycast Events")]
        public UnityEvent<PortalPointer> onRaycastEntered = new UnityEvent<PortalPointer>();
        //public UnityEvent<PortalPointer> onRaycastStay;
        public UnityEvent<PortalPointer> onRaycastExited = new UnityEvent<PortalPointer>();

        [Header("Optional"), SerializeField] private PortalCaster _portalCaster;
        public PortalCaster portalCaster
        {
            get => _portalCaster;
            set => _portalCaster = value;
        }

        [SerializeField] private Transform _origin;
        public virtual Transform origin {
            get => _origin;
            set => _origin = value;
        }

        // How is scale of the line determined
        [SerializeField] private ScaleSpace _scaleSpace = ScaleSpace.World;
        public virtual ScaleSpace space {
            get => _scaleSpace;
            set => _scaleSpace = value;
        }

        public enum ScaleSpace
        {
            World = 0,
            Local = 1,
            Origin = 2
        }

        public Transform actualOrigin => origin ? origin : transform;

        private int _portalRaysCount = 0;
        public virtual int portalRaysCount => _portalRaysCount;
        public virtual bool isValid => hitInfo.collider;

        protected PortalRay[] castingPortalRays;
        protected PortalRay[] portalRays;
        protected RaycastHit hitInfo;
        protected int hitPortalRaysIndex = -1;

        protected PortalRay[] newPortalRays;

        protected virtual void Reset()
        {
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
                hitInfo = default(RaycastHit);
                hitPortalRaysIndex = -1;
                _portalRaysCount = 0;
            }
        }

        protected virtual void OnDrawGizmos()
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

        public virtual void FixedUpdate()
        {
            Apply();
        }

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

        protected virtual void RaycastEntered()
        {
            // Enter the raycast
            onRaycastEntered?.Invoke(this);
        }

        /*protected virtual void RaycastUpdated()
        {
            // Update the raycast
            if (onRaycastStay != null) onRaycastStay.Invoke(this);
        }*/

        protected virtual void RaycastExited()
        {
            // Exit the previous one
            onRaycastExited?.Invoke(this);
        }

        public virtual int GetPortalRays(PortalRay[] portalRays)
        {
            int count = _portalRaysCount > portalRays.Length ? portalRays.Length : _portalRaysCount;

            for (int i = 0; i < count; i++)
                portalRays[i] = this.portalRays[i];

            return count;
        }

        /// <inheritdoc />
        public virtual PortalRay GetPortalRay(int index)
        {
            if (index < 0 || index >= _portalRaysCount) throw new IndexOutOfRangeException();

            return portalRays[index];
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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