using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace VRPortalToolkit.Rendering.Universal
{
    /// <summary>
    /// This render pass is specifically designed for the case where one eye of a stereo camera has passed through a portal.
    /// Handles the special stencil portal rendering required for this transition case.
    /// </summary>
    public class CompleteUndoStencilPortalPass : PortalRenderPass
    {
        /// <summary>
        /// The material used to decrease the stencil value for portal rendering.
        /// </summary>
        public Material decreaseMaterial { get; set; }

        /// <summary>
        /// Initializes a new instance of the CompleteUndoStencilPortalPass class.
        /// </summary>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public CompleteUndoStencilPortalPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent) { }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                PortalPassNode passNode = PortalPassStack.Pop();
                PortalRenderNode renderNode = passNode.renderNode;

                // Release shadow textures
                if (passNode.mainLightShadowCasterPass != null)
                    passNode.mainLightShadowCasterPass.OnPortalCleanup(cmd);

                if (passNode.additionalLightsShadowCasterPass != null)
                    passNode.additionalLightsShadowCasterPass.OnPortalCleanup(cmd);

                // Trigger Post Render
                foreach (IPortalRenderer renderer in renderNode.renderers)
                    renderer?.PostRender(renderNode);
                PortalRendering.onPostRender?.Invoke(renderNode);

                PortalPassGroupPool.Release(passNode);

                PortalPassStack.Current.RestoreState(cmd, ref renderingData);
                PortalPassStack.Current.SetViewAndProjectionMatrices(cmd);

                Material decreaseMaterial = renderNode.overrides.portalDecrease ? renderNode.overrides.portalDecrease : this.decreaseMaterial;

                // Unmask
                if (decreaseMaterial)
                {
                    foreach (IPortalRenderer renderer in renderNode.renderers)
                        renderer.Render(renderNode, cmd, decreaseMaterial);
                }

                cmd.SetGlobalInt(PropertyID.PortalStencilRef, renderNode.depth - 1);
                
                // Decrease twice so that it now acts like the normal world
                if (decreaseMaterial)
                {
                    foreach (IPortalRenderer renderer in renderNode.renderers)
                        renderer.Render(renderNode, cmd, decreaseMaterial);
                }

                context.ExecuteCommandBuffer(cmd);
            }

            CommandBufferPool.Release(cmd);
        }
    }
}