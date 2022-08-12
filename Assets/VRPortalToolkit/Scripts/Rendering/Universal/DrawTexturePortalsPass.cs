using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VRPortalToolkit.Data;

namespace VRPortalToolkit.Rendering.Universal
{
    public class DrawTexturePortalsPass : PortalRenderPass
    {
        private static MaterialPropertyBlock propertyBlock;

        public DrawTexturePortalsPass(PortalRenderFeature feature) : base(feature)
        {
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!feature.portalStereo)
            {
                Debug.LogError(nameof(DrawTexturePortalsPass) + " requires feature.portalStereo!");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                feature.currentGroup.SetViewAndProjectionMatrices(cmd);

                cmd.SetGlobalInt(PropertyID.PortalStencilRef, feature.currentGroup.stateBlock.stencilReference);

                PortalRenderNode parentNode = feature.currentGroup.renderNode;

                float width = renderingData.cameraData.cameraTargetDescriptor.width,
                    height = renderingData.cameraData.cameraTargetDescriptor.height;

                Vector4 st = new Vector4(feature.currentGroup.viewport.width / width,
                    feature.currentGroup.viewport.height / height,
                    feature.currentGroup.viewport.x / width,
                    feature.currentGroup.viewport.y / height);

                propertyBlock.SetVector(PropertyID.MainTex_ST, st);

                if (parentNode.isStereo)
                    propertyBlock.SetVector(PropertyID.MainTex_ST_2, st);

                foreach (PortalRenderNode renderNode in parentNode.children)
                {
                    if (renderNode.isValid)
                    {
                        if (feature.portalStereo && RenderPortalsBuffer.TryGetBuffer(renderNode, out RenderPortalsBuffer buffer))
                        {
                            propertyBlock.SetTexture(PropertyID.MainTex, buffer.texture);

                            renderNode.renderer.Render(renderingData.cameraData.camera, renderNode, cmd, feature.portalStereo, propertyBlock);

                            RenderPortalsBuffer.ClearBuffer(renderNode);
                        }
                        else
                            renderNode.renderer.RenderDefault(renderingData.cameraData.camera, renderNode, cmd);
                    }
                }

                context.ExecuteCommandBuffer(cmd);
            }

            CommandBufferPool.Release(cmd);
        }
    }
}
