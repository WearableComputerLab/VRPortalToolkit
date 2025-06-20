using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Rendering.Universal
{
    /// <summary>
    /// Render pass that completes the rendering process for a stencil-based portal.
    /// </summary>
    public class CompleteStencilPortalPass : PortalRenderPass
    {
        /// <summary>
        /// The material used to clear the depth buffer for portal rendering.
        /// </summary>
        public Material clearDepthMaterial { get; set; }

        /// <summary>
        /// The material used to decrease the stencil value for portal rendering.
        /// </summary>
        public Material decreaseMaterial { get; set; }

        /// <summary>
        /// The material used for depth-only rendering for portal rendering.
        /// </summary>
        public Material depthMaterial { get; set; }

        /// <summary>
        /// Initializes a new instance of the CompleteStencilPortalPass class.
        /// </summary>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public CompleteStencilPortalPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent) { }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                Camera camera = renderingData.cameraData.camera;

                PortalPassNode passNode = PortalPassStack.Pop();
                PortalRenderNode renderNode = passNode.renderNode;

                Material decreaseMaterial = renderNode.overrides.portalDecrease ? renderNode.overrides.portalDecrease : this.decreaseMaterial,
                    depthMaterial = renderNode.overrides.portalDepthOnly ? renderNode.overrides.portalDepthOnly : this.depthMaterial,
                    clearDepthMaterial = renderNode.overrides.portalClearDepth ? renderNode.overrides.portalClearDepth : this.clearDepthMaterial;

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

                //context.ExecuteCommandBuffer(cmd);
                //cmd.Clear();
                if (clearDepthMaterial)
                {
                    foreach (IPortalRenderer renderer in renderNode.renderers)
                        renderer.Render(renderNode, cmd, clearDepthMaterial);
                }

                if (depthMaterial)
                {
                    foreach (IPortalRenderer renderer in renderNode.renderers)
                        renderer.Render(renderNode, cmd, depthMaterial);
                }
                // Unmask
                if (decreaseMaterial)
                {
                    foreach (IPortalRenderer renderer in renderNode.renderers)
                        renderer.Render(renderNode, cmd, decreaseMaterial);
                }

                cmd.SetGlobalInt(PropertyID.PortalStencilRef, renderNode.depth - 1);

                context.ExecuteCommandBuffer(cmd);
            }

            CommandBufferPool.Release(cmd);
        }
    }
}
