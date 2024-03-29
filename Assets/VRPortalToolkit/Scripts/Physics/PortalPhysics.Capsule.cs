using UnityEngine;

namespace VRPortalToolkit.Physics
{
    public static partial class PortalPhysics
    {
        #region Raycast

        public static bool CapsuleCast(Vector3 start, Vector3 end, float radius, Vector3 direction, LayerMask portalLayerMask, out RaycastHit hitInfo, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new CapsuleCaster(end - start, radius), Matrix4x4.LookAt(start, start + direction, Vector3.up), portalLayerMask, out hitInfo, maxDistance, layerMask, queryTriggerInteraction);

        public static bool CapsuleCast(PortalRay[] rays, Vector3 endOffset, float radius, out RaycastHit hitInfo, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new CapsuleCaster(endOffset, radius), rays, out hitInfo, layerMask, queryTriggerInteraction);

        public static bool CapsuleCast(PortalRay[] rays, Vector3 endOffset, float radius, int rayCount, out RaycastHit hitInfo, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new CapsuleCaster(endOffset, radius), rays, rayCount, out hitInfo, out int rayIndex, layerMask, queryTriggerInteraction);
        
        public static bool CapsuleCast(Vector3 start, Vector3 end, float radius, Vector3 direction, LayerMask portalLayerMask, out RaycastHit hitInfo, out int rayIndex, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new CapsuleCaster(end - start, radius), Matrix4x4.LookAt(start, start + direction, Vector3.up), portalLayerMask, out hitInfo, out rayIndex, maxDistance, layerMask, queryTriggerInteraction);

        public static bool CapsuleCast(PortalRay[] rays, Vector3 endOffset, float radius, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new CapsuleCaster(endOffset, radius), rays, rays.Length, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);

        public static bool CapsuleCast(PortalRay[] rays, Vector3 endOffset, float radius, int rayCount, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(new CapsuleCaster(endOffset, radius), rays, rayCount, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);
        
        #endregion
        
        #region Raycast All

        public static RaycastHit[] CapsuleCastAll(Vector3 start, Vector3 end, float radius, Vector3 direction, LayerMask portalLayerMask, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastAll(new CapsuleCaster(end - start, radius), Matrix4x4.LookAt(start, start + direction, Vector3.up), portalLayerMask, maxDistance, layerMask, queryTriggerInteraction);

        public static RaycastHit[] CapsuleCastAll(PortalRay[] rays, Vector3 endOffset, float radius, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastAll(new CapsuleCaster(endOffset, radius), rays, rays.Length, layerMask, queryTriggerInteraction);

        public static RaycastHit[] CapsuleCastAll(PortalRay[] rays, Vector3 endOffset, float radius, int rayCount, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastAll(new CapsuleCaster(endOffset, radius), rays, rayCount, null, layerMask, queryTriggerInteraction);
            
        public static RaycastHit[] CapsuleCastAll(Vector3 start, Vector3 end, float radius, Vector3 direction, LayerMask portalLayerMask, float maxDistance, out int[] rayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => CastAll(new CapsuleCaster(end - start, radius), Matrix4x4.LookAt(start, start + direction, Vector3.up), portalLayerMask, maxDistance, out rayIndices, layerMask, queryTriggerInteraction);

        public static RaycastHit[] CapsuleCastAll(PortalRay[] rays, Vector3 endOffset, float radius, out int[] rayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => CastAll(new CapsuleCaster(endOffset, radius), rays, rays.Length, out rayIndices, layerMask, queryTriggerInteraction);

        public static RaycastHit[] CapsuleCastAll(PortalRay[] rays, Vector3 endOffset, float radius, int rayCount, out int[] rayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => CastAll(new CapsuleCaster(endOffset, radius), rays, rays.Length, out rayIndices, layerMask, queryTriggerInteraction);

        #endregion

        #region Raycast Non Alloc

        public static int CapsuleCastNonAlloc(Vector3 start, Vector3 end, float radius, Vector3 direction, RaycastHit[] results, LayerMask portalLayerMask, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new CapsuleCaster(end - start, radius), Matrix4x4.LookAt(start, start + direction, Vector3.up), portalLayerMask, maxDistance, results, layerMask, queryTriggerInteraction);
        
        public static int CapsuleCastNonAlloc(PortalRay[] rays, Vector3 endOffset, float radius, RaycastHit[] results, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new CapsuleCaster(endOffset, radius), rays, rays.Length, results, layerMask, queryTriggerInteraction);

        public static int CapsuleCastNonAlloc(PortalRay[] rays, Vector3 endOffset, float radius, int rayCount, RaycastHit[] results, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new CapsuleCaster(endOffset, radius), rays, rayCount, results, null, layerMask, queryTriggerInteraction);

        public static int CapsuleCastNonAlloc(Vector3 start, Vector3 end, float radius, Vector3 direction, RaycastHit[] results, int[] resultRayIndices, LayerMask portalLayerMask, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new CapsuleCaster(end - start, radius), Matrix4x4.LookAt(start, start + direction, Vector3.up), portalLayerMask, maxDistance, results, resultRayIndices, layerMask, queryTriggerInteraction);
        
        public static int CapsuleCastNonAlloc(PortalRay[] rays, Vector3 endOffset,float radius, RaycastHit[] results, int[] resultRayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new CapsuleCaster(endOffset, radius), rays, rays.Length, results, resultRayIndices, layerMask, queryTriggerInteraction);

        public static int CapsuleCastNonAlloc(PortalRay[] rays, Vector3 endOffset, float radius, int rayCount, RaycastHit[] results, int[] resultRayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(new CapsuleCaster(endOffset, radius), rays, rays.Length, results, resultRayIndices, layerMask, queryTriggerInteraction);
        
        #endregion

        #region Portal Rays

        public static PortalRay[] GetCapsuleRays(Vector3 start, Vector3 end, float radius, Vector3 direction, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => GetRays(new CapsuleCaster(end - start, radius), Matrix4x4.LookAt(start, start + direction, Vector3.up), maxDistance, layerMask, queryTriggerInteraction);

        public static int GetCapsuleRays(Vector3 start, Vector3 end, float radius, Vector3 direction, PortalRay[] rays, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => GetRays(new CapsuleCaster(end - start, radius), Matrix4x4.LookAt(start, start + direction, Vector3.up), rays, maxDistance, layerMask, queryTriggerInteraction);

        #endregion
    }
}