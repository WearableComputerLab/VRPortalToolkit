using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit.Rendering.Universal
{
    /// <summary>
    /// Render pass that begins the rendering process for a stencil-based portal.
    /// </summary>
    public class IncreaseStencilPortalsPass : ScriptableRenderPass
    {
        /// <summary>
        /// The material used to increase the stencil value for portal rendering.
        /// </summary>
        public Material increaseMaterial { get; set; }

        /// <summary>
        /// The material used to clear the depth buffer for portal rendering.
        /// </summary>
        public Material clearDepthMaterial { get; set; }

        /// <summary>
        /// The portal pass node associated with this pass.
        /// </summary>
        public PortalPassNode passNode { get; set; }

        public List<PortalRenderNode> nodesToIncrease { get; private set; } = new List<PortalRenderNode>();

        private static readonly Plane[] _planes = new Plane[6];

        /// <summary>
        /// Initializes a new instance of the BeginStencilPortalPass class.
        /// </summary>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public IncreaseStencilPortalsPass(RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRendering) : base()
        {
            this.renderPassEvent = renderPassEvent;
        }

        private class PassData
        {
            public Material increaseMaterial;

            public Material clearDepthMaterial;

            public List<PortalRenderNode> nodesToIncrease;
        }

        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            foreach (var renderNode in data.nodesToIncrease)
            {
                Material increaseMaterial = renderNode.overrides.portalIncrease ? renderNode.overrides.portalIncrease : data.increaseMaterial,
                    clearDepthMaterial = renderNode.overrides.portalClearDepth ? renderNode.overrides.portalClearDepth : data.clearDepthMaterial;

                // I dont think this actually moves the camera
                renderNode.parent.SetViewAndProjectionMatrices(context.cmd);
                //context.cmd.SetViewport(renderNode.parent.cullingWindow.GetRect());

                // Masking
                context.cmd.SetGlobalInt(PropertyID.PortalStencilRef, renderNode.depth - 1);

                if (increaseMaterial)
                {
                    foreach (IPortalRenderer renderer in renderNode.renderers)
                        renderer?.Render(renderNode, context.cmd, increaseMaterial);
                }

                context.cmd.SetGlobalInt(PropertyID.PortalStencilRef, renderNode.depth);

                if (clearDepthMaterial)
                {
                    foreach (IPortalRenderer renderer in renderNode.renderers)
                        renderer?.Render(renderNode, context.cmd, clearDepthMaterial);
                }
                //float width = renderingData.cameraData.cameraTargetDescriptor.width,
                //    height = renderingData.cameraData.cameraTargetDescriptor.height;

                //Rect rect = renderNode.cullingWindow.GetRect();
                //passNode.viewport = new Rect(rect.x * width, rect.y * height, rect.width * width, rect.height * height);

                // Setup current pass group
                //cmd.SetGlobalVector(PropertyID.WorldSpaceCameraPos, (Vector3)renderNode.localToWorldMatrix.GetColumn(3));

            }

            PortalRenderStack.Current.SetViewAndProjectionMatrices(context.cmd);
            //context.cmd.SetViewport(PortalRenderStack.Current.cullingWindow.GetRect());
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = "IncreaseStencilPortalsPass";

            // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                passData.clearDepthMaterial = clearDepthMaterial;
                passData.increaseMaterial = increaseMaterial;
                passData.nodesToIncrease = nodesToIncrease;
                var cameraData = frameData.Get<UniversalCameraData>();

                builder.AllowGlobalStateModification(true);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
                //builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }
    }
}
