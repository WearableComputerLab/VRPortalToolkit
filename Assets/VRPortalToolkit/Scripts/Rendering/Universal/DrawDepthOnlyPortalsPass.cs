using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{
    public class DrawDepthOnlyPortalsPass : PortalRenderPass
    {
        public DrawDepthOnlyPortalsPass(PortalRenderFeature feature) : base(feature) { }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!feature.portalDepthOnly)
            {
                Debug.LogError(nameof(DrawDepthOnlyPortalsPass) + " requires feature.portalDepthOnly!");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                PortalRenderNode parentNode = feature.currentGroup.renderNode;

                feature.currentGroup.SetViewAndProjectionMatrices(cmd);

                cmd.SetGlobalInt(PropertyID.PortalStencilRef, feature.currentGroup.stateBlock.stencilReference);

                foreach (PortalRenderNode renderNode in parentNode.children)
                {
                    if (renderNode.renderer)
                        renderNode.renderer.Render(renderingData.cameraData.camera, renderNode, cmd, feature.portalDepthOnly);
                }

                context.ExecuteCommandBuffer(cmd);
            }

            CommandBufferPool.Release(cmd);
        }
    }
}
