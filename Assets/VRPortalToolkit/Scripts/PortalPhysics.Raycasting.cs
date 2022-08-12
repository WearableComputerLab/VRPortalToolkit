using System;
using UnityEngine;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit
{
    public static partial class PortalPhysics
    {
        #region Raycast

        public static bool Raycast(Ray ray, LayerMask portalLayerMask, out RaycastHit hitInfo, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new Raycaster(), Matrix4x4.LookAt(ray.origin, ray.origin + ray.direction, Vector3.up), portalLayerMask, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);

        public static bool Raycast(Vector3 origin, Vector3 direction, LayerMask portalLayerMask, out RaycastHit hitInfo, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new Raycaster(), Matrix4x4.LookAt(origin, origin + direction, Vector3.up), portalLayerMask, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);

        public static bool Raycast(PortalRay[] rays, out RaycastHit hitInfo, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new Raycaster(), rays, out hitInfo, layerMask, queryTriggerInteraction);

        public static bool Raycast(PortalRay[] rays, int rayCount, out RaycastHit hitInfo, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new Raycaster(), rays, rayCount, out hitInfo, out int rayIndex, layerMask, queryTriggerInteraction);
        
        public static bool Raycast(Ray ray, LayerMask portalLayerMask, out RaycastHit hitInfo, out int rayIndex, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new Raycaster(), Matrix4x4.LookAt(ray.origin, ray.origin + ray.direction, Vector3.up), portalLayerMask, out hitInfo, out rayIndex, maxDistance, layerMask, queryTriggerInteraction);

        public static bool Raycast(Vector3 origin, Vector3 direction, LayerMask portalLayerMask, out RaycastHit hitInfo, out int rayIndex, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new Raycaster(), Matrix4x4.LookAt(origin, origin + direction, Vector3.up), portalLayerMask, out hitInfo, out rayIndex, maxDistance, layerMask, queryTriggerInteraction);

        public static bool Raycast(PortalRay[] rays, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new Raycaster(), rays, rays.Length, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);

        public static bool Raycast(PortalRay[] rays, int rayCount, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new Raycaster(), rays, rayCount, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);
        
        #endregion
        
        #region Raycast All

        public static RaycastHit[] RaycastAll(Ray ray, LayerMask portalLayerMask, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastAll(new Raycaster(), Matrix4x4.LookAt(ray.origin, ray.origin + ray.direction, Vector3.up), portalLayerMask, maxDistance, layerMask, queryTriggerInteraction);

        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, LayerMask portalLayerMask, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastAll(new Raycaster(), Matrix4x4.LookAt(origin, origin + direction, Vector3.up), portalLayerMask, maxDistance, layerMask, queryTriggerInteraction);

        public static RaycastHit[] RaycastAll(PortalRay[] rays, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastAll(new Raycaster(), rays, rays.Length, layerMask, queryTriggerInteraction);

        public static RaycastHit[] RaycastAll(PortalRay[] rays, int rayCount, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastAll(new Raycaster(), rays, rayCount, null, layerMask, queryTriggerInteraction);
            
        public static RaycastHit[] RaycastAll(Ray ray, LayerMask portalLayerMask, float maxDistance, out int[] rayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => CastAll(new Raycaster(), Matrix4x4.LookAt(ray.origin, ray.origin + ray.direction, Vector3.up), portalLayerMask, maxDistance, out rayIndices, layerMask, queryTriggerInteraction);

        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, LayerMask portalLayerMask, float maxDistance, out int[] rayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => CastAll(new Raycaster(), Matrix4x4.LookAt(origin, origin + direction, Vector3.up), portalLayerMask, maxDistance, out rayIndices, layerMask, queryTriggerInteraction);

        public static RaycastHit[] RaycastAll(PortalRay[] rays, out int[] rayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => CastAll(new Raycaster(), rays, rays.Length, out rayIndices, layerMask, queryTriggerInteraction);

        public static RaycastHit[] RaycastAll(PortalRay[] rays, int rayCount, out int[] rayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => CastAll(new Raycaster(), rays, rays.Length, out rayIndices, layerMask, queryTriggerInteraction);

        #endregion

        #region Raycast Non Alloc

        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, LayerMask portalLayerMask, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new Raycaster(), Matrix4x4.LookAt(ray.origin, ray.origin + ray.direction, Vector3.up), portalLayerMask, maxDistance, results, layerMask, queryTriggerInteraction);
        
        public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, LayerMask portalLayerMask, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new Raycaster(), Matrix4x4.LookAt(origin, origin + direction, Vector3.up), portalLayerMask, maxDistance, results, layerMask, queryTriggerInteraction);
        
        public static int RaycastNonAlloc(PortalRay[] rays, RaycastHit[] results, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new Raycaster(), rays, rays.Length, results, layerMask, queryTriggerInteraction);

        public static int RaycastNonAlloc(PortalRay[] rays, int rayCount, RaycastHit[] results, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new Raycaster(), rays, rayCount, results, null, layerMask, queryTriggerInteraction);

        public static int RaycastNonAlloc(Ray ray, RaycastHit[] results, int[] resultRayIndices, LayerMask portalLayerMask, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new Raycaster(), Matrix4x4.LookAt(ray.origin, ray.origin + ray.direction, Vector3.up), portalLayerMask, maxDistance, results, resultRayIndices, layerMask, queryTriggerInteraction);
        
        public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results, int[] resultRayIndices, LayerMask portalLayerMask, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new Raycaster(), Matrix4x4.LookAt(origin, origin + direction, Vector3.up), portalLayerMask, maxDistance, results, resultRayIndices, layerMask, queryTriggerInteraction);
        
        public static int RaycastNonAlloc(PortalRay[] rays, RaycastHit[] results, int[] resultRayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new Raycaster(), rays, rays.Length, results, resultRayIndices, layerMask, queryTriggerInteraction);

        public static int RaycastNonAlloc(PortalRay[] rays, int rayCount, RaycastHit[] results, int[] resultRayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new Raycaster(), rays, rays.Length, results, resultRayIndices, layerMask, queryTriggerInteraction);
        
        #endregion

        #region Portal Rays

        public static PortalRay[] GetRays(Ray ray, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => GetRays(new Raycaster(), Matrix4x4.LookAt(ray.origin, ray.origin + ray.direction, Vector3.up), maxDistance, layerMask, queryTriggerInteraction);

        public static PortalRay[] GetRays(Vector3 origin, Vector3 direction, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => GetRays(new Raycaster(), Matrix4x4.LookAt(origin, origin + direction, Vector3.up), maxDistance, layerMask, queryTriggerInteraction);

        public static int GetRays(Ray ray, PortalRay[] rays, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => GetRays(new Raycaster(), Matrix4x4.LookAt(ray.origin, ray.origin + ray.direction, Vector3.up), rays, maxDistance, layerMask, queryTriggerInteraction);

        public static int GetRays(Vector3 origin, Vector3 direction, PortalRay[] rays, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => GetRays(new Raycaster(), Matrix4x4.LookAt(origin, origin + direction, Vector3.up), rays, maxDistance, layerMask, queryTriggerInteraction);
        #endregion
    }
}