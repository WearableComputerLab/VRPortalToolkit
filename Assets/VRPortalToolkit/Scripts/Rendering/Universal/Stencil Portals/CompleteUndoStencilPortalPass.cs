using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

namespace VRPortalToolkit.Rendering.Universal
{
    /// <summary>
    /// The only difference between this and CompleteStencilPortalPass is that we decrease a second time instead of apply depth
    /// </summary>
    public class CompleteUndoStencilPortalPass : PortalRenderPass
    {
        public Material decreaseMaterial { get; set; }

        public CompleteUndoStencilPortalPass(RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques) : base(renderPassEvent) { }

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