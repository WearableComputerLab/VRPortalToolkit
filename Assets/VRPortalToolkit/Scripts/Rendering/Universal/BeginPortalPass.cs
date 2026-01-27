using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using VRPortalToolkit.Rendering.Universal;
using VRPortalToolkit.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace VRPortalToolkit
{
    /// <summary>
    /// Render pass that begins the portal rendering process and initializes the portal pass stack.
    /// </summary>
    public class BeginPortalPass : PortalRenderPass
    {
        /// <summary>
        /// Initializes a new instance of the BeginPortalPass class.
        /// </summary>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public BeginPortalPass(RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRendering) : base()
        {
            this.renderPassEvent = renderPassEvent;
        }

        private class PassData { }

        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            //PortalRenderStack.Current.SetViewAndProjectionMatrices(context.cmd);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            cameraData.clearDepth = false;
            cameraData.renderType = CameraRenderType.Overlay;

            PortalRendering.onPreRender?.Invoke(PortalRenderStack.Current);

            foreach (var renderer in PortalRenderStack.Current.renderers)
                renderer?.PreCull(PortalRenderStack.Current);

            // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }
    }
}
