using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{
    public class DrawDepthOnlyPortalsPass : PortalRenderPass
    {
        public Material depthOnlyMaterial { get; set; }

        public DrawDepthOnlyPortalsPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent) { }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!depthOnlyMaterial)
            {
                Debug.LogError(nameof(DrawDepthOnlyPortalsPass) + " requires a depthOnlyMaterial!");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                PortalRenderNode parentNode = PortalPassStack.Current.renderNode;

                PortalPassStack.Current.SetViewAndProjectionMatrices(cmd);

                cmd.SetGlobalInt(PropertyID.PortalStencilRef, PortalPassStack.Current.stateBlock.stencilReference);

                foreach (PortalRenderNode renderNode in parentNode.children)
                {
                    Material depthOnlyMaterial = renderNode.overrides.portalDepthOnly ? renderNode.overrides.portalDepthOnly : this.depthOnlyMaterial;

                    foreach (IPortalRenderer renderer in renderNode.renderers)
                        renderer.Render(renderNode, cmd, depthOnlyMaterial);
                }

                context.ExecuteCommandBuffer(cmd);
            }

            CommandBufferPool.Release(cmd);
        }
    }
}
