using System;
using UnityEngine;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit
{
    public static partial class PortalPhysics
    {
        #region Raycast

        public static bool BoxCast(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, LayerMask portalLayerMask, out RaycastHit hitInfo, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new BoxCaster(halfExtents, orientation), Matrix4x4.LookAt(origin, origin + direction, Vector3.up), portalLayerMask, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);

        public static bool BoxCast(PortalRay[] rays, Vector3 halfExtents, Quaternion orientation, out RaycastHit hitInfo, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new BoxCaster(halfExtents, orientation), rays, out hitInfo, layerMask, queryTriggerInteraction);

        public static bool BoxCast(PortalRay[] rays, Vector3 halfExtents, Quaternion orientation, int rayCount, out RaycastHit hitInfo, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new BoxCaster(halfExtents, orientation), rays, rayCount, out hitInfo, out int rayIndex, layerMask, queryTriggerInteraction);
        
        public static bool BoxCast(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, LayerMask portalLayerMask, out RaycastHit hitInfo, out int rayIndex, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new BoxCaster(halfExtents, orientation), Matrix4x4.LookAt(origin, origin + direction, Vector3.up), portalLayerMask, out hitInfo, out rayIndex, maxDistance, layerMask, queryTriggerInteraction);

        public static bool BoxCast(PortalRay[] rays, Vector3 halfExtents, Quaternion orientation, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new BoxCaster(halfExtents, orientation), rays, rays.Length, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);

        public static bool BoxCast(PortalRay[] rays, Vector3 halfExtents, Quaternion orientation, int rayCount, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new BoxCaster(halfExtents, orientation), rays, rayCount, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);
        
        #endregion
        
        #region Raycast All

        public static RaycastHit[] BoxCastAll(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, LayerMask portalLayerMask, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastAll(new BoxCaster(halfExtents, orientation), Matrix4x4.LookAt(origin, origin + direction, Vector3.up), portalLayerMask, maxDistance, layerMask, queryTriggerInteraction);

        public static RaycastHit[] BoxCastAll(PortalRay[] rays, Vector3 halfExtents, Quaternion orientation, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastAll(new BoxCaster(halfExtents, orientation), rays, rays.Length, layerMask, queryTriggerInteraction);

        public static RaycastHit[] BoxCastAll(PortalRay[] rays, Vector3 halfExtents, Quaternion orientation, int rayCount, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastAll(new BoxCaster(halfExtents, orientation), rays, rayCount, null, layerMask, queryTriggerInteraction);
            
        public static RaycastHit[] BoxCastAll(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, LayerMask portalLayerMask, float maxDistance, out int[] rayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => CastAll(new BoxCaster(halfExtents, orientation), Matrix4x4.LookAt(origin, origin + direction, Vector3.up), portalLayerMask, maxDistance, out rayIndices, layerMask, queryTriggerInteraction);

        public static RaycastHit[] BoxCastAll(PortalRay[] rays, Vector3 halfExtents, Quaternion orientation, out int[] rayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => CastAll(new BoxCaster(halfExtents, orientation), rays, rays.Length, out rayIndices, layerMask, queryTriggerInteraction);

        public static RaycastHit[] BoxCastAll(PortalRay[] rays, Vector3 halfExtents, Quaternion orientation, int rayCount, out int[] rayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => CastAll(new BoxCaster(halfExtents, orientation), rays, rays.Length, out rayIndices, layerMask, queryTriggerInteraction);

        #endregion

        #region Raycast Non Alloc

        public static int BoxCastNonAlloc(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, RaycastHit[] results, LayerMask portalLayerMask, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new BoxCaster(halfExtents, orientation), Matrix4x4.LookAt(origin, origin + direction, Vector3.up), portalLayerMask, maxDistance, results, layerMask, queryTriggerInteraction);
        
        public static int BoxCastNonAlloc(PortalRay[] rays, Vector3 halfExtents, Quaternion orientation, RaycastHit[] results, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new BoxCaster(halfExtents, orientation), rays, rays.Length, results, layerMask, queryTriggerInteraction);

        public static int BoxCastNonAlloc(PortalRay[] rays, Vector3 halfExtents, Quaternion orientation, int rayCount, RaycastHit[] results, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new BoxCaster(halfExtents, orientation), rays, rayCount, results, null, layerMask, queryTriggerInteraction);

        public static int BoxCastNonAlloc(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, RaycastHit[] results, int[] resultRayIndices, LayerMask portalLayerMask, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new BoxCaster(halfExtents, orientation), Matrix4x4.LookAt(origin, origin + direction, Vector3.up), portalLayerMask, maxDistance, results, resultRayIndices, layerMask, queryTriggerInteraction);
        
        public static int BoxCastNonAlloc(PortalRay[] rays, Vector3 halfExtents, Quaternion orientation, RaycastHit[] results, int[] resultRayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new BoxCaster(halfExtents, orientation), rays, rays.Length, results, resultRayIndices, layerMask, queryTriggerInteraction);

        public static int BoxCastNonAlloc(PortalRay[] rays, Vector3 halfExtents, Quaternion orientation, int rayCount, RaycastHit[] results, int[] resultRayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new BoxCaster(halfExtents, orientation), rays, rays.Length, results, resultRayIndices, layerMask, queryTriggerInteraction);
        
        #endregion

        #region Portal Rays

        public static PortalRay[] GetBoxRays(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => GetRays(new BoxCaster(halfExtents, orientation), Matrix4x4.LookAt(origin, origin + direction, Vector3.up), maxDistance, layerMask, queryTriggerInteraction);

        public static int GetBoxRays(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, PortalRay[] rays, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => GetRays(new BoxCaster(halfExtents, orientation), Matrix4x4.LookAt(origin, origin + direction, Vector3.up), rays, maxDistance, layerMask, queryTriggerInteraction);

        #endregion
    }
}