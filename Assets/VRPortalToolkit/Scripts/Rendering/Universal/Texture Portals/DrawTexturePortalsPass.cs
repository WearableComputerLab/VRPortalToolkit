using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VRPortalToolkit.Data;

namespace VRPortalToolkit.Rendering.Universal
{
    /// <summary>
    /// Render pass that draws texture-based portals into the scene.
    /// </summary>
    public class DrawTexturePortalsPass : PortalRenderPass
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
        public DrawTexturePortalsPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent)
        {
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            //using (new ProfilingScope(cmd, profilingSampler))
            {
                PortalPassStack.Current.SetViewAndProjectionMatrices(cmd);

                cmd.SetGlobalInt(PropertyID.PortalStencilRef, PortalPassStack.Current.stateBlock.stencilReference);

                PortalRenderNode parentNode = PortalPassStack.Current.renderNode;

                float width = renderingData.cameraData.cameraTargetDescriptor.width,
                    height = renderingData.cameraData.cameraTargetDescriptor.height;

                Vector4 st = new Vector4(PortalPassStack.Current.viewport.width / width, PortalPassStack.Current.viewport.height / height,
                    PortalPassStack.Current.viewport.x / width, PortalPassStack.Current.viewport.y / height);

                propertyBlock.SetVector(PropertyID.MainTex_ST, st);

                if (parentNode.isStereo)
                    propertyBlock.SetVector(PropertyID.MainTex_ST_2, st);

                foreach (PortalRenderNode renderNode in parentNode.children)
                {
                    if (renderNode.isValid)
                    {
                        Material material = renderNode.overrides.portalStereo ? renderNode.overrides.portalStereo : this.material;

                        if (RenderPortalsBuffer.TryGetBuffer(renderNode, out RenderPortalsBuffer buffer))
                        {
                            propertyBlock.SetTexture(PropertyID.MainTex, buffer.texture);

                            foreach (IPortalRenderer renderer in renderNode.renderers)
                                renderer.Render(renderNode, cmd, material, propertyBlock);

                            RenderPortalsBuffer.ClearBuffer(renderNode);
                        }
                        else
                            renderNode.renderer.RenderDefault(renderNode, cmd);
                    }
                }

                context.ExecuteCommandBuffer(cmd);
            }

            CommandBufferPool.Release(cmd);
        }
    }
}
