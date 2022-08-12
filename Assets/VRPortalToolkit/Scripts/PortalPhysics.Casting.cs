using System;
using UnityEngine;
using VRPortalToolkit.Physics;

namespace VRPortalToolkit
{
    public static partial class PortalPhysics
    {
        #region Generic Casting

        public static bool Cast(IPhysicsCaster caster, Matrix4x4 origin, LayerMask portalLayerMask, out RaycastHit hitInfo, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(caster, GetRays(caster, origin, maxDistance, portalLayerMask, queryTriggerInteraction), out hitInfo, layerMask, queryTriggerInteraction);

        public static bool Cast(IPhysicsCaster caster, PortalRay[] rays, out RaycastHit hitInfo, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(caster, rays, rays.Length, out hitInfo, out int rayIndex, layerMask, queryTriggerInteraction);

        public static bool Cast(IPhysicsCaster caster, PortalRay[] rays, int rayCount, out RaycastHit hitInfo, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(caster, rays, rayCount, out hitInfo, out int rayIndex, layerMask, queryTriggerInteraction);

        public static bool Cast(IPhysicsCaster caster, Matrix4x4 origin, LayerMask portalLayerMask, out RaycastHit hitInfo, out int rayIndex, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(caster, GetRays(caster, origin, maxDistance, portalLayerMask, queryTriggerInteraction), out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);

        public static bool Cast(IPhysicsCaster caster, PortalRay[] rays, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => Cast(caster, rays, rays.Length, out hitInfo, out rayIndex, layerMask, queryTriggerInteraction);

        public static bool Cast(IPhysicsCaster caster, PortalRay[] rays, int rayCount, out RaycastHit hitInfo, out int rayIndex, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            if (rays != null)
            {
                if (rayCount > rays.Length) rayCount = rays.Length;

                PortalRay ray;

                for (int i = 0; i < rayCount; i++)
                {
                    ray = rays[i];

                    if (caster.Cast(ray.localToWorldMatrix, out hitInfo, ray.localDistance, layerMask, queryTriggerInteraction))
                    {
                        rayIndex = i;
                        return true;
                    }
                }
            }

            rayIndex = -1;
            hitInfo = new RaycastHit();
            return false;
        }
        #endregion

        #region Raycast All

        public static RaycastHit[] CastAll(IPhysicsCaster caster, Matrix4x4 origin, LayerMask portalLayerMask, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastAll(caster, GetRays(caster, origin, maxDistance, portalLayerMask, queryTriggerInteraction), layerMask, queryTriggerInteraction);

        public static RaycastHit[] CastAll(IPhysicsCaster caster, PortalRay[] rays, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastAll(caster, rays, rays.Length, layerMask, queryTriggerInteraction);

        public static RaycastHit[] CastAll(IPhysicsCaster caster, PortalRay[] rays, int rayCount, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastAll(caster, rays, rayCount, null, layerMask, queryTriggerInteraction);

        public static RaycastHit[] CastAll(IPhysicsCaster caster, Matrix4x4 origin, LayerMask portalLayerMask, float maxDistance, out int[] rayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastAll(caster, GetRays(caster, origin, maxDistance, portalLayerMask, queryTriggerInteraction), out rayIndices, layerMask, queryTriggerInteraction);

        public static RaycastHit[] CastAll(IPhysicsCaster caster, PortalRay[] rays, out int[] rayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
            => CastAll(caster, rays, rays.Length, out rayIndices, layerMask, queryTriggerInteraction);

        public static RaycastHit[] CastAll(IPhysicsCaster caster, PortalRay[] rays, int rayCount, out int[] rayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            int[] hitIndices = new int[rayCount];

            RaycastHit[] hitInfo = CastAll(caster, rays, rayCount, hitIndices, layerMask, queryTriggerInteraction);

            rayIndices = new int[hitInfo.Length];

            if (hitInfo.Length > 0)
            {
                int rayIndex = 0, hitCount = hitIndices[0], hitIndex = 0;

                while (hitIndex < rayIndices.Length)
                {
                    rayIndices[hitIndex] = rayIndex;

                    if (hitIndex++ > hitCount)
                        hitCount = hitIndices[++rayIndex];

                }
            }

            return hitInfo;
        }

        private static RaycastHit[] CastAll(IPhysicsCaster caster, PortalRay[] rays, int rayCount, int[] hitIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            if (rayCount > rays.Length) rayCount = rays.Length;
            if (rays == null || rayCount <= 0)
            {
                hitIndices = new int[0];
                return new RaycastHit[0];
            }

            PortalRay ray;

            if (rayCount == 1)
            {
                ray = rays[0];
                if (hitIndices != null) hitIndices[0] = 1;
                return caster.CastAll(ray.localToWorldMatrix, ray.localDistance, layerMask, queryTriggerInteraction);
            }

            RaycastHit[][] allHits = new RaycastHit[rayCount][];
            RaycastHit[] hits, finalHits;
            int count = 0;

            for (int i = 0; i < rayCount; i++)
            {
                ray = rays[i];
                allHits[i] = hits = caster.CastAll(ray.localToWorldMatrix, ray.localDistance, layerMask, queryTriggerInteraction);
                count += allHits.Length;
                if (hitIndices != null) hitIndices[i] = count;
            }

            finalHits = new RaycastHit[count];
            count = 0;

            for (int i = 0; i < rayCount; i++)
            {
                hits = allHits[i];
                Array.Copy(hits, 0, finalHits, count, allHits.Length);
                count += allHits.Length;
            }

            return finalHits;
        }
        #endregion

        #region Raycast Non Alloc

        public static int CastNonAlloc(IPhysicsCaster caster, Matrix4x4 origin, LayerMask portalLayerMask, float maxDistance, RaycastHit[] results, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(caster, GetRays(caster, origin, maxDistance, portalLayerMask, queryTriggerInteraction), results, layerMask, queryTriggerInteraction);

        public static int CastNonAlloc(IPhysicsCaster caster, PortalRay[] rays, RaycastHit[] results, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(caster, rays, rays.Length, results, layerMask, queryTriggerInteraction);

        public static int CastNonAlloc(IPhysicsCaster caster, PortalRay[] rays, int rayCount, RaycastHit[] results, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(caster, rays, rayCount, results, null, layerMask, queryTriggerInteraction);

        public static int CastNonAlloc(IPhysicsCaster caster, PortalRay[] rays, RaycastHit[] results, int[] resultRayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(caster, rays, rays.Length, results, resultRayIndices, layerMask, queryTriggerInteraction);

        public static int CastNonAlloc(IPhysicsCaster caster, Matrix4x4 origin, LayerMask portalLayerMask, float maxDistance, RaycastHit[] results, int[] resultRayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => CastNonAlloc(caster, GetRays(caster, origin, maxDistance, portalLayerMask, queryTriggerInteraction), results, resultRayIndices, layerMask, queryTriggerInteraction);

        public static int CastNonAlloc(IPhysicsCaster caster, PortalRay[] rays, int rayCount, RaycastHit[] results, int[] resultRayIndices, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            if (rays == null || rayCount <= 0) return 0;

            if (rayCount > rays.Length) rayCount = rays.Length;

            PortalRay ray;

            int count = 0, total = 0, rayIndicesCount;

            if (rayCount == 1)
            {
                ray = rays[0];
                total = caster.CastNonAlloc(ray.localToWorldMatrix, results, ray.localDistance, layerMask, queryTriggerInteraction);

                if (resultRayIndices != null)
                {
                    rayIndicesCount = resultRayIndices.Length < total ? resultRayIndices.Length : total;

                    for (int i = 0; i < rayIndicesCount; i++)
                        resultRayIndices[i] = 0;
                }

                return total;
            }

            for (int i = rayCount - 1; i != 0; i--)
            {
                Array.Reverse(results);

                ray = rays[i];
                count = caster.CastNonAlloc(ray.localToWorldMatrix, results, ray.localDistance, layerMask, queryTriggerInteraction);

                Array.Reverse(results, count, results.Length);

                // TODO: Need to figure out how to get results indices, but brain no work good at the moment
                /*if (resultRayIndices != null)
                {
                    rayIndicesCount = resultRayIndices.Length < total ? resultRayIndices.Length : total;

                    for (int j = total; j < rayIndicesCount; j++)
                        resultRayIndices[j] = 0;
                }*/

                total += count;
            }

            return total > results.Length ? results.Length : total;
        }

        #endregion

        #region Portal Rays

        public static PortalRay[] GetRays(IPhysicsCaster caster, Matrix4x4 origin, float localDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
            => GetRaysRecursive(caster, origin, localDistance, layerMask, 0, out int _, queryTriggerInteraction);

        public static int GetRays(IPhysicsCaster caster, Matrix4x4 origin, PortalRay[] rays, float localDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            if (rays == null || rays.Length == 0) return 0;

            GetRaysRecursive(caster, origin, localDistance, layerMask, 0, out int count, queryTriggerInteraction, rays);

            return count;
        }

        // The uglier recurisive raycast
        private static PortalRay[] GetRaysRecursive(IPhysicsCaster caster, Matrix4x4 origin, float localDistance, LayerMask layerMask, int startIndex,
            out int count, QueryTriggerInteraction queryTriggerInteraction, PortalRay[] rays = null, Portal previous = null)
        {
            if (caster == null)
            {
                count = 0;
                return null;
            }

            if (previous) previous.PreCast();

            // Shoot raycast
            bool raycastHit = caster.Cast(origin, out RaycastHit hitInfo, localDistance, layerMask, queryTriggerInteraction);

            if (previous) previous.PostCast();

            float magnitude = origin.GetColumn(2).magnitude;

            float newLocalDistance = magnitude > 0 ? hitInfo.distance / magnitude : 0f;

            // If no objects are hit, the recursion ends here, with no effect
            if (raycastHit && (rays == null || startIndex < rays.Length - 1) && newLocalDistance > 0f && newLocalDistance < localDistance)
            {
                Portal portal = GetPortal(hitInfo);

                if (portal)
                {
                    PortalRay ray = new PortalRay(previous, origin, newLocalDistance);

                    // Move the position to the end
                    Vector3 position = ray.origin + ray.direction;
                    origin.SetColumn(3, new Vector4(position.x, position.y, position.z, origin.m33));

                    if (portal.usesLayers) layerMask = portal.ModifyLayerMask(layerMask);

                    if (portal.usesTeleport) portal.ModifyMatrix(ref origin);

                    rays = GetRaysRecursive(caster, origin, localDistance - newLocalDistance,
                        layerMask, startIndex + 1, out count, queryTriggerInteraction, rays, portal);

                    rays[startIndex] = ray;

                    return rays;
                }
            }

            count = startIndex + 1;

            if (rays == null) rays = new PortalRay[count];

            if (!raycastHit || localDistance < newLocalDistance)
                rays[startIndex] = new PortalRay(previous, origin, localDistance);
            else
                rays[startIndex] = new PortalRay(previous, origin, newLocalDistance);

            return rays;
        }

        #endregion

        private static int CastSemiAlloc(IPhysicsCaster caster, Matrix4x4 origin, ref RaycastHit[] results, float localDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            if (results == null || results.Length < 2) results = new RaycastHit[2];

            int count = caster.CastNonAlloc(origin, results, localDistance, layerMask, queryTriggerInteraction);

            if (count >= results.Length)
            {
                // There could be more
                RaycastHit[] allResults = caster.CastAll(origin, localDistance, layerMask, queryTriggerInteraction);
                count = allResults.Length;

                int newLength = results.Length;

                while (newLength < count) newLength *= 2;

                results = new RaycastHit[newLength];

                Array.Copy(allResults, results, count);
            }

            return count;
        }
    }
}