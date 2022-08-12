using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit.Cloning
{
    public static partial class PortalCloning
    {
        public static bool UpdateMeshFilter(MeshFilter clone)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<MeshFilter> cloneInfo))
            {
                UpdateMeshFilter(cloneInfo);
                return true;
            }

            return false;
        }

        public static void UpdateMeshFilter(PortalCloneInfo<MeshFilter> cloneInfo)
        {
            MeshFilter original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone)
                clone.sharedMesh = original.sharedMesh;
        }

        public static bool UpdateRenderer(Renderer clone, bool includePropertyBlocks = true)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<Renderer> cloneInfo))
            {
                UpdateRenderer(cloneInfo, includePropertyBlocks);
                return true;
            }

            return false;
        }

        public static void UpdateRenderer<TRenderer>(PortalCloneInfo<TRenderer> cloneInfo, bool includePropertyBlocks = true) where TRenderer : Renderer
        {
            Renderer original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone)
            {
                if (original is MeshRenderer originalM && clone is MeshRenderer cloneM)
                    UpdateMeshRenderer(originalM, cloneM, includePropertyBlocks);
                else if (original is SkinnedMeshRenderer originalS && clone is SkinnedMeshRenderer cloneS)
                    UpdateSkinnedMeshRenderer(originalS, cloneS, includePropertyBlocks);
                else if (original is LineRenderer originalL && clone is LineRenderer cloneL)
                    UpdateLineRenderer(originalL, cloneL, includePropertyBlocks);
                else
                    UpdateRenderer(original, clone, includePropertyBlocks);
            }
        }

        public static bool UpdateRenderer(MeshRenderer clone, bool includePropertyBlocks = true)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<MeshRenderer> cloneInfo))
            {
                UpdateRenderer(cloneInfo, includePropertyBlocks);
                return true;
            }

            return false;
        }

        public static void UpdateRenderer(PortalCloneInfo<MeshRenderer> cloneInfo, bool includePropertyBlocks = true)
        {
            MeshRenderer original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone) UpdateMeshRenderer(original, clone, includePropertyBlocks);
        }

        public static bool UpdateRenderer(SkinnedMeshRenderer clone, bool includePropertyBlocks = true)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<SkinnedMeshRenderer> cloneInfo))
            {
                UpdateRenderer(cloneInfo, includePropertyBlocks);
                return true;
            }

            return false;
        }

        public static void UpdateRenderer(PortalCloneInfo<SkinnedMeshRenderer> cloneInfo, bool includePropertyBlocks = true)
        {
            SkinnedMeshRenderer original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone) UpdateSkinnedMeshRenderer(original, clone, includePropertyBlocks);
        }

        public static bool UpdateRenderer(LineRenderer clone, bool includePropertyBlocks = true)
        {
            if (TryGetCloneInfo(clone, out PortalCloneInfo<LineRenderer> cloneInfo))
            {
                UpdateRenderer(cloneInfo, includePropertyBlocks);
                return true;
            }

            return false;
        }

        public static void UpdateRenderer(PortalCloneInfo<LineRenderer> cloneInfo, bool includePropertyBlocks = true)
        {
            LineRenderer original = cloneInfo.original, clone = cloneInfo.clone;

            if (original && clone) UpdateLineRenderer(original, clone, includePropertyBlocks);
        }

        private static void UpdateMeshRenderer(MeshRenderer original, MeshRenderer clone, bool includePropertyBlocks)
        {
            UpdateRenderer(original, clone, includePropertyBlocks);

            //clone.additionalVertexStreams = original.additionalVertexStreams;
            //clone.enlightenVertexStream = original.enlightenVertexStream;
            //clone.receiveGI = original.receiveGI;
            //clone.stitchLightmapSeams = original.stitchLightmapSeams;
        }

        private static void UpdateSkinnedMeshRenderer(SkinnedMeshRenderer original, SkinnedMeshRenderer clone, bool includePropertyBlocks)
        {
            UpdateRenderer(original, clone, includePropertyBlocks);

            clone.quality = original.quality;
            clone.updateWhenOffscreen = original.updateWhenOffscreen;
            clone.forceMatrixRecalculationPerRender = original.forceMatrixRecalculationPerRender;
            clone.quality = original.quality;
            clone.skinnedMotionVectors = original.skinnedMotionVectors;
            //clone.vertexBufferTarget = original.vertexBufferTarget;

            Mesh sharedMesh = original.sharedMesh;
            clone.sharedMesh = sharedMesh;

            for (int i = 0; i < sharedMesh.blendShapeCount; i++)
                clone.SetBlendShapeWeight(i, original.GetBlendShapeWeight(0));
        }

        private static void UpdateLineRenderer(LineRenderer original, LineRenderer clone, bool includePropertyBlocks)
        {
            UpdateRenderer(original, clone, includePropertyBlocks);

            //clone.startWidth = original.startWidth;
            //clone.endWidth = original.endWidth;
            clone.widthMultiplier = original.widthMultiplier;
            clone.numCornerVertices = original.numCornerVertices;
            clone.numCapVertices = original.numCapVertices;
            clone.useWorldSpace = original.useWorldSpace; // TODO: This should probably apply portals?
            clone.loop = original.loop;
            //clone.startColor = original.startColor;
            //clone.endColor = original.endColor;
            clone.positionCount = original.positionCount;
            clone.shadowBias = original.shadowBias;
            clone.generateLightingData = original.generateLightingData;
            clone.textureMode = original.textureMode;
            clone.alignment = original.alignment;
            clone.widthCurve = original.widthCurve;
            clone.colorGradient = original.colorGradient;

            for (int i = 0; i < original.positionCount; i++)
                clone.SetPosition(i, original.GetPosition(i));
        }

        private static MaterialPropertyBlock _propertyBlock;
        private static void UpdateRenderer(Renderer original, Renderer clone, bool includePropertyBlocks)
        {
            //clone.localBounds = original.localBounds;
            clone.enabled = original.enabled;
            clone.shadowCastingMode = original.shadowCastingMode;
            clone.receiveShadows = original.receiveShadows;
            clone.forceRenderingOff = original.forceRenderingOff;
            //clone.staticShadowCaster = original.staticShadowCaster;
            clone.motionVectorGenerationMode = original.motionVectorGenerationMode;
            clone.lightProbeUsage = original.lightProbeUsage;
            clone.reflectionProbeUsage = original.reflectionProbeUsage;
            clone.renderingLayerMask = original.renderingLayerMask;
            clone.rendererPriority = original.rendererPriority;
            clone.rayTracingMode = original.rayTracingMode;
            clone.sortingLayerName = original.sortingLayerName;
            clone.sortingLayerID = original.sortingLayerID;
            clone.sortingOrder = original.sortingOrder;
            clone.allowOcclusionWhenDynamic = original.allowOcclusionWhenDynamic;
            clone.lightmapScaleOffset = original.lightmapScaleOffset;

            Material[] sharedMaterials = original.sharedMaterials;
            clone.sharedMaterials = sharedMaterials;

            if (includePropertyBlocks)
            {
                if (original.HasPropertyBlock())
                {
                    if (_propertyBlock == null) _propertyBlock = new MaterialPropertyBlock();

                    for (int i = 0; i < sharedMaterials.Length; i++)
                    {
                        original.GetPropertyBlock(_propertyBlock, i);

                        if (_propertyBlock.isEmpty)
                            clone.SetPropertyBlock(null, i);
                        else
                            clone.SetPropertyBlock(_propertyBlock, i);
                    }
                }
                else if (clone.HasPropertyBlock())
                    clone.SetPropertyBlock(null);
            }
        }
    }
}
