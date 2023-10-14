using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VRPortalToolkit.Data;

namespace VRPortalToolkit.Rendering.Universal
{
    public class DrawTexturePortalsPass : PortalRenderPass
    {
        private static MaterialPropertyBlock propertyBlock;

        public Material material { get; set; }

        public DrawTexturePortalsPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent)
        {
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!material)
            {
                Debug.LogError(nameof(DrawTexturePortalsPass) + " requires a material!");
                return;
            }

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
