using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using VRPortalToolkit.Data;

namespace VRPortalToolkit.Rendering.Universal
{
    /// <summary>
    /// Render pass that draws texture-based portals into the scene.
    /// </summary>
    public class DrawTexturePortalsPass : ScriptableRenderPass
    {
        private static MaterialPropertyBlock propertyBlock;

        /// <summary>
        /// The material to use for rendering the portals.
        /// </summary>
        public Material material { get; set; }

        /// <summary>
        /// Initializes a new instance of the DrawTexturePortalsPass class.
        /// </summary>
        /// <param name="renderPassEvent">When this render pass should execute during rendering.</param>
        public DrawTexturePortalsPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingSkybox) : base()
        {
            this.renderPassEvent = renderPassEvent;
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        }

        private class PassData
        {
            public Material material;
            public UniversalCameraData cameraData;
        }

        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            PortalRenderNode parentNode = PortalRenderStack.Current;

            //float width = data.cameraData.cameraTargetDescriptor.width;
            //float height = data.cameraData.cameraTargetDescriptor.height;

            Rect rect = parentNode.cullingWindow.GetRect();
            Vector4 st = new Vector4(rect.width, rect.height, rect.x, rect.y);
            //Vector4 st = new Vector4(1f, 1f, 0f, 0f);

            propertyBlock.SetVector(PropertyID.MainTex_ST, st);

            if (parentNode.isStereo) 
                propertyBlock.SetVector(PropertyID.MainTex_ST_2, st);

            foreach (PortalRenderNode renderNode in parentNode.children)
            {
                if (renderNode.isValid)
                {
                    Material material = renderNode.overrides.portalStereo ? renderNode.overrides.portalStereo : data.material;

                    if (RenderPortalsBuffer.TryGetBuffer(renderNode, out RenderPortalsBuffer buffer))
                    {
                        propertyBlock.SetTexture(PropertyID.MainTex, buffer.texture);

                        foreach (IPortalRenderer renderer in renderNode.renderers)
                            renderer.Render(renderNode, context.cmd, material, propertyBlock);

                        RenderPortalsBuffer.ClearBuffer(renderNode);
                    }
                    else
                        renderNode.renderer.RenderDefault(renderNode, context.cmd);
                }
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = "DrawTexturePortalsPass";

            // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                passData.material = material;
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                passData.cameraData = frameData.Get<UniversalCameraData>();

                builder.AllowGlobalStateModification(true);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }
    }
}
