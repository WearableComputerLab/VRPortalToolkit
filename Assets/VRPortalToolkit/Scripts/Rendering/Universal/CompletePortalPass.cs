using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using VRPortalToolkit.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace VRPortalToolkit
{
    /// <summary>
    /// Render pass that completes the portal rendering process and cleans up resources.
    /// </summary>
    public class CompletePortalPass : ScriptableRenderPass
    {
        /// <summary>
        /// Initializes a new instance of the CompletePortalPass class.
        /// </summary>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public CompletePortalPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRendering) : base()
        {
            this.renderPassEvent = renderPassEvent;
        }

        private class PassData { }

        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            PortalRendering.onPostRender?.Invoke(PortalRenderStack.Current);

            foreach (var renderer in PortalRenderStack.Current.renderers)
                renderer?.PostCull(PortalRenderStack.Current);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = "CompletePortalPass";

            // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }
    }
}
