using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Rendering.Universal
{
    /// <summary>
    /// Render pass that completes the rendering process for a stencil-based portal.
    /// </summary>
    public class DecreaseStencilPortalsPass : ScriptableRenderPass
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

        public List<PortalRenderNode> nodesToDecrease { get; private set; } = new List<PortalRenderNode>();

        /// <summary>
        /// Initializes a new instance of the CompleteStencilPortalPass class.
        /// </summary>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public DecreaseStencilPortalsPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRendering) : base()
        {
            this.renderPassEvent = renderPassEvent;
        }

        private class PassData
        {
            public Material decreaseMaterial;

            public Material clearDepthMaterial;

            public Material depthMaterial;

            public List<PortalRenderNode> nodesToDecrease;
        }

        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            foreach (var renderNode in data.nodesToDecrease)
            {
                Material decreaseMaterial = renderNode.overrides.portalDecrease ? renderNode.overrides.portalDecrease : data.decreaseMaterial,
                    depthMaterial = renderNode.overrides.portalDepthOnly ? renderNode.overrides.portalDepthOnly : data.depthMaterial,
                    clearDepthMaterial = renderNode.overrides.portalClearDepth ? renderNode.overrides.portalClearDepth : data.clearDepthMaterial;

                // Trigger Post Render
                foreach (IPortalRenderer renderer in renderNode.renderers)
                    renderer?.PostRender(renderNode);
                PortalRendering.onPostRender?.Invoke(renderNode);

                renderNode.SetViewAndProjectionMatrices(context.cmd);

                if (clearDepthMaterial)
                {
                    foreach (IPortalRenderer renderer in renderNode.renderers)
                        renderer.Render(renderNode, context.cmd, clearDepthMaterial);
                }

                if (depthMaterial)
                {
                    foreach (IPortalRenderer renderer in renderNode.renderers)
                        renderer.Render(renderNode, context.cmd, depthMaterial);
                }

                // Unmask
                if (decreaseMaterial)
                {
                    foreach (IPortalRenderer renderer in renderNode.renderers)
                        renderer.Render(renderNode, context.cmd, decreaseMaterial);
                }

                context.cmd.SetGlobalInt(PropertyID.PortalStencilRef, renderNode.depth - 1);
            }

            PortalRenderStack.Current.SetViewAndProjectionMatrices(context.cmd);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = "DecreaseStencilPortalsPass";

            // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                passData.clearDepthMaterial = clearDepthMaterial;
                passData.nodesToDecrease = nodesToDecrease;
                passData.depthMaterial = depthMaterial;

                builder.AllowGlobalStateModification(true);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }
    }
}
