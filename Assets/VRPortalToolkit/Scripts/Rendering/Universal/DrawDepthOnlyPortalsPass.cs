using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{
    /// <summary>
    /// Render pass that draws portals with depth-only material to prepare the depth buffer.
    /// </summary>
    public class DrawDepthOnlyPortalsPass : PortalRenderPass
    {
        /// <summary>
        /// The material to use for depth-only rendering of portals.
        /// </summary>
        public Material depthOnlyMaterial { get; set; }

        /// <summary>
        /// Initializes a new instance of the DrawDepthOnlyPortalsPass class.
        /// </summary>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public DrawDepthOnlyPortalsPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent) { }

        /// <inheritdoc/>
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
