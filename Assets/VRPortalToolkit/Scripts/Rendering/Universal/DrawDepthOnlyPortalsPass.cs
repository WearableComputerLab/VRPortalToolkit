using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace VRPortalToolkit.Rendering.Universal
{
    /// <summary>
    /// Render pass that draws portals with depth-only material to prepare the depth buffer.
    /// </summary>
    public class DrawDepthOnlyPortalsPass : ScriptableRenderPass
    {
        /// <summary>
        /// The material to use for depth-only rendering of portals.
        /// </summary>
        public Material depthOnlyMaterial { get; set; }

        /// <summary>
        /// Initializes a new instance of the DrawDepthOnlyPortalsPass class.
        /// </summary>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public DrawDepthOnlyPortalsPass(RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing) : base()
        {
            this.renderPassEvent = renderPassEvent;
        }

        private class PassData
        {
            public Material material;
        }

        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            PortalRenderNode parentNode = PortalRenderStack.Current;

            //PortalPassStack.Current.SetViewAndProjectionMatrices(context.cmd);
            //context.cmd.SetGlobalInt(PropertyID.PortalStencilRef, PortalPassStack.Current.stateBlock.stencilReference);

            foreach (PortalRenderNode renderNode in parentNode.children)
            {
                Material depthOnlyMaterial = renderNode.overrides.portalDepthOnly ? renderNode.overrides.portalDepthOnly : data.material;

                foreach (IPortalRenderer renderer in renderNode.renderers)
                    renderer.Render(renderNode, context.cmd, depthOnlyMaterial);
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = "Draw Depth Only Portals Pass";

            // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                passData.material = depthOnlyMaterial;
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                builder.AllowGlobalStateModification(true);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }
    }
}
